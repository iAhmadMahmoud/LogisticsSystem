namespace LogisticsSystem.Api.Contracts.Shipments
{
    public sealed class AddShipmentLocationRequest
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }
}
