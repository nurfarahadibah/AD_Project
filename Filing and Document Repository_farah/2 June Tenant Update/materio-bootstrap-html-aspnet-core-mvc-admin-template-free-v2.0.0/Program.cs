using AspnetCoreMvcFull.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models; // Make sure this is here for ApplicationUser and Tenant

var builder = WebApplication.CreateBuilder(args);

// Remove this line - AddHttpContextAccessor() below is sufficient and preferred.
// builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure ApplicationDbContext for Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  options.UseSqlServer(connectionString);
});

// Configure AppDbContext for your application data (ComplianceFolders, etc.)
builder.Services.AddDbContext<AppDbContext>(options =>
{
  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
  options.UseSqlServer(connectionString);
});


// --- CRITICAL FIX HERE: Change IdentityUser to ApplicationUser ---
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // Add .AddRoles<IdentityRole>() if you are using roles
    .AddEntityFrameworkStores<ApplicationDbContext>(); // Link to your ApplicationDbContext

builder.Services.AddRazorPages();

builder.Services.AddHttpContextAccessor(); // This one is sufficient for IHttpContextAccessor
builder.Services.AddScoped<ITenantService, TenantService>();

var app = builder.Build();

// --- Data Seeding for Application Data (Tenants, Categories, Folders, etc.) ---
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  try
  {
    var context = services.GetRequiredService<AppDbContext>();
    // Apply pending migrations for AppDbContext if any. This is important before seeding.
    await context.Database.MigrateAsync();
    // Call your static SeedData method
    await DataSeeder.SeedData(context);
  }
  catch (Exception ex)
  {
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the application database.");
  }
}
// --- END OF APPLICATION DATA SEEDER CALL ---


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Add UseAuthentication() before UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.MapRazorPages();


// --- Role Seeding ---
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

// --- User Seeding (Now with ApplicationUser and TenantId) ---
using (var scope = app.Services.CreateScope())
{
  // This will now correctly resolve UserManager<ApplicationUser>
  var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

  // Admin user for Tenant A
  string adminEmailA = "adminA@example.com";
  string adminPasswordA = "Test1234,";
  if (await userManager.FindByEmailAsync(adminEmailA) == null)
  {
    var adminUserA = new ApplicationUser { UserName = adminEmailA, Email = adminEmailA, TenantId = "tenantA" };
    await userManager.CreateAsync(adminUserA, adminPasswordA);
    await userManager.AddToRoleAsync(adminUserA, "Admin");
  }



  // Manager user for Tenant A
  string managerEmailA = "managerA@example.com";
  string managerPasswordA = "Test1234,";
  if (await userManager.FindByEmailAsync(managerEmailA) == null)
  {
    var managerUserA = new ApplicationUser { UserName = managerEmailA, Email = managerEmailA, TenantId = "tenantA" };
    await userManager.CreateAsync(managerUserA, managerPasswordA);
    await userManager.AddToRoleAsync(managerUserA, "Manager");
  }

  // Regular user for Tenant B
  string userEmailB = "userB@example.com";
  string userPasswordB = "Test1234,";
  if (await userManager.FindByEmailAsync(userEmailB) == null)
  {
    var regularUserB = new ApplicationUser { UserName = userEmailB, Email = userEmailB, TenantId = "tenantB" };
    await userManager.CreateAsync(regularUserB, userPasswordB);
    await userManager.AddToRoleAsync(regularUserB, "User");
  }
  // Admin user for Tenant B (NEW ADDITION)
  string adminEmailB = "adminB@example.com";
  string adminPasswordB = "Test1234,"; // Consider using a stronger password in production
  if (await userManager.FindByEmailAsync(adminEmailB) == null)
  {
    var adminUserB = new ApplicationUser { UserName = adminEmailB, Email = adminEmailB, TenantId = "tenantB" };
    await userManager.CreateAsync(adminUserB, adminPasswordB);
    await userManager.AddToRoleAsync(adminUserB, "Admin");
  }
}

app.Run();
