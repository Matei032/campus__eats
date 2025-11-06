using CampusEats.Backend.Domain;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Backend.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    //public DbSet<Order> Orders { get; set; }
    //public DbSet<User> Users { get; set; }
    
}