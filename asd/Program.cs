using Menghini;
using TAP22_23.AuctionSite.Interface;
using TAP22_23.AlarmClock.Interface;


namespace Menghini {
    public class Class1 {

        static void Main(string[] args) {

            using (var c = new ContextDB(@"..\..\..\..\OriginalReferences\TestConfig.txt")) {
                c.Database.EnsureDeleted();
                c.Database.EnsureCreated();
            }

            
            Console.WriteLine("Hello world");


            var a = new List<string>() { "ciao" };
            IEnumerable<int> AUX = new List<int>();


        }

    }
}