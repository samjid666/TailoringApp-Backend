using System.Security.Cryptography;
using System.Text;
using Tailoring.Core.Entities;
using Tailoring.Core.Enums;

namespace Tailoring.Infrastructure.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Check if we already have data
            if (context.Customers.Any())
            {
                return; // Database has been seeded
            }

            // Create admin user first
            var adminPasswordHash = HashPassword("admin123");
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@tailoring.com",
                PasswordHash = adminPasswordHash,
                FirstName = "Admin",
                LastName = "User",
                Phone = "+1234567890",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Create sample customers
            var customers = new List<Customer>
            {
                new Customer
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@email.com",
                    Phone = "+1234567890",
                    Address = "123 Main Street, City, State 12345",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@email.com",
                    Phone = "+1234567891",
                    Address = "456 Oak Avenue, City, State 12345",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    FirstName = "Robert",
                    LastName = "Johnson",
                    Email = "robert.johnson@email.com",
                    Phone = "+1234567892",
                    Address = "789 Pine Road, City, State 12345",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    FirstName = "Emily",
                    LastName = "Brown",
                    Email = "emily.brown@email.com",
                    Phone = "+1234567893",
                    Address = "321 Elm Street, City, State 12345",
                    CreatedAt = DateTime.UtcNow
                },
                new Customer
                {
                    FirstName = "Michael",
                    LastName = "Davis",
                    Email = "michael.davis@email.com",
                    Phone = "+1234567894",
                    Address = "654 Maple Drive, City, State 12345",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // Create sample orders
            var orders = new List<Order>
            {
                new Order
                {
                    CustomerId = customers[0].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-001",
                    GarmentType = "Three-Piece Suit",
                    FabricType = "Wool Blend",
                    StyleDetails = "Classic fit with vest, notch lapel",
                    SpecialInstructions = "Extra pocket on vest",
                    OrderDate = DateTime.UtcNow.AddDays(-5),
                    DueDate = DateTime.UtcNow.AddDays(10),
                    TotalAmount = 450.00m,
                    AdvancePaid = 200.00m,
                    Status = OrderStatus.Stitching,
                    Priority = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Order
                {
                    CustomerId = customers[1].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-002",
                    GarmentType = "Wedding Dress",
                    FabricType = "Silk Satin",
                    StyleDetails = "A-line silhouette with lace details",
                    SpecialInstructions = "Needs beading on bodice",
                    OrderDate = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(30),
                    TotalAmount = 1200.00m,
                    AdvancePaid = 500.00m,
                    Status = OrderStatus.Pending,
                    Priority = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Order
                {
                    CustomerId = customers[2].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-003",
                    GarmentType = "Casual Shirt",
                    FabricType = "Cotton",
                    StyleDetails = "Slim fit with button-down collar",
                    SpecialInstructions = "Contrast buttons",
                    OrderDate = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(7),
                    TotalAmount = 80.00m,
                    AdvancePaid = 40.00m,
                    Status = OrderStatus.MeasurementTaken,
                    Priority = 3,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new Order
                {
                    CustomerId = customers[3].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-004",
                    GarmentType = "Formal Blazer",
                    FabricType = "Velvet",
                    StyleDetails = "Single-breasted, peak lapel",
                    SpecialInstructions = "Burgundy color",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(14),
                    TotalAmount = 320.00m,
                    AdvancePaid = 150.00m,
                    Status = OrderStatus.Finishing,
                    Priority = 2,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new Order
                {
                    CustomerId = customers[4].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-005",
                    GarmentType = "Trousers",
                    FabricType = "Linen",
                    StyleDetails = "Straight leg, flat front",
                    SpecialInstructions = "Cuffed hem",
                    OrderDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(8),
                    TotalAmount = 120.00m,
                    AdvancePaid = 60.00m,
                    Status = OrderStatus.Pending,
                    Priority = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new Order
                {
                    CustomerId = customers[0].Id,
                    OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-006",
                    GarmentType = "Tuxedo",
                    FabricType = "Wool",
                    StyleDetails = "Shawl lapel, single button",
                    SpecialInstructions = "Satin details",
                    OrderDate = DateTime.UtcNow.AddDays(-7),
                    DueDate = DateTime.UtcNow.AddDays(5),
                    TotalAmount = 650.00m,
                    AdvancePaid = 650.00m,
                    Status = OrderStatus.ReadyForDelivery,
                    Priority = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-7)
                }
            };

            context.Orders.AddRange(orders);
            await context.SaveChangesAsync();

            // Create sample measurements
            var measurements = new List<Measurement>
            {
                new Measurement
                {
                    CustomerId = customers[0].Id,
                    MeasurementType = "Suit",
                    TakenOn = DateTime.UtcNow.AddDays(-10),
                    Chest = 102,
                    Waist = 86,
                    Hip = 97,
                    Shoulder = 43,
                    ArmLength = 64,
                    Inseam = 81,
                    Neck = 40,
                    AdditionalNotes = "Regular fit measurements",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Measurement
                {
                    CustomerId = customers[1].Id,
                    MeasurementType = "Dress",
                    TakenOn = DateTime.UtcNow.AddDays(-8),
                    Chest = 91,
                    Waist = 71,
                    Hip = 97,
                    Shoulder = 38,
                    ArmLength = 58,
                    Inseam = 0,
                    Neck = 36,
                    AdditionalNotes = "Wedding dress measurements",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Measurement
                {
                    CustomerId = customers[2].Id,
                    MeasurementType = "Shirt",
                    TakenOn = DateTime.UtcNow.AddDays(-5),
                    Chest = 97,
                    Waist = 81,
                    Hip = 0,
                    Shoulder = 41,
                    ArmLength = 62,
                    Inseam = 0,
                    Neck = 39,
                    AdditionalNotes = "Slim fit preferred",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            context.Measurements.AddRange(measurements);
            await context.SaveChangesAsync();

            // Create customer users
            for (int i = 0; i < customers.Count; i++)
            {
                var customerUser = new User
                {
                    Username = $"customer{i + 1}",
                    Email = customers[i].Email,
                    PasswordHash = HashPassword("password123"),
                    FirstName = customers[i].FirstName,
                    LastName = customers[i].LastName,
                    Phone = customers[i].Phone,
                    Role = UserRole.Customer,
                    CustomerId = customers[i].Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Users.Add(customerUser);
            }
            await context.SaveChangesAsync();
        }

        private static string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }
    }
}
