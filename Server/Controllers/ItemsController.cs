
using DeliveryTracker.Server.Data;
using DeliveryTracker.Shared.Dtos.Items;
using DeliveryTracker.Shared.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/items")]
[Authorize]
public class ItemsController : ControllerBase
{
	private readonly AppDbContext _db;
	private readonly IAntiforgery _antiforgery;

	public ItemsController(
		AppDbContext db,
		IAntiforgery antiforgery)
	{
		_db = db;
		_antiforgery = antiforgery;
	}

	// GET: api/items
	[HttpGet]
	public async Task<ActionResult<List<ItemDto>>> GetAll(
		[FromQuery] bool activeOnly = false,
		CancellationToken ct = default)
	{
		var query = _db.ItemTypes.AsNoTracking();

		if (activeOnly)
			query = query.Where(x => x.IsActive);

		var items = await query
			.OrderBy(x => x.Name)
			.Select(x => new ItemDto
			{
				Id = x.Id,
				Name = x.Name,
				Description = x.Description,
				IsActive = x.IsActive
			})
			.ToListAsync(ct);

		return Ok(items);
	}

	// GET: api/items/5
	[HttpGet("{id:int}")]
	public async Task<ActionResult<ItemDto>> GetById(
		int id,
		CancellationToken ct)
	{
		var item = await _db.ItemTypes
			.AsNoTracking()
			.Where(x => x.Id == id)
			.Select(x => new ItemDto
			{
				Id = x.Id,
				Name = x.Name,
				Description = x.Description,
				IsActive = x.IsActive
			})
			.FirstOrDefaultAsync(ct);

		return item is null
			? NotFound(new { message = "المادة غير موجودة" })
			: Ok(item);
	}

	// POST: api/items
	[HttpPost]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Create(
		[FromBody] SaveItemRequest request,
		CancellationToken ct)
	{
		if (!await IsValidCsrfAsync())
			return BadRequest(new { message = "رمز الحماية غير صالح" });

		var name = request.Name?.Trim();

		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { message = "اسم المادة مطلوب" });

		if (await NameExistsAsync(name, null, ct))
			return Conflict(new { message = "اسم المادة موجود مسبقاً" });

		var item = new ItemType
		{
			Name = name,
			Description = Clean(request.Description),
			IsActive = request.IsActive
		};

		_db.ItemTypes.Add(item);

		try
		{
			await _db.SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex) when (IsUniqueViolation(ex))
		{
			return Conflict(new { message = "اسم المادة موجود مسبقاً" });
		}

		return CreatedAtAction(
			nameof(GetById),
			new { id = item.Id },
			ToDto(item));
	}

	// PUT: api/items/5
	[HttpPut("{id:int}")]
	[Authorize(Roles = "Admin")]
	public async Task<IActionResult> Update(
		int id,
		[FromBody] SaveItemRequest request,
		CancellationToken ct)
	{
		if (!await IsValidCsrfAsync())
			return BadRequest(new { message = "رمز الحماية غير صالح" });

		var item = await _db.ItemTypes
			.FirstOrDefaultAsync(x => x.Id == id, ct);

		if (item is null)
			return NotFound(new { message = "المادة غير موجودة" });

		var name = request.Name?.Trim();

		if (string.IsNullOrWhiteSpace(name))
			return BadRequest(new { message = "اسم المادة مطلوب" });

		if (await NameExistsAsync(name, id, ct))
			return Conflict(new { message = "اسم المادة موجود مسبقاً" });

		item.Name = name;
		item.Description = Clean(request.Description);
		item.IsActive = request.IsActive;

		try
		{
			await _db.SaveChangesAsync(ct);
		}
		catch (DbUpdateException ex) when (IsUniqueViolation(ex))
		{
			return Conflict(new { message = "اسم المادة موجود مسبقاً" });
		}

		return NoContent();
	}

	private async Task<bool> NameExistsAsync(
		string name,
		int? excludeId,
		CancellationToken ct)
	{
		return await _db.ItemTypes.AnyAsync(
			x => x.Name == name &&
				 (!excludeId.HasValue || x.Id != excludeId.Value),
			ct);
	}

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

	private static string? Clean(string? value) =>
		string.IsNullOrWhiteSpace(value) ? null : value.Trim();

	private static bool IsUniqueViolation(DbUpdateException ex) =>
		ex.InnerException is PostgresException pg &&
		pg.SqlState == PostgresErrorCodes.UniqueViolation;

	private static ItemDto ToDto(ItemType item) => new()
	{
		Id = item.Id,
		Name = item.Name,
		Description = item.Description,
		IsActive = item.IsActive
	};
}
