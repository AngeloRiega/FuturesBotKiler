using FuturesBotKiler.Shared.Models;

namespace EmptyBlazorApp1.Services.BinanceService
{
    public interface IBinanceService
    {
        //List<Position> Positions { get; set; }
        //Position Position { get; set; }
        //Task<Position> GetPosition(string Symbol);
        //Task GetPositions(string Symbol);
        Task<List<Position>> GetPositions(List<string> Symbol);
        Task<bool> ClosePosition(string Symbol);
        Task<bool> NewPosition(string symbol, bool side, decimal size, decimal porcentajeStopLoss, decimal porcentajeTakeProfit);
    }
}
