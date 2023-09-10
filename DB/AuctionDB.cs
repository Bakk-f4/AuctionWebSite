using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menghini {
    public class AuctionDB
    {
        [Key]
        public int AuctionID { get; set; }
        public String Description { get; set; }
        public DateTime EndsOn { get; set; }
        
        public UserDB Seller { get; set; }
        public int SellerID { get; set; }
        public double StartingPrice { get; set; }

        public SiteDB Site { get; set; }
        public int SiteID { get; set; }

        public ICollection<BidDB>? ListOfBids { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double MaxOffer { get; set; }
        public int? CurrentWinnerID { get; set; }
        public UserDB? CurrentWinner { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double CurrentPrice { get; set; }




    }
}
