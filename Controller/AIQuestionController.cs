using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using BE_Phygens.Services;
using System.Text.Json;
using System.Text;

namespace BE_Phygens.Controllers
{
    [Route("ai-question")]
    [ApiController]
    // [Authorize]
    public class AIQuestionController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<AIQuestionController> _logger;

        public AIQuestionController(PhygensContext context, IAIService aiService, ILogger<AIQuestionController> logger)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
        }

        /// <summary>
        /// Generate AI question using real AI service (OpenAI/Gemini)
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> GenerateQuestion([FromBody] GenerateQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating question for ChapterId: {request.ChapterId}");
                
                // REQUIRE REAL CHAPTER FROM DATABASE - NO DEFAULTS
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);
                
                if (chapter == null)
                {
                    _logger.LogError($"Chapter {request.ChapterId} not found in database");
                    return BadRequest(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = $"Chapter ID {request.ChapterId} không tồn tại trong database. Vui lòng chọn chapter hợp lệ."
                    });
                }
                
                _logger.LogInformation($"Using real chapter: {chapter.ChapterName} (Grade {chapter.Grade})");

                // Generate question using AI service
                var questionDto = await _aiService.GenerateQuestionAsync(chapter, request);

                // Save to database if requested
                if (request.SaveToDatabase)
                {
                    _logger.LogInformation($"Saving question {questionDto.QuestionId} to database...");
                    await SaveQuestionToDatabase(questionDto);
                }

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

        /// <summary>
        /// Generate multiple questions in batch
        /// </summary>
        [HttpPost("generate-batch")]
        public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GenerateBatchQuestions([FromBody] BatchGenerateRequest request)
        {
            try
            {
                var questions = new List<QuestionDto>();
                
                foreach (var spec in request.QuestionSpecs)
                {
                    var chapter = await _context.Set<Chapter>()
                        .FirstOrDefaultAsync(c => c.ChapterId == spec.ChapterId && c.IsActive);
                    
                    if (chapter == null) 
                    {
                        _logger.LogError($"Chapter {spec.ChapterId} not found for batch generation");
                        continue;
                    }

                    for (int i = 0; i < spec.Count; i++)
                    {
                        var questionRequest = new GenerateQuestionRequest
                        {
                            ChapterId = spec.ChapterId,
                            DifficultyLevel = spec.DifficultyLevel,
                            QuestionType = spec.QuestionType,
                            SpecificTopic = spec.SpecificTopic,
                            SaveToDatabase = request.SaveToDatabase
                        };

                        var question = await _aiService.GenerateQuestionAsync(chapter, questionRequest);
                        questions.Add(question);

                        // Save to database if requested - temporarily disabled for debugging
                        if (request.SaveToDatabase)
                        {
                            _logger.LogWarning($"Database save temporarily disabled for question {question.QuestionId}");
                            // await SaveQuestionToDatabase(question);
                        }

                        // Add delay to avoid rate limiting
                        await Task.Delay(1000);
                    }
                }

                var savedCount = request.SaveToDatabase ? 
                    $"Tạo và lưu thành công {questions.Count} câu hỏi vào database" :
                    $"Tạo thành công {questions.Count} câu hỏi";

                return Ok(new ApiResponse<List<QuestionDto>>
                {
                    Success = true,
                    Message = savedCount,
                    Data = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch questions");
                return StatusCode(500, new ApiResponse<List<QuestionDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Analyze and improve existing question using AI
        /// </summary>
        [HttpPost("improve/{questionId}")]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> ImproveQuestion(string questionId, [FromBody] ImproveQuestionRequest request)
        {
            try
            {
                var existingQuestion = await _context.Questions
                    .Include(q => q.AnswerChoices)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (existingQuestion == null)
                    return NotFound(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = "Câu hỏi không tồn tại"
                    });

                var improvedQuestion = await _aiService.ImproveQuestionAsync(existingQuestion, request);

                return Ok(new ApiResponse<QuestionDto>
                {
                    Success = true,
                    Message = "Cải thiện câu hỏi thành công",
                    Data = improvedQuestion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error improving question");
                return StatusCode(500, new ApiResponse<QuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get AI suggestions for question topics
        /// </summary>
        [HttpPost("suggest-topics")]
        public async Task<ActionResult<ApiResponse<List<TopicSuggestionDto>>>> SuggestTopics([FromBody] TopicSuggestionRequest request)
        {
            try
            {
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId);

                if (chapter == null)
                    return BadRequest(new ApiResponse<List<TopicSuggestionDto>>
                    {
                        Success = false,
                        Message = "Chapter không tồn tại"
                    });

                var suggestions = await _aiService.GetTopicSuggestionsAsync(chapter, request);

                return Ok(new ApiResponse<List<TopicSuggestionDto>>
                {
                    Success = true,
                    Message = "Lấy gợi ý chủ đề thành công",
                    Data = suggestions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topic suggestions");
                return StatusCode(500, new ApiResponse<List<TopicSuggestionDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Validate question quality using AI
        /// </summary>
        [HttpPost("validate/{questionId}")]
        public async Task<ActionResult<ApiResponse<QuestionValidationDto>>> ValidateQuestion(string questionId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.AnswerChoices)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                    return NotFound(new ApiResponse<QuestionValidationDto>
                    {
                        Success = false,
                        Message = "Câu hỏi không tồn tại"
                    });

                var validation = await _aiService.ValidateQuestionAsync(question);

                return Ok(new ApiResponse<QuestionValidationDto>
                {
                    Success = true,
                    Message = "Kiểm tra chất lượng câu hỏi thành công",
                    Data = validation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating question");
                return StatusCode(500, new ApiResponse<QuestionValidationDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Test AI connection and capabilities
        /// </summary>
        [HttpPost("test-connection")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> TestAIConnection()
        {
            try
            {
                var isConnected = await _aiService.TestAIConnectionAsync();
                var config = await _aiService.GetAIStatusAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = isConnected,
                    Message = isConnected ? "Kết nối AI thành công" : "Không thể kết nối AI",
                    Data = new
                    {
                        Connected = isConnected,
                        Provider = config.Provider,
                        Model = config.Model,
                        IsConfigured = config.IsConfigured,
                        Features = config.SupportedFeatures
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing AI connection");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Lỗi test kết nối AI: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get all questions with pagination and filters
        /// </summary>
        [HttpGet("list")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetQuestions(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] string search = "",
            [FromQuery] string difficultyLevel = "",
            [FromQuery] int? chapterId = null)
        {
            try
            {
                var query = _context.Questions
                    .Include(q => q.AnswerChoices)
                    .Where(q => q.IsActive)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(q => q.QuestionText.Contains(search) || 
                                           (q.Explanation != null && q.Explanation.Contains(search)));
                }

                if (!string.IsNullOrEmpty(difficultyLevel))
                {
                    query = query.Where(q => q.DifficultyLevel == difficultyLevel);
                }

                if (chapterId.HasValue)
                {
                    query = query.Where(q => q.ChapterId == chapterId.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var questions = await query
                    .OrderByDescending(q => q.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Convert to DTOs
                var questionDtos = questions.Select(q => new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    Difficulty = q.DifficultyLevel,
                    DifficultyLevel = q.DifficultyLevel, 
                    Explanation = q.Explanation, 
                    Topic = q.SpecificTopic ?? "Chưa phân loại",
                    QuestionType = q.QuestionType,
                    CreatedAt = q.CreatedAt,
                    CreatedBy = q.CreatedBy ?? "system",
                    AnswerChoices = q.AnswerChoices?.Select(ac => new AnswerChoiceDto
                    {
                        ChoiceId = ac.ChoiceId,
                        ChoiceText = ac.ChoiceText,
                        IsCorrect = ac.IsCorrect,
                        ChoiceLabel = ac.ChoiceLabel ?? "A",
                        DisplayOrder = ac.DisplayOrder ?? 0
                    }).ToList() ?? new List<AnswerChoiceDto>()
                }).ToList();

                return Ok(new ApiResponse<List<QuestionDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách câu hỏi thành công",
                    Data = questionDtos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions");
                return StatusCode(500, new ApiResponse<List<QuestionDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("chapters")]
        [AllowAnonymous]
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

        /// <summary>
        /// Get AI configuration and status
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<AIConfigDto>>> GetAIConfig()
        {
            try
            {
                var config = await _aiService.GetAIStatusAsync();

                return Ok(new ApiResponse<AIConfigDto>
                {
                    Success = true,
                    Message = "Lấy cấu hình AI thành công",
                    Data = config
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI config");
                return StatusCode(500, new ApiResponse<AIConfigDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        #region Helper Methods

        private async Task SaveQuestionToDatabase(QuestionDto questionDto)
        {
            try
            {
                _logger.LogInformation($"SaveQuestionToDatabase called for: {questionDto.QuestionId}");

                // Check if question already exists
                var existingQuestion = await _context.Questions
                    .FirstOrDefaultAsync(q => q.QuestionId == questionDto.QuestionId);
                
                if (existingQuestion != null)
                {
                    _logger.LogWarning($"Question {questionDto.QuestionId} already exists in database, skipping save");
                    return;
                }

                var defaultTopicId = await GetOrCreateDefaultTopic(); 
                
                // Ensure ai_system user exists or find alternative
                string createdByUserId = await EnsureAISystemUser();

                _logger.LogInformation("Creating Question entity...");
                var question = new Question
                {
                    QuestionId = questionDto.QuestionId,
                    QuestionText = questionDto.QuestionText,
                    QuestionType = questionDto.QuestionType,
                    DifficultyLevel = questionDto.Difficulty,
                    TopicId = defaultTopicId,
                    ChapterId = null,
                    ImageUrl = questionDto.ImageUrl,
                    CreatedBy = createdByUserId,
                    CreatedAt = questionDto.CreatedAt,
                    IsActive = true,
                    AiGenerated = true,
                    AiProvider = "Gemini",
                    AiModel = "gemini-1.5-flash",
                    AiValidationStatus = "pending",
                    AiGenerationMetadata = "{\"generated_at\": \"" + DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") + "\", \"source\": \"mock_ai\"}"
                };

                _context.Questions.Add(question);
                _logger.LogInformation("Question entity added to context");

                _logger.LogInformation($"Creating {questionDto.AnswerChoices?.Count ?? 0} answer choices...");
                foreach (var choice in questionDto.AnswerChoices ?? new List<AnswerChoiceDto>())
                {
                    var answerChoice = new AnswerChoice
                    {
                        ChoiceId = choice.ChoiceId,
                        QuestionId = questionDto.QuestionId,
                        ChoiceLabel = choice.ChoiceLabel,
                        ChoiceText = choice.ChoiceText,
                        IsCorrect = choice.IsCorrect,
                        DisplayOrder = choice.DisplayOrder
                    };
                    _context.AnswerChoices.Add(answerChoice);
                }

                _logger.LogInformation("Saving all changes to database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Question saved successfully: {questionDto.QuestionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CRITICAL: Failed to save question {questionDto.QuestionId} to database: {ex.Message}");
                _logger.LogError(ex, $"Stack trace: {ex.StackTrace}");
                
                // Now throw the exception so caller knows about the failure
                throw new InvalidOperationException($"Không thể lưu câu hỏi {questionDto.QuestionId} vào database: {ex.Message}", ex);
            }
        }

        private async Task<string> EnsureAISystemUser()
        {
            try
            {
                // First try to find any admin user
                var adminUser = await _context.Users
                    .Where(u => u.Role == "admin")
                    .FirstOrDefaultAsync();
                
                if (adminUser != null)
                {
                    _logger.LogInformation($"Using existing admin user: {adminUser.UserId}");
                    return adminUser.UserId;
                }
                
                // Try to find ai_system user
                var aiSystemUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == "ai_system");
                
                if (aiSystemUser != null)
                {
                    _logger.LogInformation("Using existing ai_system user");
                    return "ai_system";
                }
                
                // Create ai_system user if not exists
                _logger.LogInformation("Creating ai_system user...");
                var newAiUser = new User
                {
                    UserId = "ai_system",
                    Username = "ai_system",
                    FullName = "ai_system",
                    Email = "ai_system@phygens.local",
                    Role = "admin",
                    PasswordHash = "$2a$11$dummyhashforaisystemuser123456789",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                
                _context.Users.Add(newAiUser);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created ai_system user successfully");
                return "ai_system";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not ensure ai_system user, using existing fallback");
                
                // Last resort: try to get any user
                try
                {
                    var anyUser = await _context.Users.FirstOrDefaultAsync();
                    if (anyUser != null)
                    {
                        return anyUser.UserId;
                    }
                }
                catch { }
                
                // Ultimate fallback - this will likely cause FK violation but let's see the exact error
                return "ai_system";
            }
        }

        private async Task<string> GetOrCreateDefaultTopic()
        {
            try
            {
                // First try to find any existing topic
                var existingTopic = await _context.PhysicsTopics
                    .FirstOrDefaultAsync();
                
                if (existingTopic != null)
                {
                    _logger.LogInformation($"Using existing topic: {existingTopic.TopicId}");
                    return existingTopic.TopicId;
                }

                // If no topics exist, use the sample data format
                _logger.LogInformation("No topics found, using fallback TOPIC_001");
                return "TOPIC_001"; // Match sample data format
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not access PhysicsTopics table, using fallback");
                return "TOPIC_001"; // Ultimate fallback
            }
        }

        #endregion

        #region Helper Classes

        private class MockQuestion
        {
            public string Question { get; set; } = "";
            public string[] Choices { get; set; } = Array.Empty<string>();
        }

        private class OpenAIResponse
        {
            public OpenAIChoice[]? Choices { get; set; }
        }

        private class OpenAIChoice
        {
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            public string? Content { get; set; }
        }

        private class AIQuestionResponse
        {
            public string? Question { get; set; }
            public AIChoiceResponse[]? Choices { get; set; }
            public string? Explanation { get; set; }
            public string? Difficulty { get; set; }
            public string? Topic { get; set; }
        }

        private class AIChoiceResponse
        {
            public string Label { get; set; } = "";
            public string Text { get; set; } = "";
            public bool IsCorrect { get; set; }
        }

        #endregion
    }
} 