
namespace DeliveryTracker.Shared.Dtos.Reports;

public class DeliveryReportDto
{
	public int TotalDeliveries { get; set; }
	public int TotalQuantity { get; set; }
	public int TotalShops { get; set; }
	public int TotalItemTypes { get; set; }

	public List<ReportItemDto> Items { get; set; } = new();
	public List<ReportShopDto> Shops { get; set; } = new();
	public List<ReportDailyDto> Daily { get; set; } = new();
}

public class ReportItemDto
{
	public string Name { get; set; } = "";
	public int Quantity { get; set; }
}

public class ReportShopDto
{
	public string Name { get; set; } = "";
	public string Code { get; set; } = "";
	public int DeliveriesCount { get; set; }
	public int Quantity { get; set; }
}

public class ReportDailyDto
{
	public DateOnly Date { get; set; }
	public int DeliveriesCount { get; set; }
	public int Quantity { get; set; }
}
