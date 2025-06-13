using AspnetCoreMvcFull.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models; // Make sure this is here for ApplicationUser
using System; // For TimeSpan

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews();

// Configure ApplicationDbContext for Identity (using ApplicationUser)
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

// Configure ASP.NET Core Identity with ApplicationUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // Ensure roles are enabled
    .AddEntityFrameworkStores<ApplicationDbContext>(); // Link to your ApplicationDbContext

builder.Services.AddRazorPages();

builder.Services.AddScoped<ITenantService, TenantService>(); // Your custom tenant service
// Uncomment this if you have an EmailSender implementation in AspnetCoreMvcFull.Services
//builder.Services.AddTransient<IEmailSender, EmailSender>(); // Register IEmailSender

// --- FIX: Add Session services before builder.Build() ---
builder.Services.AddSession(options =>
{
  options.IdleTimeout = TimeSpan.FromMinutes(30); // Set your desired timeout
  options.Cookie.HttpOnly = true;
  options.Cookie.IsEssential = true;
});
// --- END Service Registration ---

var app = builder.Build(); // Service collection is now built and locked

// --- HTTP Request Pipeline Configuration ---
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// IMPORTANT: Add UseSession() here, after UseRouting() and before UseAuthentication/UseAuthorization
app.UseSession(); // Correct placement for UseSession

app.UseAuthentication();
app.UseAuthorization();

// Map default controller route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}"); // Changed default to Dashboards

app.MapRazorPages();
// --- END HTTP Request Pipeline Configuration ---


// --- Data Seeding (including App Data, Roles, and Users) ---
// This should ideally run after the application services are built and before app.Run()
using (var scope = app.Services.CreateScope())
{
  var services = scope.ServiceProvider;
  try
  {
    // Migrate and Seed AppDbContext data
    var appDbContext = services.GetRequiredService<AppDbContext>();
    await appDbContext.Database.MigrateAsync();
    await DataSeeder.SeedData(appDbContext); // Assuming DataSeeder.SeedData handles app data

    // Seed Roles
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Manager", "User", "SuperAdmin" }; // Include SuperAdmin from second Program.cs

    foreach (var role in roles)
    {
      if (!await roleManager.RoleExistsAsync(role))
        await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Seed Users (using ApplicationUser)
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Add the new default admin user here
    // --- NEW ADMIN USER SEEDING ---
    var superadminEmail = "aleysha18062003@gmail.com";
    var superadminUser = await userManager.FindByEmailAsync(superadminEmail);
    if (superadminUser == null)
    {
      superadminUser = new ApplicationUser
      {
        UserName = superadminEmail,
        Email = superadminEmail,
        EmailConfirmed = true, // to avoid confirmation step
                               // Consider assigning a default TenantId if this admin doesn't belong to specific tenants
                               // e.g., for a super admin that can manage multiple tenants, or a dedicated "system" tenant.
                               // For now, I'll leave it without TenantId as it was in your snippet, assuming it's a global admin.
                               // If this admin is tied to a specific tenant (e.g., Tenant A), you'd add: TenantId = "tenantA"
      };

      var result = await userManager.CreateAsync(superadminUser, "Aleysha1*");
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(superadminUser, "SuperAdmin");
        Console.WriteLine($"Default superadmin user '{superadminEmail}' created and assigned 'SuperAdmin' role.");
      }
      else
      {
        Console.WriteLine($"Failed to create default admin user '{superadminEmail}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
      }
    }
    else
    {
      Console.WriteLine($"Default admin user '{superadminEmail}' already exists.");
    }
    // --- END NEW ADMIN USER SEEDING ---


    // Admin user for Tenant A (Existing)
    string adminEmailA = "adminA@example.com";
    string adminPasswordA = "Test1234,";
    if (await userManager.FindByEmailAsync(adminEmailA) == null)
    {
      var adminUserA = new ApplicationUser { UserName = adminEmailA, Email = adminEmailA, TenantId = "tenantA" };
      var result = await userManager.CreateAsync(adminUserA, adminPasswordA);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(adminUserA, "Admin");
      }
      else { Console.WriteLine($"Failed to create adminUserA: {string.Join(", ", result.Errors.Select(e => e.Description))}"); }
    }

    // Manager user for Tenant A (Existing)
    string managerEmailA = "managerA@example.com";
    string managerPasswordA = "Test1234,";
    if (await userManager.FindByEmailAsync(managerEmailA) == null)
    {
      var managerUserA = new ApplicationUser { UserName = managerEmailA, Email = managerEmailA, TenantId = "tenantA" };
      var result = await userManager.CreateAsync(managerUserA, managerPasswordA);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(managerUserA, "Manager");
      }
      else { Console.WriteLine($"Failed to create managerUserA: {string.Join(", ", result.Errors.Select(e => e.Description))}"); }
    }

    // Regular user for Tenant B (Existing)
    string userEmailB = "userB@example.com";
    string userPasswordB = "Test1234,";
    if (await userManager.FindByEmailAsync(userEmailB) == null)
    {
      var regularUserB = new ApplicationUser { UserName = userEmailB, Email = userEmailB, TenantId = "tenantB" };
      var result = await userManager.CreateAsync(regularUserB, userPasswordB);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(regularUserB, "User");
      }
      else { Console.WriteLine($"Failed to create regularUserB: {string.Join(", ", result.Errors.Select(e => e.Description))}"); }
    }

    // Admin user for Tenant B (Existing)
    string adminEmailB = "adminB@example.com";
    string adminPasswordB = "Test1234,";
    if (await userManager.FindByEmailAsync(adminEmailB) == null)
    {
      var adminUserB = new ApplicationUser { UserName = adminEmailB, Email = adminEmailB, TenantId = "tenantB" };
      var result = await userManager.CreateAsync(adminUserB, adminPasswordB);
      if (result.Succeeded)
      {
        await userManager.AddToRoleAsync(adminUserB, "Admin");
      }
      else { Console.WriteLine($"Failed to create adminUserB: {string.Join(", ", result.Errors.Select(e => e.Description))}"); }
    }
  }
  catch (Exception ex)
  {
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database or application data.");
  }
}
// --- END OF ALL SEEDING CALLS ---

app.Run();

// Removed static async Task CreateRoles(IServiceProvider serviceProvider) as it's now inline



//using AspnetCoreMvcFull.Services;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Identity;
//using AspnetCoreMvcFull.Data;
//using AspnetCoreMvcFull.Models; // Make sure this is here for ApplicationUser and Tenant

//var builder = WebApplication.CreateBuilder(args);

//// Remove this line - AddHttpContextAccessor() below is sufficient and preferred.
//// builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();


//// Add services to the container.
//builder.Services.AddControllersWithViews();

//// Configure ApplicationDbContext for Identity
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//{
//  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//  options.UseSqlServer(connectionString);
//});

//// Configure AppDbContext for your application data (ComplianceFolders, etc.)
//builder.Services.AddDbContext<AppDbContext>(options =>
//{
//  var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//  options.UseSqlServer(connectionString);
//});


//// --- CRITICAL FIX HERE: Change IdentityUser to ApplicationUser ---
//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddRoles<IdentityRole>() // Add .AddRoles<IdentityRole>() if you are using roles
//    .AddEntityFrameworkStores<ApplicationDbContext>(); // Link to your ApplicationDbContext

//builder.Services.AddRazorPages();

//builder.Services.AddHttpContextAccessor(); // This one is sufficient for IHttpContextAccessor
//builder.Services.AddScoped<ITenantService, TenantService>();

//var app = builder.Build();

//// --- Data Seeding for Application Data (Tenants, Categories, Folders, etc.) ---
//using (var scope = app.Services.CreateScope())
//{
//  var services = scope.ServiceProvider;
//  try
//  {
//    var context = services.GetRequiredService<AppDbContext>();
//    // Apply pending migrations for AppDbContext if any. This is important before seeding.
//    await context.Database.MigrateAsync();
//    // Call your static SeedData method
//    await DataSeeder.SeedData(context);
//  }
//  catch (Exception ex)
//  {
//    var logger = services.GetRequiredService<ILogger<Program>>();
//    logger.LogError(ex, "An error occurred while seeding the application database.");
//  }
//}
//// --- END OF APPLICATION DATA SEEDER CALL ---


//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//  app.UseExceptionHandler("/Home/Error");
//  app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//// IMPORTANT: Add UseAuthentication() before UseAuthorization()
//app.UseAuthentication();
//app.UseAuthorization();

//builder.Services.AddSession(options =>
//{
//  options.IdleTimeout = TimeSpan.FromMinutes(30); // Set your desired timeout
//  options.Cookie.HttpOnly = true;
//  options.Cookie.IsEssential = true;
//});
//// ...
//app.UseSession(); // Must be after UseRouting and UseAuthentication/UseAuthorization

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

//app.MapRazorPages();


//// --- Role Seeding ---
//using (var scope = app.Services.CreateScope())
//{
//  var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//  var roles = new[] { "Admin", "Manager", "User" };

//  foreach (var role in roles)
//  {
//    if (!await roleManager.RoleExistsAsync(role))
//      await roleManager.CreateAsync(new IdentityRole(role));
//  }
//}

//// --- User Seeding (Now with ApplicationUser and TenantId) ---
//using (var scope = app.Services.CreateScope())
//{
//  // This will now correctly resolve UserManager<ApplicationUser>
//  var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

//  // Admin user for Tenant A
//  string adminEmailA = "adminA@example.com";
//  string adminPasswordA = "Test1234,";
//  if (await userManager.FindByEmailAsync(adminEmailA) == null)
//  {
//    var adminUserA = new ApplicationUser { UserName = adminEmailA, Email = adminEmailA, TenantId = "tenantA" };
//    await userManager.CreateAsync(adminUserA, adminPasswordA);
//    await userManager.AddToRoleAsync(adminUserA, "Admin");
//  }



//  // Manager user for Tenant A
//  string managerEmailA = "managerA@example.com";
//  string managerPasswordA = "Test1234,";
//  if (await userManager.FindByEmailAsync(managerEmailA) == null)
//  {
//    var managerUserA = new ApplicationUser { UserName = managerEmailA, Email = managerEmailA, TenantId = "tenantA" };
//    await userManager.CreateAsync(managerUserA, managerPasswordA);
//    await userManager.AddToRoleAsync(managerUserA, "Manager");
//  }

//  // Regular user for Tenant B
//  string userEmailB = "userB@example.com";
//  string userPasswordB = "Test1234,";
//  if (await userManager.FindByEmailAsync(userEmailB) == null)
//  {
//    var regularUserB = new ApplicationUser { UserName = userEmailB, Email = userEmailB, TenantId = "tenantB" };
//    await userManager.CreateAsync(regularUserB, userPasswordB);
//    await userManager.AddToRoleAsync(regularUserB, "User");
//  }
//  // Admin user for Tenant B (NEW ADDITION)
//  string adminEmailB = "adminB@example.com";
//  string adminPasswordB = "Test1234,"; // Consider using a stronger password in production
//  if (await userManager.FindByEmailAsync(adminEmailB) == null)
//  {
//    var adminUserB = new ApplicationUser { UserName = adminEmailB, Email = adminEmailB, TenantId = "tenantB" };
//    await userManager.CreateAsync(adminUserB, adminPasswordB);
//    await userManager.AddToRoleAsync(adminUserB, "Admin");
//  }
//}

//app.Run();
