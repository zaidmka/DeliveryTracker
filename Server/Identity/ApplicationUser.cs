
using Microsoft.AspNetCore.Identity;

namespace DeliveryTracker.Server.Identity;

public class ApplicationUser : IdentityUser
{
	public string FullName { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public bool IsActive { get; set; } = true;

}
public static class AppRoles
{
	public const string Admin = "Admin";
	public const string Employee = "Employee";
}