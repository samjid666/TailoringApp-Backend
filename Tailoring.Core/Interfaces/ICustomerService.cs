using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;

namespace Tailoring.Core.Interfaces
{
    public interface ICustomerService
    {
        Task<IReadOnlyList<Customer>> GetAllCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer> CreateCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
    }
}
