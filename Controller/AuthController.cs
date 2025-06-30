using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using BE_Phygens.Services;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IJwtService _jwtService;

        public AuthController(PhygensContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// User login
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(LoginRequestDto request)
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

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == request.Username);

                if (user == null)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Invalid username or password"));
                }

                var hashedPassword = HashPassword(request.Password);
                bool passwordValid = user.PasswordHash == hashedPassword || user.PasswordHash == request.Password;
                
                if (!passwordValid)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Invalid username or password"));
                }

                if (!user.IsActive)
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Account has been deactivated"));
                }

                var loginResponse = await _jwtService.GenerateTokenAsync(user);

                return Ok(ApiResponse<LoginResponseDto>.SuccessResult(
                    loginResponse, "Login successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred during login", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// User registration
        /// </summary>
        /// <param name="request">Registration data</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Register(CreateUserRequestDto request)
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

                var existingUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

                if (existingUser != null)
                {
                    var message = existingUser.Username == request.Username 
                        ? "Username already exists" 
                        : "Email already exists";
                    return BadRequest(ApiResponse<object>.ErrorResult(message));
                }

                if (!new[] { "student" }.Contains(request.Role.ToLower()))
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Invalid role. Only 'student' are allowed"));
                }

                var hashedPassword = HashPassword(request.Password);

                var user = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    Role = request.Role.ToLower(),
                    PasswordHash = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var loginResponse = await _jwtService.GenerateTokenAsync(user);

                return CreatedAtAction(nameof(Login), 
                    ApiResponse<LoginResponseDto>.SuccessResult(
                        loginResponse, "Registration successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred during registration", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Logout user (invalidate token)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<object>>> Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Invalid token"));
                }

                var token = Request.Headers["Authorization"]
                    .FirstOrDefault()?.Split(" ").Last();

                if (!string.IsNullOrEmpty(token))
                {
                    await _jwtService.RevokeTokenAsync(userId, token);
                }

                return Ok(ApiResponse<object>.SuccessResult(null, "Logout successful"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred during logout", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="request">Password change request</param>
        /// <returns>Success message</returns>
        [HttpPut("password")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordRequestDto request)
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

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Invalid token"));
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found"));
                }

                var currentPasswordHash = HashPassword(request.CurrentPassword);
                if (user.PasswordHash != currentPasswordHash)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Current password is incorrect"));
                }

                user.PasswordHash = HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResult(null, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while changing password", new List<string> { ex.Message }));
            }
        }

        private static string HashPassword(string password)
        {
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
} 