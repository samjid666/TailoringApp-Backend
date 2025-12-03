using Tailoring.Core.Common;

namespace Tailoring.Core.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? CustomerId { get; set; }

        // Navigation properties
        public virtual Customer? Customer { get; set; }
    }

    public enum UserRole
    {
        Admin = 1,
        Customer = 2
    }
}
