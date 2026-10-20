
namespace DeliveryTracker.Shared.Models;

public class DeliveryItem
{
	public int Id { get; set; }

	public int DeliveryId { get; set; }

	public Delivery Delivery { get; set; } = null!;

	public int ItemTypeId { get; set; }

	public ItemType ItemType { get; set; } = null!;

	public int Quantity { get; set; }
}
