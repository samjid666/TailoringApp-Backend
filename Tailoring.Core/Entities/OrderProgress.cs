using Tailoring.Core.Common;
using Tailoring.Core.Enums;

namespace Tailoring.Core.Entities
{
    public class OrderProgress : BaseEntity
    {
        public int OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
        
        // Navigation property
        public virtual Order Order { get; set; } = null!;
    }
}
