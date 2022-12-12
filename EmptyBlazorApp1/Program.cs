global using EmptyBlazorApp1.Services.BinanceService;
global using EmptyBlazorApp1.Services.PositionService;
global using FuturesBotKiler.Shared;
using EmptyBlazorApp1;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

//Add services to the container.
ConfigureServices(builder.Services);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void ConfigureServices(IServiceCollection services)
{
    services.AddRazorPages();
    services.AddServerSideBlazor();
    services.AddScoped<IBinanceService, BinanceService>();
    services.AddHttpClient<BinanceService>();
    services.AddHttpClient("futuresbotkiler", c =>
    {
        c.BaseAddress = new Uri("https://futuresbotkilerapp.azurewebsites.net/");
        //c.BaseAddress = new Uri("http://localhost:5207/");
    });
    services
        .AddBlazorise(options =>
        {
            options.Immediate = true;
        })
        .AddBootstrap5Providers()
        .AddFontAwesomeIcons();
    services.AddSingleton<PositionService>();
}