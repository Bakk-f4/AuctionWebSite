
using System.Diagnostics;
using System;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class Auction : IAuction {
        /// <summary>
        /// Gets the unique key used to identify the auctions.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Gets the user who is selling the object/service.
        /// </summary>
        public IUser Seller { get; }

        /// <summary>
        /// Gets the description of the offered object/service.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the expiring time of the auction; no bid will be accepted after it.
        /// </summary>
        public DateTime EndsOn { get; }

        public int SiteId { get; }

        public string ConnectionString { get; }
        public Site Site { get; }

        public Auction(int id, IUser seller, string description, DateTime endsOn, int siteId, string connectionString, Site site) {
            Id = id;
            Seller = seller;
            Description = description;
            EndsOn = endsOn;
            SiteId = siteId;
            ConnectionString = connectionString;
            Site = site;
        }

        /// <summary>
        /// Makes a bid for this auction on behalf of the session owner; only possible for still open auctions.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="offer"></param>
        /// <returns></returns>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public bool Bid(ISession session, double offer) {
            //create the bid and insert it into the DB
            CheckBidParams(session, offer);

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, this.SiteId);

                var queryAuction = (from auction in c.Auctions
                    where auction.SiteID == SiteId && auction.AuctionID == Id
                    select auction).SingleOrDefault();

                if (queryAuction == null)
                    throw new AuctionSiteInvalidOperationException("The Auction is not valid.");

                var queryUser = (from user in c.Users
                    where session.User.Username == user.Username && queryAuction.SiteID == SiteId
                    select user).SingleOrDefault();

                var queryBids = from bids in c.Bids where bids.AuctionID == Id select bids;

                if(queryUser == null)
                    throw new AuctionSiteInvalidOperationException("The User selected is not valid anymore.");

                var newBid = new BidDB {
                    BidValue = offer,
                    BidDate = Site.Now(),
                    AuctionID = Id,
                    Auction = queryAuction,
                    User = queryUser,
                    UserID = queryUser.UserID
                };

                var maxOffer = queryAuction.MaxOffer;
                var auctionWinner = this.CurrentWinner();
                var validWinner = (auctionWinner != null && auctionWinner.Username.Equals(queryUser.Username));

                if (validWinner && offer < maxOffer + Site.MinimumBidIncrement)
                    return false;
                if (!validWinner && offer < this.CurrentPrice())
                    return false;
                if (!validWinner && offer < this.CurrentPrice() + Site.MinimumBidIncrement && queryBids.Any())
                    return false;
                if (!queryBids.Any()) {
                    queryAuction.MaxOffer = offer;
                    queryAuction.CurrentWinner = queryUser;
                }
                else if (validWinner) 
                    queryAuction.MaxOffer = offer;
                else if (queryBids.Any() && 
                         !validWinner &&
                         offer > queryAuction.MaxOffer) {
                    queryAuction.CurrentPrice =
                        Math.Min(offer, queryAuction.MaxOffer + Site.MinimumBidIncrement);
                    queryAuction.MaxOffer = offer;
                    queryAuction.CurrentWinner = queryUser;
                    queryAuction.CurrentWinnerID = queryUser.UserID;
                } 
                else if (queryBids.Any() &&
                          !validWinner &&
                          offer <= queryAuction.MaxOffer) {
                    queryAuction.CurrentPrice =
                        Math.Min(queryAuction.MaxOffer, offer + Site.MinimumBidIncrement);
                }

                (session as Session).ValidUntil = Site.Now().AddSeconds(Site.SessionExpirationInSeconds);

                c.Bids.Add(newBid);
                c.SaveChanges();
                return true;
            }
        }

        /// <summary>
        /// Returns the current price, which is the lowest amount needed to best the second highest bid if two or more bids
        /// have been offered; otherwise, it coincides with the starting price.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public double CurrentPrice() {
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, this.SiteId);

                var queryAuction =
                    (from auction in c.Auctions
                        where auction.AuctionID == Id && auction.SiteID == SiteId
                        select auction).SingleOrDefault();

                if (queryAuction == null)
                    throw new AuctionSiteInvalidOperationException("The auction is not valid");
                return queryAuction.CurrentPrice;
            }
        }

        /// <summary>
        /// Returns the user, if any, who has submitted the highest bid so far. In case no bids have been offered yet, it returns
        /// null. It may also return null in case of closed auction whose winner has been deleted from the site(after the auction
        /// ended).
        /// </summary>
        /// <returns></returns>
        public IUser? CurrentWinner() {
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var queryWinner = (from bid in c.Bids
                    join u in c.Users on bid.UserID equals u.UserID
                    where bid.AuctionID == Id
                    orderby bid.BidValue descending
                    select new { Bid = bid, User = u }).FirstOrDefault();

                if (queryWinner == null)
                    return null;

                return new User(queryWinner.User.Username, queryWinner.User.Password, SiteId, Site);
            }
        }

        public override bool Equals(object? o) {
           if(o == null || o.GetType() != GetType())
               return false;
           var obj = o as Auction;
           return obj!.Id == Id && obj.SiteId == SiteId;
        }

        public override int GetHashCode() {
            return SiteId.GetHashCode();
        }

        /// <summary>
        /// Disposes of the auction and all associated resources, if any.
        /// </summary>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public void Delete() {
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var queryAuction = from auction in c.Auctions
                    where auction.AuctionID == Id && auction.SiteID == SiteId
                    select auction;

                if (queryAuction.SingleOrDefault() == null)
                    throw new AuctionSiteInvalidOperationException("Auction is not valid");

                c.Auctions.Remove(queryAuction.SingleOrDefault()!);
                c.SaveChanges();
            }
        }

        /// <summary>
        /// Check the bid params before using them.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="offer"></param>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteArgumentException"></exception>
        /// <exception cref="AuctionSiteArgumentOutOfRangeException"></exception>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public void CheckBidParams(ISession session, double offer) {
            if (session == null)
                throw new AuctionSiteArgumentNullException("The session is already closed, please try to login.");

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, this.SiteId);

                var querySession =
                    (from sessionUser in c.Sessions
                        where sessionUser.SessionID.ToString() == session.Id
                        select sessionUser).SingleOrDefault();
                if (querySession == null)
                    throw new AuctionSiteArgumentException("The session is not valid.");
            }
            if (offer < 0)
                throw new AuctionSiteArgumentOutOfRangeException("The offer must be positive.");
            if (this.EndsOn < Site.Now())
                throw new AuctionSiteInvalidOperationException("The auction is already closed.");
            if (session.ValidUntil < Site.Now())
                throw new AuctionSiteArgumentException("The session is not valid anymore, please try again.");
            if (session.User.Equals(Seller))
                throw new AuctionSiteArgumentException("You cannot bid your auctions.");
        }
    }
}
