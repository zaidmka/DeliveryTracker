
using DeliveryTracker.Server.Data;
using DeliveryTracker.Shared.Dtos.Shops;
using DeliveryTracker.Shared.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/shops")]
[Authorize]
public class ShopsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly IAntiforgery _antiforgery;

	public ShopsController(
		AppDbContext db,
		IAntiforgery antiforgery)
	{
		_db = db;
		_antiforgery = antiforgery;
	}

	// ========================================
	// GET: api/shops
	// ========================================
	[HttpGet]
	public async Task<ActionResult<List<ShopDto>>> GetAll(
		CancellationToken ct)
	{
		var shops = await _db.Shops
			.AsNoTracking()
			.OrderBy(x => x.Name)
			.Select(x => ToDto(x))
			.ToListAsync(ct);

		return Ok(shops);
	}

	// ========================================
	// GET: api/shops/5
	// ========================================
	[HttpGet("{id:int}")]
	public async Task<ActionResult<ShopDto>> GetById(
		int id,
		CancellationToken ct)
	{
		var shop = await _db.Shops
			.AsNoTracking()
			.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (shop is null)
			return NotFound(new { message = "المحل غير موجود" });

		return Ok(ToDto(shop));
	}

	// ========================================
	// POST: api/shops
	// ========================================
	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Create(
		[FromBody] SaveShopRequest request,
		CancellationToken ct)
	{
		if (!await IsValidCsrfAsync())
			return BadRequest(new { message = "رمز الحماية غير صالح" });

		var name = request.Name?.Trim();

		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { message = "اسم المحل مطلوب" });

		var shop = new Shop
		{
			Name = name,
			OwnerName = Clean(request.OwnerName),
			Address = Clean(request.Address),
			PhoneNumber = Clean(request.PhoneNumber),
			QrToken = Guid.NewGuid(),
			IsActive = request.IsActive,
			CreatedAt = DateTime.UtcNow
		};

		// المحاولة مجدداً إذا حدث تعارض في رمز المحل
		for (int attempt = 0; attempt < 10; attempt++)
		{
			shop.Code = await GenerateShopCodeAsync(ct);

			if (_db.Entry(shop).State == EntityState.Detached)
				_db.Shops.Add(shop);

			try
			{
				await _db.SaveChangesAsync(ct);

				return CreatedAtAction(
					nameof(GetById),
					new { id = shop.Id },
					ToDto(shop));
			}
			catch (DbUpdateException ex) when (IsUniqueViolation(ex))
			{
				// نعيد المحاولة فقط إذا التعارض يخص Code
				bool codeExists = await _db.Shops
					.AsNoTracking()
					.AnyAsync(x => x.Code == shop.Code, ct);

				if (!codeExists)
					return Conflict(new
					{
						message = "حدث تعارض أثناء إنشاء المحل"
					});

				// نعيد توليد Code بالمحاولة التالية
			}
		}

		return Conflict(new
		{
			message = "تعذر توليد رمز فريد للمحل، حاول مجدداً"
		});
	}

	// ========================================
	// PUT: api/shops/5
	// ========================================
	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Update(
		int id,
		[FromBody] SaveShopRequest request,
		CancellationToken ct)
	{
		if (!await IsValidCsrfAsync())
			return BadRequest(new { message = "رمز الحماية غير صالح" });

		var shop = await _db.Shops
			.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (shop is null)
			return NotFound(new { message = "المحل غير موجود" });

		var name = request.Name?.Trim();

		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { message = "اسم المحل مطلوب" });

		// تعديل بيانات المحل فقط
		shop.Name = name;
		shop.OwnerName = Clean(request.OwnerName);
		shop.Address = Clean(request.Address);
		shop.PhoneNumber = Clean(request.PhoneNumber);
		shop.IsActive = request.IsActive;

		// لا نغيّر Code أو QrToken نهائياً

		await _db.SaveChangesAsync(ct);

		return NoContent();
	}

	// ========================================
	// توليد رمز المحل
	// مثال: SH-A7F29C4D
	// ========================================
	private async Task<string> GenerateShopCodeAsync(
		CancellationToken ct)
	{
		for (int attempt = 0; attempt < 10; attempt++)
		{
			string code = "SH-" +
				Guid.NewGuid()
					.ToString("N")[..8]
					.ToUpperInvariant();

			bool exists = await _db.Shops
				.AsNoTracking()
				.AnyAsync(x => x.Code == code, ct);

			if (!exists)
				return code;
		}

		throw new InvalidOperationException(
			"تعذر توليد رمز فريد للمحل");
	}

	// ========================================
	// CSRF Validation
	// ========================================
	private async Task<bool> IsValidCsrfAsync()
	{
		try
		{
			await _antiforgery.ValidateRequestAsync(HttpContext);
			return true;
		}
		catch (AntiforgeryValidationException)
		{
			return false;
		}
	}

	// ========================================
	// Helpers
	// ========================================
	private static string? Clean(string? value)
	{
		return string.IsNullOrWhiteSpace(value)
			? null
			: value.Trim();
	}

	private static bool IsUniqueViolation(
		DbUpdateException exception)
	{
		return exception.InnerException is PostgresException pg
			&& pg.SqlState == PostgresErrorCodes.UniqueViolation;
	}

	private static ShopDto ToDto(Shop shop)
	{
		return new ShopDto
		{
			Id = shop.Id,
			Code = shop.Code,
			Name = shop.Name,
			OwnerName = shop.OwnerName,
			Address = shop.Address,
			PhoneNumber = shop.PhoneNumber,
			QrToken = shop.QrToken,
			IsActive = shop.IsActive,
			CreatedAt = shop.CreatedAt
		};
	}



	// GET: api/shops/by-qr/{token}
	[HttpGet("by-qr/{token:guid}")]
	public async Task<ActionResult<ShopDto>> GetByQr(
		Guid token,
		CancellationToken ct)
	{
		var shop = await _db.Shops
			.AsNoTracking()
			.Where(x => x.QrToken == token && x.IsActive)
			.Select(x => new ShopDto
			{
				Id = x.Id,
				Code = x.Code,
				Name = x.Name,
				OwnerName = x.OwnerName,
				Address = x.Address,
				PhoneNumber = x.PhoneNumber,
				QrToken = x.QrToken,
				IsActive = x.IsActive,
				CreatedAt = x.CreatedAt
			})
			.FirstOrDefaultAsync(ct);

		if (shop is null)
			return NotFound(new
			{
				message = "المحل غير موجود أو غير فعال"
			});

		return Ok(shop);
	}

}
