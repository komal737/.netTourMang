using TourManagement_MVC.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using TourManagement_MVC.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Register TourService with MongoDB
// Register tour service
builder.Services.AddScoped<ITourService, TourService>();
// Email service (SendGrid preferred, MailKit SMTP fallback)
builder.Services.AddScoped<IEmailService, EmailService>();

// Register password hasher (used by TourService) and cookie authentication
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<TourManagement_MVC.Models.User>, Microsoft.AspNetCore.Identity.PasswordHasher<TourManagement_MVC.Models.User>>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

// Configure distributed cache for session storage. Prefer Redis when configured, fall back to
// SQL Server if a DefaultConnection exists, otherwise use in-memory (not recommended for prod).
var redisConfig = builder.Configuration["Redis:Configuration"];
var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrEmpty(redisConfig))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConfig;
    });
    // Register health checks (generic). Specific Redis/SQL health-check extensions
    // are intentionally omitted to avoid extra health-check package dependencies.
    builder.Services.AddHealthChecks();
}
else if (!string.IsNullOrEmpty(sqlConn))
{
    builder.Services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = sqlConn;
        options.SchemaName = "dbo";
        options.TableName = "Sessions";
    });
    builder.Services.AddHealthChecks();
}
else
{
    // Development fallback; not for scalable/production usage.
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddHealthChecks();
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Expose health endpoint
app.MapHealthChecks("/health");

// Add basic request logging middleware using ILogger
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>()
        .CreateLogger("RequestLogger");
    logger.LogInformation("Handling {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
    logger.LogInformation("Finished {Method} {Path} with {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
