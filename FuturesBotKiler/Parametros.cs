namespace FuturesBotKiler
{
    public class Parametros
    {
        /*Binance*/
        public static int Etapa = 0;
        public static decimal Quantity = 40; //CANTIDAD EN USD CONSIDERANDO EL LEVERAGE
        public static int ClientId = 1; //USAR 0 PARA REINICIAR EL ID Y 1 PARA CONTINUAR
        public static decimal MarketPriceOrdenBot; //OBTENGO PRECIO MARKET DE LA ORDEN RECIEN CREADA
        public static string Symbol = "BTCUSDT";
        public static decimal PorcentajeTakeProfit = 0.15m;
        public static decimal PorcentajeStopLoss = 0.15m;

        /*Binance*/
        public static readonly string BinanceKeyKiler = "9qtqb883NM9N1Ar89UeDNZF08c60xadkBrEGEwmTGFvYbREPj8xNhmhKCcrEtfYD";
        public static readonly string BinanceSecretKiler = "GsTbtmXgJCXSWtnQY1iiYLBgJ1lqGOXGVT6h4pKXhv7CjgNEtBwvvBdLhDrCSVa9";
        public static readonly string BinanceKey = "DyeGKozK1THMUZ8SJz7trPBuSbHgHd3fkDvLbSUKOtWzgtJmnJp230TVkvMeHN8e";
        public static readonly string BinanceSecret = "Ocbd7nUXbTeQ1pXXhT0cRymyN5HKkFGceewuy6iadVbzWpKdfmvxwiT3nuT9r85x";

        /*FTX*/
        public static readonly string FTXKeyKiler = "";
        public static readonly string FTXSecretKiler = "";
        public static readonly string FTXKey = "Lh79ksICwKW0SZpN3B4xQvzXE3-kGheV1Ia1KZXN";
        public static readonly string FTXSecret = "ySztKxgWb3eOrT8Z9Ipg_o8U2mH0ZglEmuC7RHYJ";
    }
}
