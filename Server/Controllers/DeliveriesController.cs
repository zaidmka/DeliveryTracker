
using System.Security.Claims;
using DeliveryTracker.Server.Data;
using DeliveryTracker.Shared.Dtos.Deliveries;
using DeliveryTracker.Shared.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DeliveryTracker.Server.Services;
namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/deliveries")]
[Authorize]
public class DeliveriesController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly IAntiforgery _antiforgery;

	public DeliveriesController(
		AppDbContext db,
		IAntiforgery antiforgery)
	{
		_db = db;
		_antiforgery = antiforgery;
	}

	// POST: api/deliveries
	[HttpPost]
	public async Task<IActionResult> Create(
		[FromBody] CreateDeliveryRequest request,
		CancellationToken ct)
	{
		try
		{
			await _antiforgery.ValidateRequestAsync(HttpContext);
		}
		catch (AntiforgeryValidationException)
		{
			return BadRequest(new
			{
				message = "رمز الحماية غير صالح"
			});
		}

		var userId = User.FindFirstValue(
			ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
			return Unauthorized();

		if (request.ShopQrToken == Guid.Empty)
			return BadRequest(new
			{
				message = "رمز QR غير صالح"
			});

		if (request.Items is null || request.Items.Count == 0)
			return BadRequest(new
			{
				message = "يجب اختيار مادة واحدة على الأقل"
			});

		if (request.Items.Any(x =>
			x.ItemTypeId <= 0 || x.Quantity <= 0))
		{
			return BadRequest(new
			{
				message = "تأكد من المواد والكميات"
			});
		}

		var itemIds = request.Items
			.Select(x => x.ItemTypeId)
			.ToList();

		if (itemIds.Distinct().Count() != itemIds.Count)
		{
			return BadRequest(new
			{
				message = "لا يمكن تكرار نفس المادة في عملية واحدة"
			});
		}

		var shop = await _db.Shops
			.AsNoTracking()
			.FirstOrDefaultAsync(
				x => x.QrToken == request.ShopQrToken,
				ct);

		if (shop is null)
			return NotFound(new
			{
				message = "المحل غير موجود"
			});

		if (!shop.IsActive)
			return BadRequest(new
			{
				message = "هذا المحل معطل ولا يمكن تسجيل تسليم له"
			});

		var activeItemCount = await _db.ItemTypes
			.CountAsync(x =>
				x.IsActive && itemIds.Contains(x.Id), ct);

		if (activeItemCount != itemIds.Count)
		{
			return BadRequest(new
			{
				message = "إحدى المواد غير موجودة أو معطلة"
			});
		}

		var now = DateTime.UtcNow;

		var delivery = new Delivery
		{
			ShopId = shop.Id,
			DeliveryDate = now,
			CreatedAt = now,
			CreatedByUserId = userId,
			Notes = string.IsNullOrWhiteSpace(request.Notes)
				? null
				: request.Notes.Trim(),

			Items = request.Items.Select(x => new DeliveryItem
			{
				ItemTypeId = x.ItemTypeId,
				Quantity = x.Quantity
			}).ToList()
		};

		_db.Deliveries.Add(delivery);

		await _db.SaveChangesAsync(ct);

		return CreatedAtAction(
			nameof(GetById),
			new { id = delivery.Id },
			new
			{
				id = delivery.Id,
				message = "تم تسجيل التسليم بنجاح"
			});
	}

	// GET: api/deliveries/5

	[HttpGet("{id:int}")]
	public async Task<ActionResult<DeliveryDto>> GetById(
		int id,
		CancellationToken ct)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
			return Unauthorized();

		var query = _db.Deliveries
			.AsNoTracking()
			.Where(x => x.Id == id);

		if (!User.IsInRole("Admin"))
		{
			query = query.Where(x =>
				x.CreatedByUserId == userId);
		}

		var delivery = await query
			.Select(x => new DeliveryDto
			{
				Id = x.Id,
				ShopId = x.ShopId,
				ShopName = x.Shop.Name,
				ShopCode = x.Shop.Code,
				DeliveryDate = x.DeliveryDate,
				CreatedAt = x.CreatedAt,
				CreatedByUserId = x.CreatedByUserId,
				Notes = x.Notes,

				Items = x.Items.Select(i => new DeliveryItemDto
				{
					ItemTypeId = i.ItemTypeId,
					ItemName = i.ItemType.Name,
					Quantity = i.Quantity
				}).ToList()
			})
			.FirstOrDefaultAsync(ct);

		if (delivery is null)
			return NotFound(new
			{
				message = "عملية التسليم غير موجودة"
			});

		return Ok(delivery);
	}


	// GET: api/deliveries

	[HttpGet]
	public async Task<ActionResult<DeliveryPageDto>> GetAll(
		[FromQuery] string? search,
		[FromQuery] DateOnly? from,
		[FromQuery] DateOnly? to,
		[FromQuery] int page = 1,
		[FromQuery] int pageSize = 10,
		CancellationToken ct = default)
	{
		var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
			return Unauthorized();

		if (from.HasValue && to.HasValue && from > to)
			return BadRequest(new
			{
				message = "تاريخ البداية يجب أن يسبق تاريخ النهاية"
			});

		page = Math.Max(1, page);
		pageSize = Math.Clamp(pageSize, 1, 50);

		var query = _db.Deliveries
			.AsNoTracking()
			.AsQueryable();

		// المدير يشوف الكل، وبقية المستخدمين يشوفون عملياتهم فقط
		if (!User.IsInRole("Admin"))
		{
			query = query.Where(x =>
				x.CreatedByUserId == userId);
		}


		if (!string.IsNullOrWhiteSpace(search))
		{
			var keyword = search.Trim();

			// 
			bool isDeliveryId = int.TryParse(keyword, out int deliveryId);

			query = query.Where(x =>
				x.Shop.Name.Contains(keyword) ||
				x.Shop.Code.Contains(keyword) ||
				(isDeliveryId && x.Id == deliveryId));
		}


		// 
		if (from.HasValue)
		{
			var start = DateTime.SpecifyKind(
				from.Value.ToDateTime(TimeOnly.MinValue),
				DateTimeKind.Utc);

			query = query.Where(x => x.DeliveryDate >= start);
		}

		if (to.HasValue)
		{
			var endExclusive = DateTime.SpecifyKind(
				to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
				DateTimeKind.Utc);

			query = query.Where(x => x.DeliveryDate < endExclusive);
		}

		var totalCount = await query.CountAsync(ct);

		var deliveries = await query
			.OrderByDescending(x => x.DeliveryDate)
			.ThenByDescending(x => x.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(x => new DeliveryDto
			{
				Id = x.Id,
				ShopId = x.ShopId,
				ShopName = x.Shop.Name,
				ShopCode = x.Shop.Code,
				DeliveryDate = x.DeliveryDate,
				CreatedAt = x.CreatedAt,
				CreatedByUserId = x.CreatedByUserId,
				Notes = x.Notes,

				Items = x.Items.Select(i => new DeliveryItemDto
				{
					ItemTypeId = i.ItemTypeId,
					ItemName = i.ItemType.Name,
					Quantity = i.Quantity
				}).ToList()
			})
			.ToListAsync(ct);

		return Ok(new DeliveryPageDto
		{
			Items = deliveries,
			TotalCount = totalCount,
			Page = page,
			PageSize = pageSize
		});
	}




	[HttpGet("{id:int}/pdf")]
	public async Task<IActionResult> DownloadReceipt(
		int id,
		[FromServices] DeliveryReceiptPdfService pdfService,
		[FromServices] IConfiguration config,
		CancellationToken ct)
	{
		var result = await GetById(id, ct);

		if (result.Result is not OkObjectResult ok ||
			ok.Value is not DeliveryDto delivery)
		{
			return result.Result ?? NotFound();
		}

		var qrToken = await _db.Shops
			.AsNoTracking()
			.Where(x => x.Id == delivery.ShopId)
			.Select(x => x.QrToken)
			.FirstOrDefaultAsync(ct);

		if (qrToken == Guid.Empty)
			return NotFound("رمز المحل غير موجود");

		var baseUrl = config["App:PublicBaseUrl"];

		if (string.IsNullOrWhiteSpace(baseUrl))
			return Problem("لم يتم إعداد رابط الموقع");

		var shopUrl =
			$"{baseUrl.TrimEnd('/')}/scan?token={qrToken:D}";

		var pdf = pdfService.Generate(delivery, shopUrl);

		return File(
			pdf,
			"application/pdf",
			$"Delivery-{id:D6}.pdf");
	}



}
