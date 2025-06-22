using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Services;
using BE_Phygens.Dto;

namespace BE_Phygens.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class LoginGoogleController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginGoogleController> _logger;

        public LoginGoogleController(PhygensContext context, IJwtService jwtService, ILogger<LoginGoogleController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _logger = logger;
        }

        public class FirebaseLoginRequest
        {
            public string? IdToken { get; set; }
            public string? Email { get; set; }
            public string? FullName { get; set; }
        }

        /// <summary>
        /// Firebase/Google login - Xử lý đăng nhập từ Firebase Authentication
        /// </summary>
        [HttpPost("firebase-login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> FirebaseLogin([FromBody] FirebaseLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Email is required"));
                }
                
                _logger.LogInformation($"Firebase login for: {request.Email}");

                // Find user by email
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    _logger.LogInformation($"New user detected: {request.Email}");
                    return Ok(ApiResponse<object>.SuccessResult(new
                    {
                        IsNewUser = true,
                        Email = request.Email,
                        Name = request.FullName
                    }, "New user detected, registration required"));
                }

                _logger.LogInformation($"Existing user found: {user.Email}, UserId: {user.UserId}");
                
                // Sử dụng IJwtService thay vì tự implement
                var loginResponse = await _jwtService.GenerateTokenAsync(user);
                
                return Ok(ApiResponse<LoginResponseDto>.SuccessResult(
                    loginResponse, "Firebase login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Firebase authentication");
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred during authentication", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Hoàn tất đăng ký cho user mới từ Google/Firebase
        /// </summary>
        [HttpPost("complete-registration")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .SelectMany(x => x.Value!.Errors)
                        .Select(x => x.ErrorMessage)
                        .ToList();
                    return BadRequest(ApiResponse<object>.ErrorResult("Validation failed", errors));
                }

                _logger.LogInformation($"Completing registration for: {request.Email}");

                // Check if user already exists
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("User already exists"));
                }

                var user = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Email = request.Email,
                    FullName = request.FullName,
                    Username = request.Email.Split('@')[0], // Generate username from email
                    PasswordHash = string.Empty, // Google users don't have password
                    Role = "student", // Default role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user created: {user.Email}, UserId: {user.UserId}");
                
                // Sử dụng IJwtService
                var loginResponse = await _jwtService.GenerateTokenAsync(user);
                
                return Ok(ApiResponse<LoginResponseDto>.SuccessResult(
                    loginResponse, "Registration completed successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration");
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred during registration", new List<string> { ex.Message }));
            }
        }

        public class CompleteRegistrationRequest
        {
            public string Email { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Phone { get; set; }
            public string? Address { get; set; }
        }
    }
}