using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using System.Text.Json;
using System.Text;

namespace BE_Phygens.Controllers
{
    [Route("api/ai-question")]
    [ApiController]
    [Authorize]
    public class AIQuestionController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIQuestionController> _logger;

        public AIQuestionController(PhygensContext context, IConfiguration configuration, HttpClient httpClient, ILogger<AIQuestionController> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> GenerateQuestion([FromBody] GenerateQuestionRequest request)
        {
            try
            {
                // Validate Chapter exists
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);
                
                if (chapter == null)
                    return BadRequest(new ApiResponse<QuestionDto> 
                    { 
                        Success = false, 
                        Message = "Chapter không tồn tại" 
                    });

                // For demo - create mock AI question
                var questionDto = CreateMockQuestion(chapter, request);

                return Ok(new ApiResponse<QuestionDto>
                {
                    Success = true,
                    Message = "Tạo câu hỏi AI thành công",
                    Data = questionDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI question");
                return StatusCode(500, new ApiResponse<QuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("chapters")]
        public async Task<ActionResult<ApiResponse<List<Chapter>>>> GetChapters()
        {
            try
            {
                var chapters = await _context.Set<Chapter>()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Grade)
                    .ThenBy(c => c.DisplayOrder)
                    .ToListAsync();

                return Ok(new ApiResponse<List<Chapter>>
                {
                    Success = true,
                    Message = "Lấy danh sách chương thành công",
                    Data = chapters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters");
                return StatusCode(500, new ApiResponse<List<Chapter>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        private QuestionDto CreateMockQuestion(Chapter chapter, GenerateQuestionRequest request)
        {
            var questionId = Guid.NewGuid().ToString();
            
            // Mock questions based on chapter and difficulty
            var mockQuestions = GetMockQuestionsByChapter(chapter, request.DifficultyLevel);
            var random = new Random();
            var selectedQuestion = mockQuestions[random.Next(mockQuestions.Count)];

            return new QuestionDto
            {
                QuestionId = questionId,
                Topic = chapter.ChapterName,
                QuestionText = selectedQuestion.Question,
                QuestionType = request.QuestionType,
                Difficulty = request.DifficultyLevel,
                ImageUrl = "",
                CreatedBy = "AI_System",
                CreatedAt = DateTime.UtcNow,
                AnswerChoices = selectedQuestion.Choices.Select((choice, index) => new AnswerChoiceDto
                {
                    ChoiceId = Guid.NewGuid().ToString(),
                    ChoiceLabel = ((char)('A' + index)).ToString(),
                    ChoiceText = choice
                }).ToList()
            };
        }

        private List<MockQuestion> GetMockQuestionsByChapter(Chapter chapter, string difficulty)
        {
            return chapter.ChapterName.ToLower() switch
            {
                "cơ học" when difficulty == "easy" => new List<MockQuestion>
                {
                    new() { Question = "Chuyển động thẳng đều có đặc điểm gì?", Choices = new[] { "Vận tốc không đổi", "Gia tốc không đổi", "Lực không đổi", "Quãng đường không đổi" }},
                    new() { Question = "Đơn vị của vận tốc trong hệ SI là gì?", Choices = new[] { "m/s", "km/h", "m/s²", "N" }}
                },
                "cơ học" when difficulty == "medium" => new List<MockQuestion>
                {
                    new() { Question = "Một vật chuyển động thẳng đều với vận tốc 20 m/s. Quãng đường vật đi được trong 5s là?", Choices = new[] { "100m", "25m", "4m", "15m" }},
                    new() { Question = "Công thức tính động năng là?", Choices = new[] { "Ek = 1/2mv²", "Ek = mgh", "Ek = Fs", "Ek = mv" }}
                },
                "nhiệt học" when difficulty == "easy" => new List<MockQuestion>
                {
                    new() { Question = "Đơn vị của nhiệt độ trong hệ SI là gì?", Choices = new[] { "Kelvin (K)", "Celsius (°C)", "Fahrenheit (°F)", "Joule (J)" }},
                    new() { Question = "Quá trình nào sau đây là đẳng tích?", Choices = new[] { "Thể tích không đổi", "Áp suất không đổi", "Nhiệt độ không đổi", "Khối lượng không đổi" }}
                },
                _ => new List<MockQuestion>
                {
                    new() { Question = $"Câu hỏi mẫu về {chapter.ChapterName} - độ khó {difficulty}", Choices = new[] { "Đáp án A", "Đáp án B", "Đáp án C", "Đáp án D" }}
                }
            };
        }

        private class MockQuestion
        {
            public string Question { get; set; } = "";
            public string[] Choices { get; set; } = Array.Empty<string>();
        }
    }
} 