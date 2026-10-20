
using System.ComponentModel.DataAnnotations;

namespace DeliveryTracker.Shared.Models;

public class ItemType
{
	public int Id { get; set; }

	[Required, MaxLength(150)]
	public string Name { get; set; } = string.Empty;

	[MaxLength(300)]
	public string? Description { get; set; }

	public bool IsActive { get; set; } = true;

	public ICollection<DeliveryItem> DeliveryItems { get; set; }
		= new List<DeliveryItem>();
}
