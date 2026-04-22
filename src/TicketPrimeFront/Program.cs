using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TicketPrimeFront;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL base da API definida em wwwroot/appsettings.json (chave ApiBaseUrl).
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5246";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

await builder.Build().RunAsync();
