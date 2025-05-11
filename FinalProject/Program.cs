using Microsoft.EntityFrameworkCore;
using FinalProject.Data; // Make sure this namespace matches your DbContext location
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for CookieAuthenticationDefaults
using FinalProject.Configuration;
using FinalProject.Services;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(); // MVC  controllers and views
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // "cookieAuth" for members
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => // Config Member scheme ("CookieAuth")
{
    options.Cookie.Name = "FinalProject.Member"; // custom cookie for members
    options.LoginPath = "/Member/Login"; // member login page
    options.AccessDeniedPath = "/Member/AccessDenied"; // Path for member access denied
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Member cookie expiration time
    options.SlidingExpiration = true; // Renew member cookie on activity
})
.AddCookie("AdminCookieAuth", options => // Admin cookie auth name
{
    options.Cookie.Name = "FinalProject.Admin"; // custom cookie for auth
    options.LoginPath = "/Admin/Login"; // login page
    options.AccessDeniedPath = "/Admin/AccessDenied"; // access denied page
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // admin cookie expiration
    options.SlidingExpiration = true; // Renew admin cookie on activity
});


builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("DefaultConnection connection string is not configured in appsettings.json!");
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseMySql(connectionString,
                         ServerVersion.AutoDetect(connectionString),
                         mySqlOptions =>
                         {
                         })
    );
}


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection(); 
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization(); 



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
