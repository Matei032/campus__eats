using CampusEats.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CampusEats.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }
}