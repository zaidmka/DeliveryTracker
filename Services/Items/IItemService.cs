
using DeliveryTracker.Shared.Dtos.Items;

namespace DeliveryTracker.Services.Items;

public interface IItemService
{
	Task<List<ItemDto>> GetAllAsync(bool activeOnly = false);
	Task CreateAsync(SaveItemRequest request);
	Task UpdateAsync(int id, SaveItemRequest request);
}
