using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BE_Phygens.Controllers
{
    [Route("/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly ILogger<UploadController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public UploadController(ILogger<UploadController> logger, IConfiguration configuration, HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Upload file qua backend để tránh CORS issues
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file, string folder = "uploads")
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "Không có file được tải lên" });
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return BadRequest(new { error = "File quá lớn. Kích thước tối đa 10MB" });
                }

                // Validate file type
                var allowedTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedTypes.Contains(fileExtension))
                {
                    return BadRequest(new { error = $"Loại file không được hỗ trợ. Chỉ chấp nhận: {string.Join(", ", allowedTypes)}" });
                }

                _logger.LogInformation($"📤 Uploading file: {file.FileName} ({file.Length} bytes) to folder: {folder}");

                // Clean filename
                var cleanFileName = Path.GetFileNameWithoutExtension(file.FileName)
                    .Replace(" ", "_")
                    .Replace("%", "")
                    .Replace("#", "")
                    .Replace("&", "");

                var uniqueFileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{cleanFileName}{fileExtension}";

                // Try multiple upload strategies
                string uploadUrl = null;
                Exception lastException = null;

                // Strategy 1: Local storage (fallback)
                try
                {
                    uploadUrl = await UploadToLocalStorage(file, folder, uniqueFileName);
                    if (!string.IsNullOrEmpty(uploadUrl))
                    {
                        return Ok(new { 
                            success = true, 
                            url = uploadUrl, 
                            method = "local_storage",
                            filename = uniqueFileName 
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Local storage upload failed");
                    lastException = ex;
                }

                // Strategy 2: External service proxy (if needed)
                try
                {
                    uploadUrl = await UploadViaProxy(file, folder, uniqueFileName);
                    if (!string.IsNullOrEmpty(uploadUrl))
                    {
                        return Ok(new { 
                            success = true, 
                            url = uploadUrl, 
                            method = "proxy_upload",
                            filename = uniqueFileName 
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Proxy upload failed");
                    lastException = ex;
                }

                // All strategies failed
                _logger.LogError(lastException, "All upload strategies failed");
                return StatusCode(500, new { 
                    error = "Upload thất bại", 
                    details = lastException?.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file upload");
                return StatusCode(500, new { 
                    error = "Lỗi server khi upload file", 
                    details = ex.Message 
                });
            }
        }

        /// <summary>
        /// Upload file to local storage (wwwroot/uploads)
        /// </summary>
        private async Task<string> UploadToLocalStorage(IFormFile file, string folder, string fileName)
        {
            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
                
                // Create directory if it doesn't exist
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation($"📁 Created directory: {uploadsPath}");
                }

                var fullPath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the URL path
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{folder}/{fileName}";

                _logger.LogInformation($"✅ Local upload successful: {fileUrl}");
                return fileUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local storage upload failed");
                throw;
            }
        }

        /// <summary>
        /// Upload via external proxy service (placeholder)
        /// </summary>
        private async Task<string> UploadViaProxy(IFormFile file, string folder, string fileName)
        {
            try
            {
                // This is a placeholder for external service integration
                // You can integrate with Cloudinary, AWS S3, etc.
                
                await Task.Delay(100); // Simulate async operation
                
                _logger.LogWarning("⚠️ Proxy upload not implemented yet");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proxy upload failed");
                throw;
            }
        }

        /// <summary>
        /// Get uploaded file info
        /// </summary>
        [HttpGet("info/{folder}/{filename}")]
        [AllowAnonymous]
        public IActionResult GetFileInfo(string folder, string filename)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder, filename);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "File không tồn tại" });
                }

                var fileInfo = new FileInfo(filePath);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var fileUrl = $"{baseUrl}/uploads/{folder}/{filename}";

                return Ok(new
                {
                    filename = filename,
                    size = fileInfo.Length,
                    created = fileInfo.CreationTime,
                    url = fileUrl,
                    exists = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info");
                return StatusCode(500, new { error = "Lỗi lấy thông tin file" });
            }
        }

        /// <summary>
        /// Delete uploaded file
        /// </summary>
        [HttpDelete("{folder}/{filename}")]
        public IActionResult DeleteFile(string folder, string filename)
        {
            try
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder, filename);
                
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { error = "File không tồn tại" });
                }

                System.IO.File.Delete(filePath);
                _logger.LogInformation($"🗑️ Deleted file: {filePath}");

                return Ok(new { success = true, message = "Xóa file thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return StatusCode(500, new { error = "Lỗi xóa file" });
            }
        }

        /// <summary>
        /// List uploaded files in folder
        /// </summary>
        [HttpGet("list/{folder?}")]
        [AllowAnonymous]
        public IActionResult ListFiles(string folder = "uploads")
        {
            try
            {
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", folder);
                
                if (!Directory.Exists(uploadsPath))
                {
                    return Ok(new { files = new string[0], count = 0 });
                }

                var files = Directory.GetFiles(uploadsPath)
                    .Select(f => new
                    {
                        filename = Path.GetFileName(f),
                        size = new FileInfo(f).Length,
                        created = new FileInfo(f).CreationTime,
                        url = $"{Request.Scheme}://{Request.Host}/uploads/{folder}/{Path.GetFileName(f)}"
                    })
                    .OrderByDescending(f => f.created)
                    .Take(50) // Limit to 50 files
                    .ToList();

                return Ok(new { files = files, count = files.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return StatusCode(500, new { error = "Lỗi liệt kê files" });
            }
        }

        /// <summary>
        /// 🌤️ Delete image from Cloudinary
        /// </summary>
        [HttpDelete("cloudinary/delete")]
        [AllowAnonymous]
        public async Task<IActionResult> DeleteFromCloudinary([FromBody] CloudinaryDeleteRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.PublicId))
                {
                    return BadRequest(new { error = "PublicId is required" });
                }

                // Get Cloudinary config from environment
                var cloudName = _configuration["Cloudinary:CloudName"];
                var apiKey = _configuration["Cloudinary:ApiKey"];
                var apiSecret = _configuration["Cloudinary:ApiSecret"];

                if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    return BadRequest(new { error = "Cloudinary configuration not found" });
                }

                // Create timestamp and signature for authentication
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                var stringToSign = $"public_id={request.PublicId}&timestamp={timestamp}{apiSecret}";
                var signature = ComputeSha1Hash(stringToSign);

                // Prepare form data
                var formData = new List<KeyValuePair<string, string>>
                {
                    new("public_id", request.PublicId),
                    new("timestamp", timestamp),
                    new("api_key", apiKey),
                    new("signature", signature)
                };

                var formContent = new FormUrlEncodedContent(formData);

                // Call Cloudinary delete API
                var deleteUrl = $"https://api.cloudinary.com/v1_1/{cloudName}/image/destroy";
                var response = await _httpClient.PostAsync(deleteUrl, formContent);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return StatusCode(500, new { error = $"Cloudinary delete failed: {errorContent}" });
                }

                var result = await response.Content.ReadAsStringAsync();
                return Ok(new { success = true, message = "Image deleted successfully", result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary");
                return StatusCode(500, new { error = "Failed to delete image" });
            }
        }

        /// <summary>
        /// 🔐 Generate SHA1 hash for Cloudinary signature
        /// </summary>
        private static string ComputeSha1Hash(string input)
        {
            using var sha1 = System.Security.Cryptography.SHA1.Create();
            var hash = sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLower();
        }
    }

    public class CloudinaryDeleteRequest
    {
        public string PublicId { get; set; } = "";
    }
} 