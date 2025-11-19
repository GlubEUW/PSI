using Microsoft.EntityFrameworkCore;
using Api.Entities;
using Api.Models;

namespace Api.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
   public DbSet<RegisteredUser> Users { get; set; } = null!;
   public DbSet<GameStatsDto> GameStats { get; set; } = null!;

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      modelBuilder.Entity<GameStatsDto>()
         .HasKey(gs => gs.UserId);
   }

}
