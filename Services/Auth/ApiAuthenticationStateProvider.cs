using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace DeliveryTracker.Services.Auth;

public class ApiAuthenticationStateProvider
	: AuthenticationStateProvider
{
	private readonly IAuthService _authService;

	public ApiAuthenticationStateProvider(
		IAuthService authService)
	{
		_authService = authService;
	}

	public override async Task<AuthenticationState>
		GetAuthenticationStateAsync()
	{
		try
		{
			var user = await _authService.GetCurrentUserAsync();

			if (user == null)
				return Anonymous();

			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, user.Id),
				new(ClaimTypes.Name, user.UserName ?? "")
			};

			foreach (var role in user.Roles)
			{
				claims.Add(new Claim(ClaimTypes.Role, role));
			}

			var identity = new ClaimsIdentity(
				claims, "Cookie");

			return new AuthenticationState(
				new ClaimsPrincipal(identity));
		}
		catch
		{
			return Anonymous();
		}
	}

	public void NotifyAuthenticationChanged()
	{
		NotifyAuthenticationStateChanged(
			GetAuthenticationStateAsync());
	}

	private static AuthenticationState Anonymous()
	{
		return new AuthenticationState(
			new ClaimsPrincipal(
				new ClaimsIdentity()));
	}
}
