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
                
                // Validate Chapter exists
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);
                
                if (chapter == null)
                {
                    _logger.LogWarning($"Chapter {request.ChapterId} not found, creating default chapter");
                    
                    // Create a default chapter for AI generation
                    chapter = new Chapter
                    {
                        ChapterId = request.ChapterId,
                        ChapterName = "Chương học mặc định",
                        Grade = 10,
                        Description = "Chương học được tạo tự động cho AI",
                        DisplayOrder = 1,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                }

                // Generate question using AI service
                var questionDto = await _aiService.GenerateQuestionAsync(chapter, request);

                // Save to database if requested
                if (request.SaveToDatabase)
                {
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
                    
                    if (chapter == null) continue;

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

                        // Add delay to avoid rate limiting
                        await Task.Delay(1000);
                    }
                }

                return Ok(new ApiResponse<List<QuestionDto>>
                {
                    Success = true,
                    Message = $"Tạo thành công {questions.Count} câu hỏi",
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
                
                // TEMPORARY FIX: Disable database save to avoid PhysicsTopics compatibility issues
                // Frontend still works - questions are generated and used to create exams
                // Database persistence can be fixed later
                
                _logger.LogInformation("Database save temporarily disabled - question data still available for exam creation");
                
                // TODO: Fix PhysicsTopics table compatibility and re-enable database save
                // The AI generation still works, questions are used in memory for exam creation
                
                return; // Skip database save for now
                
                /*
                // Original save logic - commented out temporarily
                var defaultTopicId = "TOPIC_001"; 
                
                User? aiUser = null;
                try
                {
                    _logger.LogInformation("Searching for ai_system user...");
                    aiUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == "ai_system");
                    
                    if (aiUser == null)
                    {
                        _logger.LogInformation("ai_system not found, searching for admin users...");
                        aiUser = await _context.Users
                            .FirstOrDefaultAsync(u => u.Role == "admin");
                    }
                    _logger.LogInformation($"Found user: {aiUser?.UserId ?? "NULL"}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "User query failed");
                }

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
                    CreatedBy = aiUser?.UserId ?? "ai_system",
                    CreatedAt = questionDto.CreatedAt,
                    IsActive = true,
                    AiGenerated = true,
                    AiProvider = "Gemini",
                    AiModel = "gemini-1.5-flash",
                    AiValidationStatus = "pending"
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
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SaveQuestionToDatabase: {ex.Message}");
                // Don't throw - let the process continue without database save
                _logger.LogWarning("Continuing without database save - AI generation still functional");
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