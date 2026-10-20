
using System.Security.Claims;
using DeliveryTracker.Server.Identity;
using DeliveryTracker.Shared.Dtos.Users;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : ControllerBase
{
	private readonly UserManager<ApplicationUser> _users;
	private readonly IAntiforgery _antiforgery;

	public UsersController(
		UserManager<ApplicationUser> users,
		IAntiforgery antiforgery)
	{
		_users = users;
		_antiforgery = antiforgery;
	}

	[HttpGet]
	public async Task<ActionResult<List<UserDto>>> GetAll()
	{
		var users = await _users.Users
			.AsNoTracking()
			.OrderBy(x => x.FullName)
			.ToListAsync();

		var result = new List<UserDto>();

		foreach (var user in users)
		{
			var roles = await _users.GetRolesAsync(user);

			result.Add(new UserDto
			{
				Id = user.Id,
				UserName = user.UserName ?? "",
				FullName = user.FullName,
				IsActive = user.IsActive,
				CreatedAt = user.CreatedAt,
				Role = roles.FirstOrDefault() ?? AppRoles.Employee
			});
		}

		return Ok(result);
	}

	[HttpPost]
	public async Task<IActionResult> Create(
		[FromBody] CreateUserRequest request)
	{
		await _antiforgery.ValidateRequestAsync(HttpContext);

		if (!ValidRole(request.Role))
			return BadRequest(new { message = "الصلاحية غير صحيحة" });

		var username = request.UserName.Trim();
		var fullName = request.FullName.Trim();

		if (username.Length == 0 || fullName.Length == 0)
			return BadRequest(new { message = "أكمل بيانات المستخدم" });

		if (await _users.FindByNameAsync(username) is not null)
			return Conflict(new { message = "اسم المستخدم موجود مسبقاً" });

		var user = new ApplicationUser
		{
			UserName = username,
			FullName = fullName,
			IsActive = true,
			CreatedAt = DateTime.UtcNow
		};

		var created = await _users.CreateAsync(
			user, request.Password);

		if (!created.Succeeded)
			return IdentityError(created);

		var assigned = await _users.AddToRoleAsync(
			user, request.Role);

		if (!assigned.Succeeded)
		{
			await _users.DeleteAsync(user);
			return IdentityError(assigned);
		}

		return Ok(new { message = "تم إنشاء المستخدم بنجاح" });
	}

	[HttpPut("{id}")]
	public async Task<IActionResult> Update(
		string id,
		[FromBody] UpdateUserRequest request)
	{
		await _antiforgery.ValidateRequestAsync(HttpContext);

		if (!ValidRole(request.Role))
			return BadRequest(new { message = "الصلاحية غير صحيحة" });

		var user = await _users.FindByIdAsync(id);

		if (user is null)
			return NotFound();

		var fullName = request.FullName.Trim();

		if (fullName.Length == 0)
			return BadRequest(new { message = "الاسم الكامل مطلوب" });

		var currentRoles = await _users.GetRolesAsync(user);
		var wasAdmin = currentRoles.Contains(AppRoles.Admin);
		var removesAdmin = wasAdmin &&
			(request.Role != AppRoles.Admin || !request.IsActive);

		if (removesAdmin && !await HasAnotherActiveAdmin(id))
			return BadRequest(new
			{
				message = "لا يمكن تعطيل أو إزالة صلاحية آخر مدير فعال"
			});

		var currentUserId = User.FindFirstValue(
			ClaimTypes.NameIdentifier);

		if (id == currentUserId &&
			(!request.IsActive || request.Role != AppRoles.Admin))
			return BadRequest(new
			{
				message = "لا يمكنك تعطيل حسابك أو إزالة صلاحيتك بنفسك"
			});

		// نضيف الدور الجديد قبل إزالة القديم
		if (!currentRoles.Contains(request.Role))
		{
			var addRole = await _users.AddToRoleAsync(
				user, request.Role);

			if (!addRole.Succeeded)
				return IdentityError(addRole);
		}

		var rolesToRemove = currentRoles
			.Where(x => x != request.Role)
			.ToArray();

		if (rolesToRemove.Length > 0)
		{
			var removeRoles = await _users.RemoveFromRolesAsync(
				user, rolesToRemove);

			if (!removeRoles.Succeeded)
				return IdentityError(removeRoles);
		}

		user.FullName = fullName;
		user.IsActive = request.IsActive;

		var updated = await _users.UpdateAsync(user);

		if (!updated.Succeeded)
			return IdentityError(updated);

		// إبطال الجلسات القديمة بعد تغيير الصلاحيات
		var stamp = await _users.UpdateSecurityStampAsync(user);

		if (!stamp.Succeeded)
			return IdentityError(stamp);

		return Ok(new { message = "تم تحديث المستخدم بنجاح" });
	}

	[HttpPost("{id}/reset-password")]
	public async Task<IActionResult> ResetPassword(
		string id,
		[FromBody] ResetUserPasswordRequest request)
	{
		await _antiforgery.ValidateRequestAsync(HttpContext);

		var user = await _users.FindByIdAsync(id);

		if (user is null)
			return NotFound();

		var token = await _users.GeneratePasswordResetTokenAsync(user);

		var result = await _users.ResetPasswordAsync(
			user, token, request.NewPassword);

		if (!result.Succeeded)
			return IdentityError(result);

		// ResetPasswordAsync يحدّث SecurityStamp أيضاً
		return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
	}

	private static bool ValidRole(string role) =>
		role is AppRoles.Admin or AppRoles.Employee;

	private async Task<bool> HasAnotherActiveAdmin(string excludedId)
	{
		var admins = await _users.GetUsersInRoleAsync(
			AppRoles.Admin);

		return admins.Any(x =>
			x.Id != excludedId && x.IsActive);
	}

	private IActionResult IdentityError(IdentityResult result)
	{
		return BadRequest(new
		{
			message = "تعذر تنفيذ العملية",
			errors = result.Errors
				.Select(x => x.Description)
				.ToArray()
		});
	}
}
