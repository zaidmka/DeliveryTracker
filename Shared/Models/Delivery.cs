
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Models;

public class Delivery
{
	public int Id { get; set; }

	public int ShopId { get; set; }
	public Shop Shop { get; set; } = null!;

	// وقت التوصيل الفعلي
	public DateTime DeliveryDate { get; set; } = DateTime.UtcNow;

	[MaxLength(500)]
	public string? Notes { get; set; }

	// معرف المستخدم الذي سجل التسليم
	[Required]
	[MaxLength(450)]
	public string CreatedByUserId { get; set; } = string.Empty;

	// وقت التسجيل بالنظام
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<DeliveryItem> Items { get; set; }
		= new List<DeliveryItem>();
}
