using DeliveryTracker.Shared.Dtos.Shops;

namespace DeliveryTracker.Services.Shops;

public interface IShopService
{
	Task<List<ShopDto>> GetAllAsync();

	Task<ShopDto?> GetByIdAsync(int id);

	Task CreateAsync(SaveShopRequest request);

	Task UpdateAsync(int id, SaveShopRequest request);
}
