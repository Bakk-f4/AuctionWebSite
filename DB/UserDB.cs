using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAP22_23.AuctionSite.Interface;

namespace Menghini {

    [Index(nameof(Username), nameof(SiteId), IsUnique = true)]
    public class UserDB {
        [Key]
        public int UserID { get; set; }

        [MinLength(DomainConstraints.MinUserName)]
        [MaxLength(DomainConstraints.MaxUserName)]
        public string Username { get; set; }

        [MinLength(DomainConstraints.MinUserPassword)]
        public string Password { get; set; }

        public int SiteId { get; set; }
        //public SiteDB Site { get; set; }

        public int? SessionID { get; set; }
        public SessionDB? Session { get; set; }

        //lista di offerte
        public ICollection<BidDB> Bids { get; set; }

        //lista di auction
        public ICollection<AuctionDB> Auctions { get; set; }

    }
}
