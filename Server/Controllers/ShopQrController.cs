
using DeliveryTracker.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/shops")]
[Authorize]
public class ShopQrController : ControllerBase
{
	private readonly AppDbContext _db;

	public ShopQrController(AppDbContext db)
	{
		_db = db;
	}

	// GET: api/shops/5/qr
	[HttpGet("{id:int}/qr")]
	public async Task<IActionResult> GetQr(
		int id,
		CancellationToken ct)
	{
		var shop = await _db.Shops
			.AsNoTracking()
			.Where(x => x.Id == id)
			.Select(x => new
			{
				x.QrToken
			})
			.FirstOrDefaultAsync(ct);

		if (shop is null)
			return NotFound();

		// نفس رابط الموقع الحالي
		var baseUrl = $"{Request.Scheme}://{Request.Host}";

		var scanUrl =
			$"{baseUrl}/scan?token={shop.QrToken:D}";

		using var qrGenerator = new QRCodeGenerator();

		using var qrData = qrGenerator.CreateQrCode(
			scanUrl,
			QRCodeGenerator.ECCLevel.Q);

		var qrCode = new PngByteQRCode(qrData);

		byte[] pngBytes = qrCode.GetGraphic(12);

		return File(pngBytes, "image/png");
	}
}
