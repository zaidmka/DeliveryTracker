
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Dtos.Deliveries;

public class CreateDeliveryRequest
{
	[Required]
	public Guid ShopQrToken { get; set; }

	[MaxLength(500)]
	public string? Notes { get; set; }

	[Required]
	[MinLength(1, ErrorMessage = "يجب تحديد مادة واحدة على الأقل")]
	public List<CreateDeliveryItemRequest> Items { get; set; } = new();
}

public class CreateDeliveryItemRequest
{
	[Range(1, int.MaxValue)]
	public int ItemTypeId { get; set; }

	[Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
	public int Quantity { get; set; }
}

public class DeliveryDto
{
	public int Id { get; set; }
	public int ShopId { get; set; }
	public string ShopName { get; set; } = "";
	public string ShopCode { get; set; } = "";
	public DateTime DeliveryDate { get; set; }
	public DateTime CreatedAt { get; set; }
	public string CreatedByUserId { get; set; } = "";
	public string? Notes { get; set; }
	public List<DeliveryItemDto> Items { get; set; } = new();
}

public class DeliveryItemDto
{
	public int ItemTypeId { get; set; }
	public string ItemName { get; set; } = "";
	public int Quantity { get; set; }
}
