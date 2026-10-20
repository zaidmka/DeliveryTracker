
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Dtos.Users;

public class UserDto
{
	public string Id { get; set; } = "";
	public string UserName { get; set; } = "";
	public string FullName { get; set; } = "";
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public string Role { get; set; } = "";
}

public class CreateUserRequest
{
	[Required]
	public string UserName { get; set; } = "";

	[Required]
	public string FullName { get; set; } = "";

	[Required]
	public string Password { get; set; } = "";

	[Required]
	public string Role { get; set; } = "Employee";
}

public class UpdateUserRequest
{
	[Required]
	public string FullName { get; set; } = "";

	[Required]
	public string Role { get; set; } = "Employee";

	public bool IsActive { get; set; } = true;
}

public class ResetUserPasswordRequest
{
	[Required]
	public string NewPassword { get; set; } = "";
}
