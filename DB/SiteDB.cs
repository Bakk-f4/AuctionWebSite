using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {

    [Index(nameof(Name), IsUnique = true)]
    public class SiteDB {
        [Key]
        public int SiteID { get; set; }

        [MinLength(DomainConstraints.MinSiteName)]
        [MaxLength(DomainConstraints.MaxSiteName)]
        public string Name { get; set; }

        [Range(0, int.MaxValue)]
        public int SessionExpirationInSeconds { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double MinimumBidIncrement { get; set; }

        [Range(DomainConstraints.MinTimeZone, DomainConstraints.MaxTimeZone)]
        public int Timezone { get; set; }


        //chiavi esterne
        public ICollection<AuctionDB>? Auctions { get; set; }
        //public ICollection<UserDB>? Users { get; set; }
        public ICollection<SessionDB>? Sessions { get; set; }

    }
}
