using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;

namespace api.Data
{
    public class ApplicationDBContextFactory : IDesignTimeDbContextFactory<ApplicationDBContext>
    {
        public ApplicationDBContext CreateDbContext(string[] args)
        {
            Console.WriteLine("⚙️  Creating ApplicationDBContext at design-time...");

            var connectionString = "Server=(localdb)\\MSSQLLocalDB;Database=finshark;Trusted_Connection=True;MultipleActiveResultSets=true";
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("❌ Connection string is null or empty!");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDBContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDBContext(optionsBuilder.Options);
        }
    }
}
