using FuturesBotKiler.Blazor.Pages;
using FuturesBotKiler.Shared.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace FuturesBotKiler.Blazor.Services.BinanceService
{
    public class BinanceService : IBinanceService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BinanceService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Position>> GetPositions(List<string> symbol)
        {
            List<Position> positions = new List<Position>();

            foreach (var item in symbol)
            {
                var client = _httpClientFactory.CreateClient("futuresbotkiler");
                var result = await client.GetFromJsonAsync<List<Position>>($"binance/positions/{item}");

                if (result != null)
                {
                    positions.Add(result[0]);
                }
            }

            return positions;
        }

        public async Task<bool> ClosePosition(string symbol)
        {
            var client = _httpClientFactory.CreateClient("futuresbotkiler");
            var result = await client.DeleteFromJsonAsync<bool>($"binance/positions/{symbol}/close");

            return result;   
        }

        public async Task<bool> NewPosition(string symbol, bool side, decimal size, decimal porcentajeStopLoss, decimal porcentajeTakeProfit)
        {
            string? errorMessage;

            var postBody = new
            {
                side = side ? "SELL" : "BUY",
                size,
                symbol,
                porcentajeStopLoss,
                porcentajeTakeProfit
            };
            var client = _httpClientFactory.CreateClient("futuresbotkiler");
            var response = await client.PostAsJsonAsync("binance/alertatradingview", postBody);

            if (!response.IsSuccessStatusCode)
            {
                // set error message for display, log to console and return
                errorMessage = response.ReasonPhrase;
                Console.WriteLine($"There was an error! {errorMessage}");
                return false;
            }
            else
            {
                return true;
            }
            // convert response data to JsonElement which can handle any JSON data
            //var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        }
    }
}
