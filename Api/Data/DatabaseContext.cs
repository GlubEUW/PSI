using Microsoft.EntityFrameworkCore;

using Api.Entities;

namespace Api.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
   public DbSet<User> Users { get; set; } = null!;
   public DbSet<Tournament> Tournaments { get; set; } = null!;
   public DbSet<Round> Rounds { get; set; } = null!;
   public DbSet<UserRound> UserRounds { get; set; } = null!;
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<User>()
          .HasDiscriminator<string>("Discriminator")
          .HasValue<RegisteredUser>("RegisteredUser")
          .HasValue<Guest>("Guest");

      modelBuilder.Entity<UserRound>()
       .HasKey(ur => new { ur.UserId, ur.RoundId });
   }

}
