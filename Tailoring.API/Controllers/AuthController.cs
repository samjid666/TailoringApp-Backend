


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tailoring.Core.DTOs;
using Tailoring.Core.Interfaces;

namespace Tailoring.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _authService.LoginAsync(loginDto);
                _logger.LogInformation("User {Username} logged in successfully", loginDto.Username);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Failed login attempt for user {Username}", loginDto.Username);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Username}", loginDto.Username);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { message = "Validation failed", errors });
            }

            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                _logger.LogInformation("User {Username} registered successfully", registerDto.Username);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Registration failed for {Username}: {Error}", registerDto.Username, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Username}", registerDto.Username);
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpGet("validate")]
        [Authorize]
        public ActionResult ValidateToken()
        {
            var username = User.Identity?.Name;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return Ok(new { valid = true, username, role });
        }
    }
}
