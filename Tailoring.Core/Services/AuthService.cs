using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tailoring.Core.DTOs;
using Tailoring.Core.Entities;
using Tailoring.Core.Interfaces;

namespace Tailoring.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Customer> _customerRepository;
        private readonly IConfiguration _configuration;

        public AuthService(
            IRepository<User> userRepository,
            IRepository<Customer> customerRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _customerRepository = customerRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Trim input
            var username = loginDto.Username?.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var users = await _userRepository.GetAsync(u => u.Username == username || u.Email == username);
            var user = users.FirstOrDefault();

            if (user == null || !VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CustomerId = user.CustomerId,
                ExpiresAt = expiresAt
            };
        }

        public async Task<LoginResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Trim and validate inputs
            var username = registerDto.Username?.Trim();
            var email = registerDto.Email?.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Username and email are required");
            }

            // Check if username or email already exists
            var existingUsers = await _userRepository.GetAsync(u =>
                u.Username.ToLower() == username.ToLower() || u.Email.ToLower() == email);

            if (existingUsers.Any())
            {
                var existingUser = existingUsers.First();
                if (existingUser.Username.ToLower() == username.ToLower())
                    throw new ArgumentException("Username already exists");
                if (existingUser.Email.ToLower() == email)
                    throw new ArgumentException("Email already exists");
            }

            // Create customer first
            var customer = new Customer
            {
                FirstName = registerDto.FirstName.Trim(),
                LastName = registerDto.LastName.Trim(),
                Email = email,
                Phone = registerDto.Phone.Trim(),
                Address = string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            var createdCustomer = await _customerRepository.AddAsync(customer);

            // Create user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(registerDto.Password),
                FirstName = registerDto.FirstName.Trim(),
                LastName = registerDto.LastName.Trim(),
                Phone = registerDto.Phone.Trim(),
                Role = UserRole.Customer,
                CustomerId = createdCustomer.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddDays(7);

            return new LoginResponseDto
            {
                Token = token,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role.ToString(),
                CustomerId = user.CustomerId,
                ExpiresAt = expiresAt
            };
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            var users = await _userRepository.GetAsync(u => u.Username == username);
            return users.FirstOrDefault();
        }

        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("CustomerId", user.CustomerId?.ToString() ?? "")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "TailoringApp",
                audience: _configuration["Jwt:Audience"] ?? "TailoringAppUsers",
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var hmac = new HMACSHA512();
            var salt = hmac.Key;
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            using var hmac = new HMACSHA512(salt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            return computedHash.SequenceEqual(hash);
        }
    }
}
