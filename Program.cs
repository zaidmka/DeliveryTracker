using DeliveryTracker;
using DeliveryTracker.Services.Auth;
using DeliveryTracker.Services.Deliveries;
using DeliveryTracker.Services.Items;
using DeliveryTracker.Services.Shops;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
// Authentication Service
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped(sp => new HttpClient
{
	BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});


builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<	AuthenticationStateProvider,ApiAuthenticationStateProvider>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
await builder.Build().RunAsync();
