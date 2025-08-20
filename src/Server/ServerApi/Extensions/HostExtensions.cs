using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerApi.Infrastructure.Data;
using ServerApi.Infrastructure.Identity;

namespace ServerApi.Extensions;

public static class HostExtensions
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleMgr.Roles.AnyAsync())
        {
            await roleMgr.CreateAsync(new IdentityRole("Admin"));
            await roleMgr.CreateAsync(new IdentityRole("User"));
        }

        if (!await userMgr.Users.AnyAsync())
        {
            var admin = new ApplicationUser { Email = "admin@local", UserName = "admin@local", FullName = "Super Admin" };
            await userMgr.CreateAsync(admin, "Admin@123");
            await userMgr.AddToRoleAsync(admin, "Admin");
        }
    }
}