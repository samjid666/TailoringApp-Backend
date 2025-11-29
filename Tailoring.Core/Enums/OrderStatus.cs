namespace Tailoring.Core.Enums
{
    public enum OrderStatus
    {
        Pending = 1,
        MeasurementTaken = 2,
        Cutting = 3,
        Stitching = 4,
        Finishing = 5,
        QualityCheck = 6,
        ReadyForDelivery = 7,
        Delivered = 8,
        Cancelled = 9
    }
}