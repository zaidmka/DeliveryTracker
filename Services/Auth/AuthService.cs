using DeliveryTracker.Shared.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace DeliveryTracker.Services.Auth;

public class AuthService : IAuthService
{
	private readonly HttpClient _http;

	public AuthService(HttpClient http)
	{
		_http = http;
	}

	private async Task<string> GetCsrfTokenAsync()
	{
		var result = await _http.GetFromJsonAsync<CsrfResponse>("api/auth/csrf");

		return result?.Token
			?? throw new InvalidOperationException(
				"CSRF token missing");
	}

	public async Task<bool> LoginAsync(
		string username,
		string password)
	{
		var csrfToken = await GetCsrfTokenAsync();

		using var request = new HttpRequestMessage(
			HttpMethod.Post, "api/auth/login");

		request.Headers.Add("X-CSRF-TOKEN", csrfToken);

		request.Content = JsonContent.Create(new
		{
			Username = username,
			Password = password
		});

		using var response = await _http.SendAsync(request);

		if (response.StatusCode == HttpStatusCode.Unauthorized ||
			response.StatusCode == HttpStatusCode.TooManyRequests)
		{
			return false;
		}

		response.EnsureSuccessStatusCode();

		return true;
	}

	public async Task LogoutAsync()
	{
		var csrfToken = await GetCsrfTokenAsync();

		using var request = new HttpRequestMessage(
			HttpMethod.Post, "api/auth/logout");

		request.Headers.Add("X-CSRF-TOKEN", csrfToken);

		using var response = await _http.SendAsync(request);
		response.EnsureSuccessStatusCode();
	}

	public async Task<AuthUserDto?> GetCurrentUserAsync()
	{
		using var response = await _http.GetAsync("api/auth/me");

		if (response.StatusCode == HttpStatusCode.Unauthorized)
			return null;

		response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync<AuthUserDto>();
	}

	private sealed class CsrfResponse
	{
		public string Token { get; set; } = string.Empty;
	}
}
