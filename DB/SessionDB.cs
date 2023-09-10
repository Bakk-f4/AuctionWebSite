using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Menghini {
    public class SessionDB {
        [Key]
        public int SessionID { get; set; }

        public DateTime ValidUntil { get; set; }

        //chiave esterna
        public UserDB User { get; set; }
        public int UserID { get; set; }
        public SiteDB Site { get; set; }
        public int SiteID { get; set; }
    }
}
