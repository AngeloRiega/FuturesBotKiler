using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using FuturesBotKiler.Shared;

BinanceClient.SetDefaultOptions(new BinanceClientOptions
{
    //ApiCredentials = new ApiCredentials(Parametros.BinanceSubKey, Parametros.BinanceSubSecret)
    ApiCredentials = new ApiCredentials(Parametros.BinanceKeyKiler, Parametros.BinanceSecretKiler)
});
BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions
{
    //ApiCredentials = new ApiCredentials(Parametros.BinanceSubKey, Parametros.BinanceSubSecret)
    ApiCredentials = new ApiCredentials(Parametros.BinanceKeyKiler, Parametros.BinanceSecretKiler)
});

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();