using Microsoft.EntityFrameworkCore;
using FinalProject.Data; // Make sure this namespace matches your DbContext location
using Pomelo.EntityFrameworkCore.MySql.Infrastructure; // Needed for MySQL specific configurations
using Microsoft.Extensions.DependencyInjection; // Added for IServiceCollection and related extensions
using Microsoft.Extensions.Hosting; // Added for IHostEnvironment
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Microsoft.Extensions.Logging; // Added for ILogger

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // This registers MVC controllers and views
builder.Services.AddAuthentication("CookieAuth") // "CookieAuth" is the default scheme name
    .AddCookie("CookieAuth", options =>
    {
        options.Cookie.Name = "FinalProject"; // Set a custom cookie name
        options.LoginPath = "/Members/Login"; // Path to your login page
        options.AccessDeniedPath = "/Members/AccessDenied"; // Path for access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Cookie expiration time
        options.SlidingExpiration = true; // Renew cookie on activity
    });

// Add Authorization services
builder.Services.AddAuthorization();

// --- Register ApplicationDbContext with Dependency Injection ---
// Get the MySQL connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check if the connection string was found (good practice)
if (string.IsNullOrEmpty(connectionString))
{
    // Log an error or throw an exception if the connection string is missing
    // For now, we'll just print to console (you should use a logger in a real app)
    Console.WriteLine("DefaultConnection connection string is not configured in appsettings.json!");
    // Optionally, you could throw an exception here to prevent the app from starting
    // throw new InvalidOperationException("Database connection string is not configured.");
}
else
{
    // Add the ApplicationDbContext to the services
    // Configure it to use the Pomelo MySQL provider
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
                         // ServerVersion.AutoDetect attempts to determine the MySQL server version.
                         // You can explicitly specify the version if needed, e.g., new MySqlServerVersion(new Version(8, 0, 21))
                         ServerVersion.AutoDetect(connectionString),
                         // Optional: Configure MySQL specific options here
                         mySqlOptions =>
                         {
                             // mySqlOptions.EnableRetryOnFailure(); // Example: Enable retries for transient errors
                         })
        // Optional: Enable sensitive data logging in development for debugging SQL queries
        // .EnableSensitiveDataLogging() // Only in Development!
        // Optional: Enable detailed errors
        // .EnableDetailedErrors()
    );
}
// --- End Register ApplicationDbContext ---


var app = builder.Build();

// Configure the HTTP request pipeline.
// In Development, you might want to use the DeveloperExceptionPage
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection(); // Added HSTS for production, add HttpsRedirection
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization(); // Add authorization middleware if you implement security

// Configure endpoint routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
