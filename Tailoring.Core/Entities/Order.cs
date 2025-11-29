using Tailoring.Core.Common;
using Tailoring.Core.Enums;

namespace Tailoring.Core.Entities
{
    public class Order : BaseEntity
    {
        public int CustomerId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string GarmentType { get; set; } = string.Empty;
        public string FabricType { get; set; } = string.Empty;
        public string StyleDetails { get; set; } = string.Empty;
        public string SpecialInstructions { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdvancePaid { get; set; }
        public OrderStatus Status { get; set; }
        public int Priority { get; set; }

        // Navigation properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual ICollection<OrderProgress> ProgressUpdates { get; set; } = new List<OrderProgress>();
    }
}