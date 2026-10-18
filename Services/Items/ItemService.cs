
using System.Net;
using System.Net.Http.Json;
using DeliveryTracker.Shared.Dtos.Items;

namespace DeliveryTracker.Services.Items;

public class ItemService : IItemService
{
	private readonly HttpClient _http;

	public ItemService(HttpClient http)
	{
		_http = http;
	}

	public async Task<List<ItemDto>> GetAllAsync(bool activeOnly = false)
	{
		return await _http.GetFromJsonAsync<List<ItemDto>>(
			$"api/items?activeOnly={activeOnly.ToString().ToLowerInvariant()}"
		) ?? new();
	}

	public async Task CreateAsync(SaveItemRequest request)
	{
		await SendAsync(HttpMethod.Post, "api/items", request);
	}

	public async Task UpdateAsync(int id, SaveItemRequest request)
	{
		await SendAsync(HttpMethod.Put, $"api/items/{id}", request);
	}

	private async Task SendAsync(
		HttpMethod method,
		string url,
		SaveItemRequest request)
	{
		var csrf = await _http.GetFromJsonAsync<CsrfResponse>(
			"api/auth/csrf");

		if (string.IsNullOrWhiteSpace(csrf?.Token))
			throw new InvalidOperationException("تعذر الحصول على رمز الحماية");

		using var message = new HttpRequestMessage(method, url)
		{
			Content = JsonContent.Create(request)
		};

		message.Headers.Add("X-CSRF-TOKEN", csrf.Token);

		using var response = await _http.SendAsync(message);

		if (response.IsSuccessStatusCode)
			return;

		if (response.StatusCode == HttpStatusCode.Conflict)
			throw new InvalidOperationException("اسم المادة موجود مسبقاً");

		if (response.StatusCode == HttpStatusCode.Unauthorized)
			throw new UnauthorizedAccessException("انتهت جلسة تسجيل الدخول");

		if (response.StatusCode == HttpStatusCode.Forbidden)
			throw new UnauthorizedAccessException("ليست لديك صلاحية");

		throw new HttpRequestException(
			$"تعذر حفظ المادة: {(int)response.StatusCode}");
	}

	private sealed class CsrfResponse
	{
		public string Token { get; set; } = "";
	}
}
