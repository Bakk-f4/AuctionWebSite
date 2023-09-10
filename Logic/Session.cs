using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class Session : ISession {

        /// <summary>
        /// Gets the unique key used to identify the sessions.
        /// </summary>
        public string Id { get; }

        public int SiteId { get; }

        public Site Site { get; }

        /// <summary>
        /// Gets the current expiration time of the session.
        /// </summary>
        public DateTime ValidUntil { get; set; }

        /// <summary>
        /// Gets the user owner of the session.
        /// </summary>
        public IUser User { get; }

        public string ConnectionString { get; }

        public Session(string id, DateTime validUntil, IUser user, int siteId, Site site) {
            Id = id;
            ValidUntil = validUntil;
            User = user;
            SiteId = siteId;
            ConnectionString = site.ConnectionString;
            Site = site;
        }

        /// <summary>
        /// Yields an auction for the described object/service. As a side effect, the expiration time of the session is reset (to the
        /// same value as if the session was newly created).
        /// </summary>
        /// <param name="description"></param>
        /// <param name="endsOn"></param>
        /// <param name="startingPrice"></param>
        /// <returns>Returns the newly created auction, whose Id is an automatically-generated unique identifier.</returns>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public IAuction CreateAuction(string description, DateTime endsOn, double startingPrice) {
            CheckAuctionParameters(description, endsOn, startingPrice);
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var querySite = (from site in c.Sites where site.SiteID == SiteId select site).SingleOrDefault();
                if (querySite == null)
                    throw new AuctionSiteInvalidOperationException("The Site selected is not valid anymore.");

                var queryUser = (from user in c.Users
                    where user.Username == User.Username && user.SiteId == SiteId
                    select user).SingleOrDefault();
                if (queryUser == null)
                    throw new AuctionSiteInvalidOperationException("The User selected is not valid anymore.");

                var querySession = (from session in c.Sessions
                    where session.UserID == queryUser!.UserID
                    select session).SingleOrDefault();

                if (querySession == null || querySession.ValidUntil < Site.Now())
                    throw new AuctionSiteInvalidOperationException("The session is not valid.");

                var auction = new AuctionDB {
                    Description = description,
                    EndsOn = endsOn,
                    StartingPrice = startingPrice,
                    CurrentPrice = startingPrice,
                    Site = querySite,
                    SiteID = querySite.SiteID,
                    Seller = queryUser,
                    SellerID = queryUser.UserID,
                };

                c.Auctions.Add(auction);
                c.SaveChanges();
                querySession.ValidUntil = Site.Now().AddSeconds(Site.SessionExpirationInSeconds);
                ValidUntil = querySession.ValidUntil;

                //in case of error is possible i need to bring a User instead of IUser here...
                return new Auction(auction.AuctionID, User, description, endsOn, SiteId, ConnectionString, this.Site);
            }
        }

        /// <summary>
        /// Deletes the session and disposes of all associated resources, if any.
        /// </summary>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public void Logout() {
            //delete the session
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var querySession =
                    (from session in c.Sessions
                        where session.SessionID.ToString().Equals(Id)
                        select session).SingleOrDefault();

                if (querySession == null || querySession.ValidUntil < Site.Now())
                    throw new AuctionSiteInvalidOperationException("Cannot delete an nonexistent session");

                c.Sessions.Remove(querySession);
                c.SaveChanges();
            }
        }

        public override bool Equals(object? o) {
            if (o == null || o.GetType() != GetType())
                return false;
            var obj = o as Session;
            return obj!.Id == Id && obj.SiteId == SiteId;
        }

        public override int GetHashCode() {
            return Id.GetHashCode()^SiteId.GetHashCode();
        }

        /// <summary>
        /// Check all the auction parameters.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="endsOn"></param>
        /// <param name="startingPrice"></param>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteArgumentException"></exception>
        /// <exception cref="AuctionSiteArgumentOutOfRangeException"></exception>
        /// <exception cref="AuctionSiteUnavailableTimeMachineException"></exception>
        private void CheckAuctionParameters(string description, DateTime endsOn, double startingPrice) {
            if (ValidUntil < Site.Now())
                throw new AuctionSiteInvalidOperationException("The session is expired, please login again.");
            if (description == null)
                throw new AuctionSiteArgumentNullException("The auction description cannot be null.");
            if (description == "")
                throw new AuctionSiteArgumentException("The auction description cannot be empty.");
            if (startingPrice < 0)
                throw new AuctionSiteArgumentOutOfRangeException("The auction starting price cannot be negative");
            if (endsOn < Site.Now())
                throw new AuctionSiteUnavailableTimeMachineException("The auction 'end' cannot be in the past.");
        }
    }
}
