using FuturesBotKiler.Shared.Models;
using System.Collections.Generic;

namespace FuturesBotKiler.Shared
{
    public class Parametros
    {
        /*Binance*/
        public static Dictionary<int, Orden> Ordenes = new Dictionary<int, Orden>();

        /*Binance*/
        public static readonly string BinanceKeyKiler = "9qtqb883NM9N1Ar89UeDNZF08c60xadkBrEGEwmTGFvYbREPj8xNhmhKCcrEtfYD";
        public static readonly string BinanceSecretKiler = "GsTbtmXgJCXSWtnQY1iiYLBgJ1lqGOXGVT6h4pKXhv7CjgNEtBwvvBdLhDrCSVa9";
        public static readonly string BinanceKey = "DyeGKozK1THMUZ8SJz7trPBuSbHgHd3fkDvLbSUKOtWzgtJmnJp230TVkvMeHN8e";
        public static readonly string BinanceSecret = "Ocbd7nUXbTeQ1pXXhT0cRymyN5HKkFGceewuy6iadVbzWpKdfmvxwiT3nuT9r85x";
        public static readonly string BinanceSubKey = "hGkHeJgAEl6F7legsM9ey5UlP7BOBScLNauYdTddVRokhhclwjvL50VENpVSrFEG";
        public static readonly string BinanceSubSecret = "f6y0y5wGJEkA6uRszdO1JeYfEpBDqoMixelW3IFnlTJOOTIIkL71Tsyu86N4o65e";

        /*Azure*/ /*WEBJOBS_IDLE_TIMEOUT*/
        public static readonly string WebJobUsername = "$FuturesBotKilerApp";
        public static readonly string WebJobPassword = "ZrhK7GtWWiAMzbG70NmRafwgjN1WkLqdr0dEFPsPjXblrNq4Z2SLdhpqlJsb";
    }
}
