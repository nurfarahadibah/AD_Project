using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace AspnetCoreMvcFull.Data
{
  public class ProjDbContext : IdentityDbContext
  {
    public ProjDbContext(DbContextOptions<ProjDbContext> options) : base(options) { }

    //protected override void OnModelCreating(ModelBuilder builder)
    //{
    //  base.OnModelCreating(builder);

    //  var user = new IdentityRole("user");
    //  user.NormalizedName = "user";

    //  var manager = new IdentityRole("manager");
    //  manager.NormalizedName = "manager";

    //  var admin = new IdentityRole("admin");
    //  admin.NormalizedName = "admin";

    //  builder.Entity<IdentityRole>().HasData(user, manager, admin);
    //}

  }
}


