using System.ComponentModel.DataAnnotations;
using Tailoring.Core.Enums;

namespace Tailoring.Core.DTOs
{
    public class CreateOrderDto
    {
        // Either provide CustomerId OR customer details (firstName, lastName, email, phone)
        public int? CustomerId { get; set; }

        // Customer details for creating new customer (if CustomerId is not provided)
        public string? CustomerFirstName { get; set; }
        public string? CustomerLastName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }

        [Required(ErrorMessage = "Garment type is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Garment type must be between 2 and 100 characters")]
        public string GarmentType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Fabric type is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Fabric type must be between 2 and 100 characters")]
        public string FabricType { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Style details cannot exceed 500 characters")]
        public string StyleDetails { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Special instructions cannot exceed 1000 characters")]
        public string SpecialInstructions { get; set; } = string.Empty;

        [Required(ErrorMessage = "Due date is required")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Total amount must be non-negative")]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Advance paid must be non-negative")]
        public decimal AdvancePaid { get; set; }

        [Range(1, 3, ErrorMessage = "Priority must be 1 (Normal), 2 (High), or 3 (Urgent)")]
        public int Priority { get; set; } = 3;

        // Measurements
        [Range(0, 500, ErrorMessage = "Chest measurement must be between 0 and 500 inches")]
        public decimal? Chest { get; set; }

        [Range(0, 500, ErrorMessage = "Waist measurement must be between 0 and 500 inches")]
        public decimal? Waist { get; set; }

        [Range(0, 500, ErrorMessage = "Hip measurement must be between 0 and 500 inches")]
        public decimal? Hip { get; set; }

        [Range(0, 500, ErrorMessage = "Shoulder measurement must be between 0 and 500 inches")]
        public decimal? Shoulder { get; set; }

        [Range(0, 500, ErrorMessage = "Arm length must be between 0 and 500 inches")]
        public decimal? ArmLength { get; set; }

        [Range(0, 500, ErrorMessage = "Inseam measurement must be between 0 and 500 inches")]
        public decimal? Inseam { get; set; }

        [Range(0, 500, ErrorMessage = "Neck measurement must be between 0 and 500 inches")]
        public decimal? Neck { get; set; }

        [StringLength(500, ErrorMessage = "Measurement notes cannot exceed 500 characters")]
        public string? MeasurementNotes { get; set; }
    }

    public class UpdateOrderDto
    {
        public string? GarmentType { get; set; }
        public string? FabricType { get; set; }
        public string? StyleDetails { get; set; }
        public string? SpecialInstructions { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? AdvancePaid { get; set; }
        public OrderStatus? Status { get; set; }
        public int? Priority { get; set; }
    }

    public class OrderListDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string GarmentType { get; set; } = string.Empty;
        public string FabricType { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdvancePaid { get; set; }
        public decimal Balance { get; set; }
        public int Priority { get; set; }
    }

    public class PaginatedOrdersDto
    {
        public List<OrderListDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
