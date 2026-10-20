
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Models;

public class Shop
{
	public int Id { get; set; }

	[Required, MaxLength(30)]
	public string Code { get; set; } = string.Empty;

	[Required, MaxLength(200)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(200)]
	public string? OwnerName { get; set; }

	[MaxLength(300)]
	public string? Address { get; set; }

	[MaxLength(20)]
	public string? PhoneNumber { get; set; }

	// رمز عشوائي ثابت يستخدم في رابط QR
	public Guid QrToken { get; set; } = Guid.NewGuid();

	public bool IsActive { get; set; } = true;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
