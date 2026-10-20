
using System.Security.Claims;
using DeliveryTracker.Server.Data;
using DeliveryTracker.Shared.Dtos.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryTracker.Server.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
	private readonly AppDbContext _db;

	public ReportsController(AppDbContext db)
	{
		_db = db;
	}

	[HttpGet("overview")]
	public async Task<ActionResult<DeliveryReportDto>> GetOverview(
		[FromQuery] DateOnly? from,
		[FromQuery] DateOnly? to,
		CancellationToken ct)
	{
		var userId = User.FindFirstValue(
			ClaimTypes.NameIdentifier);

		if (string.IsNullOrWhiteSpace(userId))
			return Unauthorized();

		if (from.HasValue && to.HasValue && from > to)
			return BadRequest(new
			{
				message = "تاريخ البداية يجب أن يسبق تاريخ النهاية"
			});

		var query = _db.Deliveries
			.AsNoTracking()
			.AsQueryable();

		// تطبيق الصلاحيات داخل السيرفر
		if (!User.IsInRole("Admin"))
		{
			query = query.Where(x =>
				x.CreatedByUserId == userId);
		}

		// حدود اليوم بتوقيت بغداد وتحويلها إلى UTC
		var baghdad = TimeZoneInfo.FindSystemTimeZoneById(
			"Asia/Baghdad");

		if (from.HasValue)
		{
			var localStart = DateTime.SpecifyKind(
				from.Value.ToDateTime(TimeOnly.MinValue),
				DateTimeKind.Unspecified);

			var utcStart = TimeZoneInfo.ConvertTimeToUtc(
				localStart, baghdad);

			query = query.Where(x =>
				x.DeliveryDate >= utcStart);
		}

		if (to.HasValue)
		{
			var localEnd = DateTime.SpecifyKind(
				to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
				DateTimeKind.Unspecified);

			var utcEnd = TimeZoneInfo.ConvertTimeToUtc(
				localEnd, baghdad);

			query = query.Where(x =>
				x.DeliveryDate < utcEnd);
		}

		var totalDeliveries = await query.CountAsync(ct);

		var totalShops = await query
			.Select(x => x.ShopId)
			.Distinct()
			.CountAsync(ct);

		var itemsQuery = query.SelectMany(x => x.Items);

		var totalQuantity = await itemsQuery
			.SumAsync(x => (int?)x.Quantity, ct) ?? 0;

		var totalItemTypes = await itemsQuery
			.Select(x => x.ItemTypeId)
			.Distinct()
			.CountAsync(ct);

		var items = await itemsQuery
			.GroupBy(x => new
			{
				x.ItemTypeId,
				x.ItemType.Name
			})
			.Select(g => new ReportItemDto
			{
				Name = g.Key.Name,
				Quantity = g.Sum(x => x.Quantity)
			})
			.OrderByDescending(x => x.Quantity)
			.ToListAsync(ct);

		var shops = await query
			.GroupBy(x => new
			{
				x.ShopId,
				x.Shop.Name,
				x.Shop.Code
			})
			.Select(g => new ReportShopDto
			{
				Name = g.Key.Name,
				Code = g.Key.Code,
				DeliveriesCount = g.Count(),
				Quantity = g.SelectMany(x => x.Items)
					.Sum(x => (int?)x.Quantity) ?? 0
			})
			.OrderByDescending(x => x.Quantity)
			.Take(10)
			.ToListAsync(ct);

		// نجلب فقط التاريخ والكمية لكل عملية،
		// ثم نجمع حسب يوم بغداد بعد التحويل
		var dailySource = await query
			.Select(x => new
			{
				x.DeliveryDate,
				Quantity = x.Items
					.Sum(i => (int?)i.Quantity) ?? 0
			})
			.ToListAsync(ct);

		var daily = dailySource
			.GroupBy(x => DateOnly.FromDateTime(
				TimeZoneInfo.ConvertTimeFromUtc(
					DateTime.SpecifyKind(
						x.DeliveryDate, DateTimeKind.Utc),
					baghdad)))
			.Select(g => new ReportDailyDto
			{
				Date = g.Key,
				DeliveriesCount = g.Count(),
				Quantity = g.Sum(x => x.Quantity)
			})
			.OrderBy(x => x.Date)
			.ToList();

		return Ok(new DeliveryReportDto
		{
			TotalDeliveries = totalDeliveries,
			TotalQuantity = totalQuantity,
			TotalShops = totalShops,
			TotalItemTypes = totalItemTypes,
			Items = items,
			Shops = shops,
			Daily = daily
		});
	}
}
