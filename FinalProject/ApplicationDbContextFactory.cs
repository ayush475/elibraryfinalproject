using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // Needed for MySQL specific configurations
using FinalProject.Data; // Make sure this namespace matches your DbContext location

// This factory is needed by the EF Core design-time tools (like 'dotnet ef')
// to create an instance of the DbContext when scaffolding migrations.
// It ensures the tools can access the application's configuration,
// specifically the database connection string.
namespace FinalProject // Use your project's root namespace or a suitable common namespace
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        // Creates a new instance of the DbContext for design-time operations.
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Build configuration from appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // Set the base path to the project directory
                .AddJsonFile("appsettings.json") // Add your appsettings.json file
                .Build();

            // Get the connection string from configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Create DbContextOptions using the MySQL provider and connection string
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Configure the DbContext to use MySQL with the specified connection string
            // ServerVersion.AutoDetect is a convenient way to let Pomelo figure out the MySQL version
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            // Create and return a new instance of the ApplicationDbContext
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
