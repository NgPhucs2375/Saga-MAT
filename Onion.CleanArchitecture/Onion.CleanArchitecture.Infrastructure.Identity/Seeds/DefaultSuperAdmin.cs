using Microsoft.AspNetCore.Identity;
using Onion.CleanArchitecture.Application.Enums;
using Onion.CleanArchitecture.Infrastructure.Identity.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Onion.CleanArchitecture.Infrastructure.Identity.Seeds
{
    public static class DefaultSuperAdmin
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // 1. Kiểm tra / Tạo tài khoản SuperAdmin mặc định
            var defaultUser = new ApplicationUser
            {
                UserName = "superadmin",
                Email = "superadmin@gmail.com",
                FirstName = "Mukesh",
                LastName = "Murugan",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var user = await userManager.FindByEmailAsync(defaultUser.Email);
            if (user == null)
            {
                await userManager.CreateAsync(defaultUser, "123Pa$$word!");
                await userManager.AddToRoleAsync(defaultUser, Roles.SuperAdmin.ToString());
            }

            // 2. Lấy role SuperAdmin để cấu hình Role Claims (Tách ra ngoài điều kiện user == null để luôn cập nhật khi restart)
            var role = await roleManager.FindByNameAsync(Roles.SuperAdmin.ToString());
            if (role != null)
            {
                // Danh sách các Claims chuẩn dành cho SuperAdmin tương ứng với Client Routes
                var targetClaims = new List<Claim>
                {
                    new Claim("dashboard", "list"),
                    new Claim("products", "list#create#create-range#clone#edit#show#delete#delete-range"),
                    new Claim("orders", "list#create#clone#edit#show#delete"),
                    new Claim("notifications", "list#edit"),
                    new Claim("users", "list#create#clone#edit#show#delete"),
                    new Claim("roles", "list#create#clone#edit#show#delete"),
                    new Claim("roleclaims", "list#create#clone#edit#show#delete")
                };

                // Lấy danh sách Claims hiện có của Role để tránh thêm trùng (Idempotent)
                var existingClaims = await roleManager.GetClaimsAsync(role);

                foreach (var claim in targetClaims)
                {
                    var hasClaim = existingClaims.Any(c => c.Type == claim.Type && c.Value == claim.Value);
                    if (!hasClaim)
                    {
                        await roleManager.AddClaimAsync(role, claim);
                    }
                }
            }
        }
    }
}