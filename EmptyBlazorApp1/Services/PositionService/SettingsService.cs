namespace EmptyBlazorApp1.Services.PositionService
{
    public class SettingsService
    {
        private string? etapaMaxima = "15";
        private string? porcentajeStopLoss = "0.95";
        private string? porcentajeTakeProfit = "2.75";
        private string? size = "30";

        public string? EtapaMaxima { get => etapaMaxima; set => etapaMaxima = value; }
        public string? PorcentajeStopLoss { get => porcentajeStopLoss; set => porcentajeStopLoss = value; }
        public string? PorcentajeTakeProfit { get => porcentajeTakeProfit; set => porcentajeTakeProfit = value; }
        public string? Size { get => size; set => size = value; }
    }
}