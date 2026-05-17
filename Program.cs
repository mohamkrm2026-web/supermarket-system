using SuperMarket.Data;
using SuperMarket.Middleware;
using SuperMarket.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// السماح للموقع الخارجي يتصل بالـ API
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebsitePolicy", policy =>
    {
        policy
            // ضع هنا رابط موقعك لما تعمله — أو اتركه * للتطوير
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=supermarket.db"));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseStaticFiles();
app.UseCors("WebsitePolicy");
app.UseMiddleware<LicenseMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
