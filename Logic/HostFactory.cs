using Microsoft.Data.SqlClient;
using TAP22_23.AlarmClock.Interface;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {
    public class HostFactory : IHostFactory {

        /// <summary>
        /// Creates a new DB (dropping existing previous version, if any), and initialize it with all the necessary DB elements
        /// for the Host.
        /// </summary>
        public void CreateHost(string connectionString) {

            if (string.IsNullOrEmpty(connectionString))
                throw new AuctionSiteArgumentNullException("Cannot use null or empty string for connection.");
            
            try {
                using (var c = new ContextDB(connectionString)) {
                    c.Database.EnsureDeleted();
                    c.Database.EnsureCreated();
                }
            } catch (SqlException ex) {
                throw new AuctionSiteUnavailableDbException(
                    "connectionString is (non-null but) malformed, the DB server is not" +
                    "\r\nresponding or returns an unexpected error.\r\n", ex);
            }
        }

        /// <summary>
        /// Yields the Host managing a group of Sites having their data resident on the same database.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="alarmClockFactory"></param>
        /// <returns></returns>
        /// <exception cref="AuctionSiteArgumentNullException"></exception>
        /// <exception cref="AuctionSiteUnavailableDbException"></exception>
        public IHost LoadHost(string connectionString, IAlarmClockFactory alarmClockFactory) {
            if (string.IsNullOrEmpty(connectionString))
                throw new AuctionSiteArgumentNullException("Cannot use null or empty string for connection.");
            if (alarmClockFactory == null)
                throw new AuctionSiteArgumentNullException("Alarm Clock cannot be null.");

            try {
                using (var c = new ContextDB(connectionString)) {
                    Helpers.CanConnectToDb(c);
                    return new Host(connectionString, alarmClockFactory);
                }
            } catch (SqlException ex) {
                throw new AuctionSiteUnavailableDbException("Failed to connect to the DataBase.", ex);
            }
            

        }
    }
}