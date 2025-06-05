using AspnetCoreMvcFull.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpContextAccessor();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ProjDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
  .AddRoles<IdentityRole>()
  .AddEntityFrameworkStores<ProjDbContext>();

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddRazorPages();

builder.Services.AddSession(); // Add session services

// Add controllers with views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Role seeding
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  await CreateRoles(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.Use(async (context, next) =>
//{
//    if (context.Request.Path == "/")
//    {
//        context.Response.Redirect("/Identity/Account/Login");
//        return;
//    }
//    await next();
//});

app.UseAuthentication();
app.UseAuthorization();

app.UseSession(); // Add session middleware before UseEndpoints or UseRouting

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Account}/{action=Login}/{id?}");

//app.MapRazorPages();
//app.Run();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();

static async Task CreateRoles(IServiceProvider serviceProvider)
{
  var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
  string[] roles = { "Admin", "Manager", "User", "SuperAdmin" };

  foreach (var role in roles)
  {
    if (!await roleManager.RoleExistsAsync(role))
      await roleManager.CreateAsync(new IdentityRole(role));
  }
}

