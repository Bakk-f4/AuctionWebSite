using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class User : IUser {

        public string Username { get; }
        public string Password { get; }
        public int SiteID { get; }
        public Site Site { get; }
        public string ConnectionString { get; }

        public User(string username, string password, int siteId, Site site) {
            Username = username;
            Password = password;
            SiteID = siteId;
            Site = site;
            ConnectionString = site.ConnectionString;
        }

        /// <summary>
        /// Disposes of the user and all its resources.
        /// </summary>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public void Delete() {
            //no own an active auction and not active winning an auction
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteID);

                var queryUser = (from user in c.Users
                    where user.Username == Username && user.SiteId == SiteID
                    select user).SingleOrDefault();
                if (queryUser == null)
                    throw new AuctionSiteInvalidOperationException("The user is not valid.");

                var queryAuctionOwner = (from auction in c.Auctions
                    where auction.Seller.UserID == queryUser.UserID && auction.EndsOn > Site.Now()
                    select auction);
                if (queryAuctionOwner.Any())
                    throw new AuctionSiteInvalidOperationException("The User " + queryUser.Username +
                                                                   " cannot be deleted because he owns Auctions.");

                var queryAuctionsWinner = (from auction in c.Auctions
                    where auction.CurrentWinnerID == queryUser.UserID && auction.EndsOn > Site.Now()
                    select auction);
                if (queryAuctionsWinner.Any())
                    throw new AuctionSiteInvalidOperationException("The User " + queryUser.Username +
                                                                   " cannot be deleted because he is winning an Auction.");

                var queryEndedAuctionsWinner = (from auction in c.Auctions
                    where auction.CurrentWinnerID == queryUser.UserID && auction.EndsOn <= Site.Now()
                    select auction);
                //removing winner information from his won auctions
                foreach (var q in queryEndedAuctionsWinner) {
                    q.CurrentWinnerID = null;
                    q.CurrentWinner = null;
                }
                c.Users.Remove(queryUser);
                c.SaveChanges();
            }
        }

        /// <summary>
        /// Yields the auctions won by the user.
        /// </summary>
        /// <returns>
        /// The auctions won by the user (that is, all the ended auctions of this site where this user is the highest bidder)
        /// </returns>
        public IEnumerable<IAuction> WonAuctions() {
            var aux = new List<IAuction>();

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteID);

                var queryWonAuctions = from a in c.Auctions
                    where a.CurrentWinner != null && a.CurrentWinner.Username == Username && a.SiteID == SiteID
                    select new {
                        Auction = a,
                        Seller = a.Seller
                    };

                foreach (var q in queryWonAuctions) {
                    var seller = new User(q.Seller.Username, q.Seller.Password, q.Seller.SiteId, Site);
                    var auction = new Auction(q.Auction.AuctionID, seller, q.Auction.Description, q.Auction.EndsOn,
                        q.Auction.SiteID, ConnectionString, Site);
                    aux.Add(auction);
                }
                return aux;
            }
        }

        public override bool Equals(object? o) {
            if (o == null || o.GetType() != GetType())
                return false;
            var obj = o as User;
            return obj!.SiteID == SiteID && obj.Username == Username;
        }

        public override int GetHashCode() {
            return Username.GetHashCode()^SiteID.GetHashCode();
        }
    }
}
