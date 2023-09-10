using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class Host : IHost {

        public string ConnectionString { get; }
        public IAlarmClockFactory AlarmClockFactory { get; }


        public Host(string connectionString, IAlarmClockFactory alarmClockFactory) {
            ConnectionString = connectionString;
            AlarmClockFactory = alarmClockFactory;
        }

        /// <summary>
        /// Create a new site, identified by its name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timezone"></param>
        /// <param name="sessionExpirationTimeInSeconds"></param>
        /// <param name="minimumBidIncrement"></param>
        public void CreateSite(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            checkSiteParams(name, timezone, sessionExpirationTimeInSeconds, minimumBidIncrement);
            using (var c = new ContextDB(ConnectionString)) {

                Helpers.CanConnectToDb(c);

                var site = new SiteDB() {
                    Name = name,
                    Timezone = timezone,
                    SessionExpirationInSeconds = sessionExpirationTimeInSeconds,
                    MinimumBidIncrement = minimumBidIncrement
                };
                c.Sites.Add(site);
                c.SaveChanges();
            }
        }

        /// <summary>
        /// Yields the names and corresponding time zones of managed sites.
        /// </summary>
        /// <returns>
        /// The names of the managed sites and their time zones.
        /// </returns>
        public IEnumerable<(string Name, int TimeZone)> GetSiteInfos() {
            var aux = new List<(string Name, int TimeZone)>();
            using (var c = new ContextDB(ConnectionString)) {
                Helpers.CanConnectToDb(c);
                //load only name and timezone from sites
                var query = from site in c.Sites
                    select new { name = site.Name, timezone = site.Timezone };
                //for each site, add name and timezone
                foreach (var q in query)
                    aux.Add((q.name, q.timezone));
                return aux;
            }
        }

        /// <summary>
        /// Yields the ISite object corresponding to an existing Site.
        /// </summary>
        /// <param name="name"></param>
        /// <returns>A new instance for the site.</returns>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteArgumentException"></exception>
        /// <exception cref="AuctionSiteInexistentNameException"></exception>
        public ISite LoadSite(string name) {
            if (name == null)
                throw new AuctionSiteArgumentNullException("The name of the website cannot be null.");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException("The site name is too short or too long");

            using (var c = new ContextDB(ConnectionString)) {

                Helpers.CanConnectToDb(c);

                var query = from Site in c.Sites where Site.Name == name select Site;

                if (query.SingleOrDefault() == null)
                    throw new AuctionSiteInexistentNameException($"The website name: {name} is not present in the DB.");

                var id = query.Single().SiteID;
                var timezone = query.Single().Timezone;
                var sessionExpirationInSeconds = query.Single().SessionExpirationInSeconds;
                var minimumBidIncrement = query.Single().MinimumBidIncrement;

                var alarmClock = AlarmClockFactory.InstantiateAlarmClock(timezone);

                return new Site(id, name, timezone, sessionExpirationInSeconds, minimumBidIncrement, ConnectionString, alarmClock);
            }
        }

        /// <summary>
        /// Helper function to check the params for CreateSite function.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="timezone"></param>
        /// <param name="sessionExpirationTimeInSeconds"></param>
        /// <param name="minimumBidIncrement"></param>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteArgumentException"></exception>
        /// <exception cref="AuctionSiteArgumentOutOfRangeException"></exception>
        private void checkSiteParams(string name, int timezone, int sessionExpirationTimeInSeconds, double minimumBidIncrement) {
            if (name == null)
                throw new AuctionSiteArgumentNullException("Name of the site is null.");
            if (name.Length < DomainConstraints.MinSiteName || name.Length > DomainConstraints.MaxSiteName)
                throw new AuctionSiteArgumentException($"The site name: {name} is too short or too long.");
            if (timezone < DomainConstraints.MinTimeZone || timezone > DomainConstraints.MaxTimeZone ||
                sessionExpirationTimeInSeconds < 0 || minimumBidIncrement < 0)
                throw new AuctionSiteArgumentOutOfRangeException(
                    "One of those values are out of range: Timezone, sessionExpirationTimeInSeconds, minimumBidIncrement");
        }
    }
}
