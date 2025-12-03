using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;

namespace Tailoring.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<User?> GetUserByUsernameAsync(string username);
        string GenerateJwtToken(User user);
    }
}
