using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using BE_Phygens.Services;
using System.Security.Cryptography;
using System.Text;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("users")]
    [Produces("application/json")]
    public class UsersController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IJwtService _jwtService;

        public UsersController(PhygensContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        /// <summary>
        /// Get paginated list of users (Admin only)
        /// </summary>
        /// <param name="request">Pagination and filter parameters</param>
        /// <returns>Paginated list of users</returns>
        [HttpGet]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<UserResponseDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<PaginatedResponse<UserResponseDto>>>> GetUsers([FromQuery] PaginationRequest request)
        {
            try
            {
                var query = _context.Users.AsNoTracking();

                // Chỉ lấy các user đang active
                query = query.Where(u => u.IsActive);

                // Search functionality
                if (!string.IsNullOrEmpty(request.Search))
                {
                    query = query.Where(u => 
                        u.Username.Contains(request.Search) ||
                        u.Email.Contains(request.Search) ||
                        u.FullName.Contains(request.Search));
                }

                // Sorting
                query = request.SortBy?.ToLower() switch
                {
                    "username" => request.SortDirection.ToLower() == "desc" 
                        ? query.OrderByDescending(u => u.Username)
                        : query.OrderBy(u => u.Username),
                    "email" => request.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.Email)
                        : query.OrderBy(u => u.Email),
                    "createdat" => request.SortDirection.ToLower() == "desc"
                        ? query.OrderByDescending(u => u.CreatedAt)
                        : query.OrderBy(u => u.CreatedAt),
                    _ => query.OrderBy(u => u.Username)
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                var users = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(u => new UserResponseDto
                    {
                        Id = u.UserId,
                        Username = u.Username,
                        Email = u.Email,
                        FullName = u.FullName,
                        Role = u.Role,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync();

                var result = new PaginatedResponse<UserResponseDto>
                {
                    Items = users,
                    TotalCount = totalCount,
                    PageSize = request.PageSize,
                    CurrentPage = request.Page,
                    TotalPages = totalPages,
                    HasNext = request.Page < totalPages,
                    HasPrevious = request.Page > 1
                };

                return Ok(ApiResponse<PaginatedResponse<UserResponseDto>>.SuccessResult(
                    result, "Users retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while retrieving users", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>User details</returns>
        [HttpGet("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> GetUser(string id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Users can only view their own profile unless they're admin
                if (currentUserId != id && currentUserRole != "admin")
                {
                    return Forbid();
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found"));
                }

                var userDto = new UserResponseDto
                {
                    Id = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserResponseDto>.SuccessResult(
                    userDto, "User retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while retrieving user", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>Current user profile with additional statistics</returns>
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserProfileResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponse<UserProfileResponseDto>>> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResult("Invalid token"));
                }

                var user = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.CreatedExams)
                    .Include(u => u.CreatedQuestions)
                    .Include(u => u.StudentAttempts)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found"));
                }

                var userProfile = new UserProfileResponseDto
                {
                    Id = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    CreatedExamsCount = user.CreatedExams.Count,
                    CreatedQuestionsCount = user.CreatedQuestions.Count,
                    AttemptsCount = user.StudentAttempts.Count
                };

                return Ok(ApiResponse<UserProfileResponseDto>.SuccessResult(
                    userProfile, "Profile retrieved successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while retrieving profile", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Create a new user (Admin only)
        /// </summary>
        /// <param name="request">User creation data</param>
        /// <returns>Created user</returns>
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> CreateUser(CreateUserRequestDto request)
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

                // Check if username or email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Username or email already exists"));
                }

                // Hash password (simple MD5 for now, should use bcrypt in production)
                var hashedPassword = HashPassword(request.Password);

                var user = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Username = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    Role = request.Role,
                    PasswordHash = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userDto = new UserResponseDto
                {
                    Id = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.UserId },
                    ApiResponse<UserResponseDto>.SuccessResult(userDto, "User created successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while creating user", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Update user information
        /// </summary>
        /// <param name="id">User ID</param>
        /// <param name="request">Update data</param>
        /// <returns>Updated user</returns>
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<UserResponseDto>>> UpdateUser(string id, UpdateUserRequestDto request)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                // Users can only update their own profile unless they're admin
                if (currentUserId != id && currentUserRole != "admin")
                {
                    return Forbid();
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found"));
                }

                // Update only provided fields
                if (!string.IsNullOrEmpty(request.FullName))
                    user.FullName = request.FullName;

                if (!string.IsNullOrEmpty(request.Email))
                {
                    // Check if email is already taken by another user
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == request.Email && u.UserId != id);
                    if (emailExists)
                    {
                        return BadRequest(ApiResponse<object>.ErrorResult("Email already exists"));
                    }
                    user.Email = request.Email;
                }

                // Only admin can change active status
                if (request.IsActive.HasValue && currentUserRole == "admin")
                    user.IsActive = request.IsActive.Value;

                await _context.SaveChangesAsync();

                var userDto = new UserResponseDto
                {
                    Id = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                };

                return Ok(ApiResponse<UserResponseDto>.SuccessResult(
                    userDto, "User updated successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while updating user", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="request">Password change data</param>
        /// <returns>Success message</returns>
        [HttpPut("me/passwords")]
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

                // Verify current password
                var currentPasswordHash = HashPassword(request.CurrentPassword);
                if (user.PasswordHash != currentPasswordHash)
                {
                    return BadRequest(ApiResponse<object>.ErrorResult("Current password is incorrect"));
                }

                // Update password
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

        /// <summary>
        /// Delete user (Admin only)
        /// </summary>
        /// <param name="id">User ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<ApiResponse<object>>> DeleteUser(string id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResult("User not found"));
                }

                // Soft delete by setting IsActive to false
                user.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResult(null, "User deleted successfully"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResult(
                    "An error occurred while deleting user", new List<string> { ex.Message }));
            }
        }

        private static string HashPassword(string password)
        {
            // Simple MD5 hash - should use bcrypt or Argon2 in production
            using var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
} 