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
    [Authorize]
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
                // Validate Chapter exists
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);
                
                if (chapter == null)
                    return BadRequest(new ApiResponse<QuestionDto> 
                    { 
                        Success = false, 
                        Message = "Chapter không tồn tại" 
                    });

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
        [Authorize(Roles = "admin")]
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
        [Authorize(Roles = "admin")]
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
            var question = new Question
            {
                QuestionId = questionDto.QuestionId,
                QuestionText = questionDto.QuestionText,
                QuestionType = questionDto.QuestionType,
                DifficultyLevel = questionDto.Difficulty,
                TopicId = questionDto.Topic, // Use TopicId instead of Topic
                ImageUrl = questionDto.ImageUrl,
                CreatedBy = questionDto.CreatedBy,
                CreatedAt = questionDto.CreatedAt,
                IsActive = true
            };

            _context.Questions.Add(question);

            foreach (var choice in questionDto.AnswerChoices)
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

            await _context.SaveChangesAsync();
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