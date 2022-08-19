using Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class StormATPContext : DbContext
    {
        public StormATPContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<AccountQueries> AccountQueries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            SetupIndexes(modelBuilder);
            SetupColumnTypes(modelBuilder);
        }

        private static void SetupIndexes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.Reference);

            modelBuilder.Entity<Payment>()
                .HasIndex(x => x.Finished)
                .IncludeProperties(x => x.Status);
        }

        private static void SetupColumnTypes(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}