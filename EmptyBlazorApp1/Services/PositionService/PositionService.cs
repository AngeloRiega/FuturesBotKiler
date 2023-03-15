namespace EmptyBlazorApp1.Services.PositionService
{
    public class PositionService
    {
        public List<string> Watchlist = new List<string>();

        //TODO: NO REPETIDOS EN EL WATCHLIST (PASARLO A DICCIONARIO)

        public void AddWatchlist(string symbol)
        {
            Watchlist.Add(symbol);
        }

        public void DeleteWatchlist(string symbol)
        {
            Watchlist.Remove(symbol);
        }
    }
}