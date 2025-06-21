using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BE_Phygens;
using BE_Phygens.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BE_Phygens.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public UsersController(PhygensContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }
        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(string id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == id)
                .Select(u => new UserDto
                {
                    UserId = int.Parse(u.UserId),
                    Name = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    // ...map các trường khác nếu cần...
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // GET: api/users/me
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetMe()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => new UserDto
                {
                    UserId = int.Parse(u.UserId),
                    Name = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    // ...map các trường khác nếu cần...
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        // GET: api/Users/5

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // GET: api/Users/profile/{id}
        [HttpGet("profile/{id}")]
        public async Task<ActionResult<User>> ViewProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginModel loginUser)
        {
            if (loginUser == null || string.IsNullOrEmpty(loginUser.Name) || string.IsNullOrEmpty(loginUser.Password))
            {
                return BadRequest("Invalid login data");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginUser.Name);

            if (user == null || user.PasswordHash != loginUser.Password)
            {
                return Unauthorized("Invalid username or password");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Sử dụng cùng logic như trong Programs.cs để lấy JWT key
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                           Environment.GetEnvironmentVariable("JWT_KEY") ?? 
                           _configuration["Jwt:SecretKey"];
            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT_KEY or JWT_SECRET_KEY environment variable is not configured.");
            }
            
            var key = Encoding.ASCII.GetBytes(secretKey);
            
            // Lấy issuer và audience từ environment variables như trong Programs.cs
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");

            //Claim là thông tin mã hóa từ token
            var claims = new List<Claim>
    {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                userId = user.UserId
            });
        }

        public class LoginModel
        {
            public string Name { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] User newUser)
        {
            // Kiểm tra model validation
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new
                    {
                        Field = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                    })
                    .ToArray();

                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(newUser.Username) ||
                string.IsNullOrWhiteSpace(newUser.FullName) ||
                string.IsNullOrWhiteSpace(newUser.Email) ||
                string.IsNullOrWhiteSpace(newUser.PasswordHash))
            {
                return BadRequest("All fields are required.");
            }

            // Kiểm tra email & username đã tồn tại chưa
            if (await _context.Users.AnyAsync(u => u.Email == newUser.Email))
            {
                return Conflict("Email already in use.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == newUser.Username))
            {
                return Conflict("Username already in use.");
            }

            // Tạo UserId mới nếu UserId là string (ví dụ: "1", "2", "3" → tăng tiếp)
            var maxUserIdStr = await _context.Users
    .OrderByDescending(u => u.UserId)
    .Select(u => u.UserId)
    .FirstOrDefaultAsync();

            int maxId = 0;
            if (!string.IsNullOrEmpty(maxUserIdStr) && int.TryParse(maxUserIdStr, out int parsed))
            {
                maxId = parsed;
            }
            newUser.UserId = (maxId + 1).ToString("D3");
            _context.Users.Add(newUser);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Ghi log nếu cần
                return StatusCode(500, "An error occurred while registering the user. Please try again.");
            }

            // Không trả về mật khẩu
            newUser.PasswordHash = null;

            return CreatedAtAction("GetUser", new { id = newUser.UserId }, newUser);
        }


        // PUT: api/Users/update/{id}




        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id.ToString() != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }




        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromForm] string FullName, [FromForm] string Email)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.FullName = FullName;
            user.Email = Email;



            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(user);
        }
        private async Task<string> SaveImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException("Invalid file");
            }

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + fileName;
        }










        // POST: api/Users
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        public class ForgotPasswordModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
        [HttpPost("forgot-password")]

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId.Equals(id));
        }
        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return BadRequest("Invalid user ID");
            }

            var user = await _context.Users
                .AsNoTracking()
                .Select(u => new UserDto
                {
                    UserId = int.Parse(u.UserId), // Convert string to int
                    Name = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                })
                .FirstOrDefaultAsync(u => u.UserId == int.Parse(userId)); // Ensure comparison is done with int

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }



        [Authorize]
        [HttpPut("address")]
        public async Task<IActionResult> UpdateUserProfile2([FromBody] UserUpdateModel updatedUser)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound("User not found");
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = "Profile updated successfully", user = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        public class UserDto
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string UserType { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public DateTime? RegistrationDate { get; set; }
        }
        public class UserUpdateModel
        {
            public string? Name { get; set; }
            public string? FullName { get; set; }
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public string? WardCode { get; set; }
            public int? DistrictId { get; set; }
        }
    }
}