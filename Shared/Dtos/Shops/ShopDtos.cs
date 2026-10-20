
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Dtos.Shops;

public class ShopDto
{
	public int Id { get; set; }
	public string Code { get; set; } = "";
	public string Name { get; set; } = "";
	public string? OwnerName { get; set; }
	public string? Address { get; set; }
	public string? PhoneNumber { get; set; }
	public Guid QrToken { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class SaveShopRequest
{
	[Required(ErrorMessage = "اسم المحل مطلوب")]
	[StringLength(200)]
	public string Name { get; set; } = "";

	[StringLength(200)]
	public string? OwnerName { get; set; }

	[StringLength(300)]
	public string? Address { get; set; }

	[StringLength(20)]
	public string? PhoneNumber { get; set; }

	public bool IsActive { get; set; } = true;
}