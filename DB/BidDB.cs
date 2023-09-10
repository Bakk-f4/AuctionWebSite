using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menghini {
    public class BidDB {
        [Key]
        public int BidID { get; set; }

        [Range(double.Epsilon, double.MaxValue)]
        public double BidValue { get; set; }
        public DateTime BidDate { get; set; }

        public UserDB User { get; set; }
        public int UserID { get; set; }

        public AuctionDB Auction { get; set; }
        public int AuctionID { get; set; }
    }
}
