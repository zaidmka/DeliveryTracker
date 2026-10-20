
using DeliveryTracker.Server.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Server.Controllers;

public class LoginRequest
{
	[Required]
	public string Username { get; set; } = string.Empty;

	[Required]
	public string Password { get; set; } = string.Empty;
}

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
	private readonly SignInManager<ApplicationUser> _signInManager;
	private readonly UserManager<ApplicationUser> _userManager;
	private readonly IAntiforgery _antiforgery;

	public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager,IAntiforgery antiforgery)
	{
		_signInManager = signInManager;
		_userManager = userManager;
		_antiforgery = antiforgery;
	}

	[HttpGet("csrf")]
	[AllowAnonymous]
	public IActionResult GetCsrfToken()
	{
		Response.Headers.CacheControl = "no-store";

		var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

		return Ok(new
		{
			token = tokens.RequestToken
		});
	}


	[HttpPost("login")]
	[AllowAnonymous]
	[EnableRateLimiting("login")]
	public async Task<IActionResult> Login(
		[FromBody] LoginRequest request)
	{
		await _antiforgery.ValidateRequestAsync(HttpContext);

		var user = await _userManager.FindByNameAsync(
			request.Username);

		// لا نسمح للحساب المعطّل بتسجيل الدخول
		if (user is null || !user.IsActive)
		{
			return Unauthorized(new
			{
				message = "بيانات الدخول غير صحيحة أو الحساب معطّل"
			});
		}

		var result = await _signInManager.PasswordSignInAsync(
			user,
			request.Password,
			isPersistent: false,
			lockoutOnFailure: true);

		if (result.IsLockedOut)
		{
			return StatusCode(429, new
			{
				message = "الحساب مقفل مؤقتاً"
			});
		}

		if (!result.Succeeded)
		{
			return Unauthorized(new
			{
				message = "بيانات الدخول غير صحيحة"
			});
		}

		return Ok(new
		{
			message = "تم تسجيل الدخول"
		});
	}


	[HttpGet("me")]
	[Authorize]
	public async Task<IActionResult> Me()
	{
		var user = await _userManager.GetUserAsync(User);

		if (user == null)
			return Unauthorized();

		return Ok(new
		{
			user.Id,
			user.UserName,
			user.FullName,
			Roles = await _userManager.GetRolesAsync(user)
		});
	}

	[HttpPost("logout")]
	[Authorize]
	public async Task<IActionResult> Logout()
	{
		await _antiforgery.ValidateRequestAsync(HttpContext);

		await _signInManager.SignOutAsync();

		return Ok(new { message = "تم تسجيل الخروج" });
	}
}
