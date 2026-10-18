
using System.Net;
using System.Net.Http.Json;
using DeliveryTracker.Shared.Dtos.Shops;

namespace DeliveryTracker.Services.Shops;

public class ShopService : IShopService
{
	private readonly HttpClient _http;

	public ShopService(HttpClient http)
	{
		_http = http;
	}

	public async Task<List<ShopDto>> GetAllAsync()
	{
		return await _http.GetFromJsonAsync<List<ShopDto>>(
			"api/shops") ?? new();
	}

	public async Task<ShopDto?> GetByIdAsync(int id)
	{
		var response = await _http.GetAsync($"api/shops/{id}");

		if (response.StatusCode == HttpStatusCode.NotFound)
			return null;

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<ShopDto>();
	}

	public async Task CreateAsync(SaveShopRequest request)
	{
		using var message = new HttpRequestMessage(
			HttpMethod.Post, "api/shops")
		{
			Content = JsonContent.Create(request)
		};

		await AddCsrfTokenAsync(message);

		using var response = await _http.SendAsync(message);

		await EnsureSuccessAsync(response);
	}

	public async Task UpdateAsync(int id, SaveShopRequest request)
	{
		using var message = new HttpRequestMessage(
			HttpMethod.Put, $"api/shops/{id}")
		{
			Content = JsonContent.Create(request)
		};

		await AddCsrfTokenAsync(message);

		using var response = await _http.SendAsync(message);

		await EnsureSuccessAsync(response);
	}

	private async Task AddCsrfTokenAsync(HttpRequestMessage message)
	{
		var csrf = await _http.GetFromJsonAsync<CsrfResponse>(
			"api/auth/csrf");

		if (string.IsNullOrWhiteSpace(csrf?.Token))
			throw new InvalidOperationException(
				"تعذر الحصول على رمز الحماية");

		message.Headers.Add("X-CSRF-TOKEN", csrf.Token);
	}

	private static async Task EnsureSuccessAsync(
		HttpResponseMessage response)
	{
		if (response.IsSuccessStatusCode)
			return;

		if (response.StatusCode == HttpStatusCode.Conflict)
			throw new InvalidOperationException(
				"رمز المحل مستخدم مسبقاً");

		if (response.StatusCode == HttpStatusCode.Unauthorized)
			throw new UnauthorizedAccessException(
				"انتهت جلسة تسجيل الدخول");

		if (response.StatusCode == HttpStatusCode.Forbidden)
			throw new UnauthorizedAccessException(
				"ليست لديك صلاحية تنفيذ هذه العملية");

		throw new HttpRequestException(
			$"فشل تنفيذ العملية: {(int)response.StatusCode}");
	}

	private sealed class CsrfResponse
	{
		public string Token { get; set; } = "";
	}
}
