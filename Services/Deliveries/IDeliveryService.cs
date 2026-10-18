
using DeliveryTracker.Shared.Dtos.Deliveries;
using DeliveryTracker.Shared.Dtos.Shops;

namespace DeliveryTracker.Services.Deliveries;

public interface IDeliveryService
{
	Task<ShopDto?> GetShopByQrAsync(Guid token);

	Task<int> CreateAsync(CreateDeliveryRequest request);
}
