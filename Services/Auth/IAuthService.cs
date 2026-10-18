using DeliveryTracker.Shared.Dtos;
namespace DeliveryTracker.Services.Auth
{
	public interface IAuthService
	{
		Task<bool> LoginAsync(string username, string password);
		Task LogoutAsync();
		Task<AuthUserDto?> GetCurrentUserAsync();
	}
}
