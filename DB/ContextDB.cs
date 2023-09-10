using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using TAP22_23.AuctionSite.Interface;


namespace Menghini {
    public class ContextDB : TapDbContext {

        public DbSet<AuctionDB> Auctions { get; set; }
        public DbSet<UserDB> Users { get; set; }
        public DbSet<BidDB> Bids { get; set; }
        public DbSet<SessionDB> Sessions { get; set; }
        public DbSet<SiteDB> Sites { get; set; }


        private readonly string _connectionString;
        
        public ContextDB(string connectionString) : base(new DbContextOptionsBuilder<ContextDB>().Options) {
            _connectionString = connectionString;
        }
        

        protected override void OnConfiguring(DbContextOptionsBuilder options) {
            options.UseSqlServer(_connectionString);
            base.OnConfiguring(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            var user = modelBuilder.Entity<UserDB>();
            var auction = modelBuilder.Entity<AuctionDB>();
            var session = modelBuilder.Entity<SessionDB>();
            var bid = modelBuilder.Entity<BidDB>();

            user
                .HasOne(u => u.Session)
                .WithOne(s => s.User)
                .HasForeignKey<UserDB>(u => u.SessionID)
                .OnDelete(DeleteBehavior.SetNull);

            user
                .HasMany(u => u.Auctions)
                .WithOne(g => g.Seller)
                .OnDelete(DeleteBehavior.NoAction);

            user
                .HasMany(u => u.Bids)
                .WithOne(b => b.User)
                .HasForeignKey(u => u.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            bid
                .HasOne(b => b.User)
                .WithMany(u => u.Bids)
                .HasForeignKey(b => b.UserID)
                .OnDelete(DeleteBehavior.ClientCascade);

            session
                .HasOne(sess => sess.Site)
                .WithMany(site => site.Sessions)
                .HasForeignKey(s => s.SiteID)
                .OnDelete(DeleteBehavior.ClientCascade)
                .IsRequired();

            auction
                .HasOne(a => a.Site)
                .WithMany(s => s.Auctions)
                .HasForeignKey(a => a.SiteID)
                .OnDelete(DeleteBehavior.ClientCascade)
                .IsRequired();

            auction
                .HasMany(a => a.ListOfBids)
                .WithOne(b => b.Auction)
                .OnDelete(DeleteBehavior.NoAction);

            auction
                .HasOne(s => s.Seller)
                .WithMany(a => a.Auctions)
                .HasForeignKey(s => s.SellerID)
                .OnDelete(DeleteBehavior.ClientCascade)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges() {
            try {
                return base.SaveChanges();
            }
            catch (SqlException e) {
                throw new AuctionSiteUnavailableDbException("Cannot establish connection with the database.", e);
            }
            catch (DbUpdateConcurrencyException e) {
                throw new AuctionSiteConcurrentChangeException(
                    "Error during an attempt to save an entity that has been modified concurrently.", e);
            }
            catch (DbUpdateException e) {
                var aux = e.InnerException as SqlException;
                if(aux == null)
                    throw new AuctionSiteInvalidOperationException("Inner exception value was not supplied.", e);
                switch (aux.Number) {
                    case 2601:
                        throw new AuctionSiteNameAlreadyInUseException(null, "The site name is already used.", aux);
                    case 547:
                        throw new AuctionSiteInvalidOperationException("Foreign key error", e); 
                    default:
                        throw new AuctionSiteInvalidOperationException("Default SQL error: " + aux.Number + " : " + aux.Message, e);
                }
            }
        }
    }


}