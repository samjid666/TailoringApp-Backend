using Tailoring.Core.Common;

namespace Tailoring.Core.Entities
{
    public class Measurement : BaseEntity
    {
        public int CustomerId { get; set; }
        public string MeasurementType { get; set; } = string.Empty; // e.g., Shirt, Pant, Suit
        public DateTime TakenOn { get; set; }

        // Measurement values (in cm)
        public decimal Chest { get; set; }
        public decimal Waist { get; set; }
        public decimal Hip { get; set; }
        public decimal Shoulder { get; set; }
        public decimal ArmLength { get; set; }
        public decimal Inseam { get; set; }
        public decimal Neck { get; set; }
        public string? AdditionalNotes { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
    }
}