using System.Collections;
using Microsoft.EntityFrameworkCore;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class Site : ISite {
        /// <summary>
        /// The name of the auction site.
        /// </summary>
        public string Name { get; }

        public int SiteId { get; }

        /// <summary>
        /// The timezone of the auction site.
        /// </summary>
        public int Timezone { get; }

        /// <summary>
        /// The number of seconds needed for the session of an idle user to time out. A positive number
        /// </summary>
        public int SessionExpirationInSeconds { get; }

        /// <summary>
        /// The minimum amount allowed as increment (from the starting price) for a bid. A positive number
        /// </summary>
        public double MinimumBidIncrement { get; }
        public string ConnectionString { get; }
        
        private readonly IAlarmClock _alarmClock;
        private IAlarm _alarm;

        /// <summary>
        /// Site constructor
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="name"></param>
        /// <param name="timezone"></param>
        /// <param name="sessionExpirationInSeconds"></param>
        /// <param name="minimumBidIncrement"></param>
        /// <param name="connectionString"></param>
        /// <param name="alarmClock"></param>
        public Site(int siteId, string name, int timezone, int sessionExpirationInSeconds, double minimumBidIncrement, string connectionString, IAlarmClock alarmClock) {
            SiteId = siteId;
            Name = name;
            Timezone = timezone;
            SessionExpirationInSeconds = sessionExpirationInSeconds;
            MinimumBidIncrement = minimumBidIncrement;
            ConnectionString = connectionString;

            _alarmClock = alarmClock;
            _alarm = _alarmClock.InstantiateAlarm(300000);
            _alarm.RingingEvent += DeleteExpiredSessions;

        }

        /// <summary>
        /// Add a user of the site.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void CreateUser(string username, string password) {
            checkUserParams(username, password);
            using (var c = new ContextDB(ConnectionString)) {
                
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var user = new UserDB() {
                    Username = username,
                    Password = Helpers.HashPassword(password),
                    SiteId = SiteId
                };
                c.Users.Add(user);
                c.SaveChanges();
            }
        }

        /// <summary>
        /// Disposes of the site and all its associated resources.
        /// </summary>
        /// <exception cref="AuctionSiteInvalidOperationException"></exception>
        public void Delete() {
            using (var c = new ContextDB(ConnectionString)) {
                
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var query = from site in c.Sites
                    where site.SiteID == SiteId
                    select site;

                if (query.SingleOrDefault() == null)
                    throw new AuctionSiteInvalidOperationException("Cannot delete an nonexistent site");

                c.Sites.Remove(query.SingleOrDefault()!);
                c.SaveChanges();
            }
        }

        /// <summary>
        /// Yields the session for the user, new iff no valid session for him/her exists. No user can have two valid sessions on
        /// the same site at the same time.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>The session for the user or null if username and password do not correspond to a user of the site.</returns>
        public ISession? Login(string username, string password) {
            checkUserParams(username, password);

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);
                //check if user exist in this siteId
                var queryUser = (from user in c.Users
                    where user.Username == username && user.SiteId == SiteId
                    select user).SingleOrDefault();
                //if not exist or password don't match
                if (queryUser == null ||
                    !Helpers.VerifyHashPassword(queryUser.Password, password))
                    return null;

                var userObj = new User(queryUser.Username, queryUser.Password, queryUser.SiteId, this);

                //check if exist any session with siteid and userid
                //means user was already logged in
                var querySession = (from session in c.Sessions
                    where session.SiteID == SiteId && session.UserID == queryUser.UserID
                    select session).SingleOrDefault();

                //if query do exist...
                if (querySession != null) {
                    //check if session is still valid...
                    if (querySession.ValidUntil < Now()) {
                        c.Sessions.Remove(querySession);
                        c.SaveChanges();
                    } else {
                        //session time is valid
                        querySession.ValidUntil = Now().AddSeconds(SessionExpirationInSeconds);
                        c.SaveChanges();
                        return new Session(querySession.SessionID.ToString(), querySession.ValidUntil,
                            userObj, querySession.SiteID, this);
                    }
                }
                //recreate a new session
                var updatedSession = new SessionDB {
                    User = queryUser,
                    SiteID = SiteId,
                    UserID = queryUser.UserID,
                    ValidUntil = Now().AddSeconds(SessionExpirationInSeconds)
                };
                c.Sessions.Add(updatedSession);
                c.SaveChanges();
                return new Session(updatedSession.SessionID.ToString(), updatedSession.ValidUntil, userObj, SiteId, this);
            }
        }

        /// <summary>
        /// Returns the current time
        /// </summary>
        /// <returns>The current time, as provided by the internal IAlarmClock</returns>
        public DateTime Now() {
            return _alarmClock.Now;
        }

        /// <summary>
        /// Yields all the (not yet ended) auctions of the site. In a realistic example, this method would be more complex, using
        /// some sort of pagination.
        /// </summary>
        /// <param name="onlyNotEnded"></param>
        /// <returns>If not onlyNotEnded , all the auctions, otherwise only those not yet ended.</returns>
        public IEnumerable<IAuction> ToyGetAuctions(bool onlyNotEnded) {
            var aux = new List<IAuction>();

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                IEnumerable<AuctionDB> queryAuctions;

                if(onlyNotEnded) {
                    queryAuctions = (from a in c.Auctions
                        where a.EndsOn > Now() && a.SiteID == SiteId
                        select a).Include(a => a.Seller);
                } else {
                    queryAuctions = (from a in c.Auctions
                        where a.SiteID == SiteId 
                        select a).Include(a => a.Seller);
                }

                foreach (var q in queryAuctions) {
                    var newUser = new User(q.Seller.Username, q.Seller.Password, SiteId, this);
                    var newAuction = new Auction(q.AuctionID, newUser, q.Description , q.EndsOn, SiteId, ConnectionString, this);
                    aux.Add(newAuction);
                }
            }
            return aux;
        }

        /// <summary>
        /// Yields all the sessions of the site.
        /// </summary>
        /// <returns>All the sessions of the site.</returns>
        public IEnumerable<ISession> ToyGetSessions() {
            var aux = new List<ISession>();

            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var querySession = (from s in c.Sessions
                    join u in c.Users on s.UserID equals u.UserID
                    where s.SiteID == SiteId
                    select new { Session = s, User = u }).ToList();

                foreach (var q in querySession) {
                    var user = new User(q.User.Username, q.User.Password, SiteId, this);
                    var session = new Session(q.Session.SessionID.ToString(), q.Session.ValidUntil, user, SiteId, this);
                    aux.Add(session);
                }
            }
            return aux;
        }

        /// <summary>
        /// Yields all the users of the site.
        /// </summary>
        /// <returns>All the users of the site.</returns>
        public IEnumerable<IUser> ToyGetUsers() {
            var aux = new List<IUser>();
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);
                var queryUsers = from user in c.Users where user.SiteId == SiteId select user;
                foreach (var q in queryUsers) {
                    var user = new User(q.Username, q.Password , q.SiteId, this);
                    aux.Add(user);
                }
            }
            return aux;
        }

        /// <summary>
        /// Delete the expired sessions
        /// </summary>
        private void DeleteExpiredSessions() {

            using (var c = new ContextDB(ConnectionString)) {

                //TODO controllare se mi sono effettivamente connesso al DB?!?
                Helpers.CanConnectToDb(c);
                Helpers.CheckWebSite(c, SiteId);

                var querySession = from session in c.Sessions
                    where session.SiteID == SiteId
                    where session.ValidUntil < Now()
                    select session;

                foreach (var q in querySession) 
                    c.Sessions.Remove(q);
                c.SaveChanges();
            }
            _alarm = _alarmClock.InstantiateAlarm(300_000);
        }

        /// <summary>
        /// Check the user parameters and throw the corresponding exception.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteArgumentException"></exception>
        private void checkUserParams(string username, string password) {
            if (username == null || password == null)
                throw new AuctionSiteArgumentNullException("Username or Password are null.");
            if (username.Length < DomainConstraints.MinUserName || username.Length > DomainConstraints.MaxUserName)
                throw new AuctionSiteArgumentException($"Username: {username} is too short or too long.");
            if (password.Length < DomainConstraints.MinUserPassword)
                throw new AuctionSiteArgumentException("The chosen password is too short");
        }

        public override bool Equals(object? obj) {
            if (obj == null || obj.GetType() != GetType())
                return false;
            var otherSite = obj as Site;
            return otherSite!.Name.Equals(Name);
        }

        public override int GetHashCode() {
            return Name.GetHashCode();
        }
    }
}
