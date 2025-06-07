using AspnetCoreMvcFull.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  options.UseSqlServer(connectionString);
});
builder.Services.AddDbContext<AppDbContext>(options =>
{
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  options.UseSqlServer(connectionString);
});


builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

var app = builder.Build();

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

// IMPORTANT: Add UseAuthentication() before UseAuthorization()
app.UseAuthentication(); // <--- THIS WAS MISSING
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.MapRazorPages();

// Role Seeding
using (var scope = app.Services.CreateScope())
{
  var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
  var roles = new[] { "Admin", "Manager", "User" };

  foreach (var role in roles)
  {
    if (!await roleManager.RoleExistsAsync(role))
      await roleManager.CreateAsync(new IdentityRole(role));
  }
}

// User Seeding
using (var scope = app.Services.CreateScope())
{
  var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

  // Admin user
  string adminEmail = "admin2@admin.com";
  string adminPassword = "Test1234,";
  if (await userManager.FindByEmailAsync(adminEmail) == null)
  {
    var adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
    await userManager.CreateAsync(adminUser, adminPassword);
    await userManager.AddToRoleAsync(adminUser, "Admin");
  }

  // Manager user
  string managerEmail = "manager@admin.com";
  string managerPassword = "Test1234,";
  if (await userManager.FindByEmailAsync(managerEmail) == null)
  {
    var managerUser = new IdentityUser { UserName = managerEmail, Email = managerEmail };
    await userManager.CreateAsync(managerUser, managerPassword);
    await userManager.AddToRoleAsync(managerUser, "Manager");
  }

  // Regular user
  string userEmail = "user@admin.com";
  string userPassword = "Test1234,";
  if (await userManager.FindByEmailAsync(userEmail) == null)
  {
    var regularUser = new IdentityUser { UserName = userEmail, Email = userEmail };
    await userManager.CreateAsync(regularUser, userPassword);
    await userManager.AddToRoleAsync(regularUser, "User");
  }
}

app.Run();
