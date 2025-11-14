using Microsoft.EntityFrameworkCore;
using Api.Entities;

namespace Api.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
   public DbSet<RegisteredUser> Users { get; set; } = null!;
}
