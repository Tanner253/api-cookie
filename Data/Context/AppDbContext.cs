using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Api.Data.Context
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
              
        }

        //create player data table
        public DbSet<PlayerData> PlayerDatas { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Call the base method first

            // --- Seed Data ---
            modelBuilder.Entity<PlayerData>().HasData(
                new PlayerData
                {
                    Id = 1, // Explicitly set the ID for seed data
                    GoldNuggets = 100m, // Example value (use 'm' suffix for decimal)
                    LastSavedTimestampTicks = 2000 // Example value
                }
            // You can add more PlayerData objects here separated by commas
            );
        }

    }
}
