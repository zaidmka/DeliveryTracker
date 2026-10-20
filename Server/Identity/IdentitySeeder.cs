
using Microsoft.AspNetCore.Identity;

namespace DeliveryTracker.Server.Identity;

public static class IdentitySeeder
{
	public static async Task SeedAsync(
		IServiceProvider services,
		IConfiguration configuration)
	{
		using var scope = services.CreateScope();

		var roleManager = scope.ServiceProvider
			.GetRequiredService<RoleManager<IdentityRole>>();

		var userManager = scope.ServiceProvider
			.GetRequiredService<UserManager<ApplicationUser>>();

		// 1. التأكد من وجود جميع أدوار النظام
		var roles = new[]
		{
			AppRoles.Admin,
			AppRoles.Employee
		};

		foreach (var role in roles)
		{
			if (await roleManager.RoleExistsAsync(role))
				continue;

			var result = await roleManager.CreateAsync(
				new IdentityRole(role));

			if (!result.Succeeded)
			{
				throw new InvalidOperationException(
					$"Failed to create role {role}: " +
					string.Join("; ",
						result.Errors.Select(x => x.Description)));
			}
		}

		// 2. قراءة بيانات مدير النظام الأولي
		var username = configuration["SeedAdmin:Username"];
		var password = configuration["SeedAdmin:Password"];

		if (string.IsNullOrWhiteSpace(username) ||
			string.IsNullOrWhiteSpace(password))
		{
			return;
		}

		// 3. البحث عن المدير أو إنشاؤه عند عدم وجوده
		var user = await userManager.FindByNameAsync(username);

		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = username,
				FullName = "System Administrator",
				IsActive = true,
				CreatedAt = DateTime.UtcNow
			};

			var createResult = await userManager.CreateAsync(
				user, password);

			if (!createResult.Succeeded)
			{
				throw new InvalidOperationException(
					"Failed to create admin: " +
					string.Join("; ",
						createResult.Errors.Select(x => x.Description)));
			}
		}

		// 4. منح المدير صلاحية Admin
		if (!await userManager.IsInRoleAsync(
			user, AppRoles.Admin))
		{
			var roleResult = await userManager.AddToRoleAsync(
				user, AppRoles.Admin);

			if (!roleResult.Succeeded)
			{
				throw new InvalidOperationException(
					"Failed to assign admin role: " +
					string.Join("; ",
						roleResult.Errors.Select(x => x.Description)));
			}
		}
	}
}
