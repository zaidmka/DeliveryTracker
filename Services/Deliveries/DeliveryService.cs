
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DeliveryTracker.Shared.Dtos.Deliveries;
using DeliveryTracker.Shared.Dtos.Shops;

namespace DeliveryTracker.Services.Deliveries;

public class DeliveryService : IDeliveryService
{
	private readonly HttpClient _http;

	public DeliveryService(HttpClient http)
	{
		_http = http;
	}

	public async Task<ShopDto?> GetShopByQrAsync(Guid token)
	{
		using var response = await _http.GetAsync(
			$"api/shops/by-qr/{token:D}");

		if (response.StatusCode == HttpStatusCode.NotFound)
			return null;

		response.EnsureSuccessStatusCode();

		return await response.Content.ReadFromJsonAsync<ShopDto>();
	}

	public async Task<int> CreateAsync(CreateDeliveryRequest request)
	{
		var csrf = await _http.GetFromJsonAsync<CsrfResponse>(
			"api/auth/csrf");

		if (string.IsNullOrWhiteSpace(csrf?.Token))
			throw new InvalidOperationException(
				"تعذر الحصول على رمز الحماية");

		using var message = new HttpRequestMessage(
			HttpMethod.Post, "api/deliveries")
		{
			Content = JsonContent.Create(request)
		};

		message.Headers.Add("X-CSRF-TOKEN", csrf.Token);

		using var response = await _http.SendAsync(message);

		if (!response.IsSuccessStatusCode)
		{
			if (response.StatusCode == HttpStatusCode.Unauthorized)
				throw new InvalidOperationException(
					"انتهت جلسة تسجيل الدخول");

			if (response.StatusCode == HttpStatusCode.Forbidden)
				throw new InvalidOperationException(
					"ليست لديك صلاحية تسجيل التسليم");

			string? serverMessage = null;

			try
			{
				using var json = JsonDocument.Parse(
					await response.Content.ReadAsStringAsync());

				if (json.RootElement.TryGetProperty(
					"message", out var value))
				{
					serverMessage = value.GetString();
				}
			}
			catch (JsonException)
			{
				// إذا الرد ليس JSON نستخدم رسالة عامة
			}

			throw new InvalidOperationException(
				serverMessage ??
				$"تعذر تسجيل التسليم ({(int)response.StatusCode})");
		}

		var result = await response.Content
			.ReadFromJsonAsync<CreateDeliveryResponse>();

		return result?.Id
			?? throw new InvalidOperationException(
				"تم إرسال الطلب لكن لم يصل رقم العملية");
	}

	private sealed class CsrfResponse
	{
		public string Token { get; set; } = "";
	}

	private sealed class CreateDeliveryResponse
	{
		public int Id { get; set; }
	}
}
