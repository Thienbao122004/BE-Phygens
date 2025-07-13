using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_Phygens.Dto;
using BE_Phygens.Services;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Controllers
{
    [Route("/essay")]
    [ApiController]
    // [Authorize]
    public class EssayController : ControllerBase
    {
        private readonly IEssayGradingService _essayService;
        private readonly PhygensContext _context;
        private readonly ILogger<EssayController> _logger;

        public EssayController(
            IEssayGradingService essayService,
            PhygensContext context,
            ILogger<EssayController> logger)
        {
            _essayService = essayService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo câu hỏi tự luận bằng AI
        /// POST: api/essay/generate
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponse<EssayQuestionDto>>> GenerateEssayQuestion([FromBody] GenerateEssayQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"Tạo câu hỏi tự luận cho chương {request.ChapterId}");

                // Kiểm tra chapter tồn tại
                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);

                if (chapter == null)
                {
                    return BadRequest(new ApiResponse<EssayQuestionDto>
                    {
                        Success = false,
                        Message = "Chapter không tồn tại"
                    });
                }

                // Tạo câu hỏi tự luận
                var essayQuestion = await _essayService.GenerateEssayQuestionAsync(chapter, request);

                // Lưu vào database nếu được yêu cầu
                if (request.SaveToDatabase)
                {
                    await SaveEssayQuestionToDatabase(essayQuestion);
                }

                return Ok(new ApiResponse<EssayQuestionDto>
                {
                    Success = true,
                    Message = "Tạo câu hỏi tự luận thành công",
                    Data = essayQuestion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi tự luận");
                return StatusCode(500, new ApiResponse<EssayQuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Chấm điểm bài tự luận
        /// POST: api/essay/grade
        /// </summary>
        [HttpPost("grade")]
        public async Task<ActionResult<ApiResponse<EssayGradingResultDto>>> GradeEssay([FromBody] EssayAnswerSubmissionDto submission)
        {
            try
            {
                _logger.LogInformation($"Chấm điểm bài tự luận cho câu hỏi {submission.QuestionId}");

                // Validate đầu vào
                if (string.IsNullOrWhiteSpace(submission.StudentAnswer))
                {
                    return BadRequest(new ApiResponse<EssayGradingResultDto>
                    {
                        Success = false,
                        Message = "Câu trả lời không được để trống"
                    });
                }

                // Chấm điểm
                var gradingResult = await _essayService.GradeEssayAsync(
                    submission.QuestionId, 
                    submission.StudentAnswer, 
                    submission.StudentId);

                return Ok(new ApiResponse<EssayGradingResultDto>
                {
                    Success = true,
                    Message = "Chấm điểm thành công",
                    Data = gradingResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm điểm bài tự luận");
                return StatusCode(500, new ApiResponse<EssayGradingResultDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Chấm điểm hàng loạt bài tự luận
        /// POST: api/essay/batch-grade
        /// </summary>
        [HttpPost("batch-grade")]
        public async Task<ActionResult<ApiResponse<List<EssayGradingResultDto>>>> BatchGradeEssays([FromBody] EssayBatchGradingRequest request)
        {
            try
            {
                _logger.LogInformation($"Chấm điểm hàng loạt {request.Submissions.Count} bài tự luận");

                var results = await _essayService.BatchGradeEssaysAsync(request);

                return Ok(new ApiResponse<List<EssayGradingResultDto>>
                {
                    Success = true,
                    Message = $"Chấm điểm thành công {results.Count} bài tự luận",
                    Data = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm điểm hàng loạt");
                return StatusCode(500, new ApiResponse<List<EssayGradingResultDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Phân tích bài viết tự luận
        /// POST: api/essay/analyze
        /// </summary>
        [HttpPost("analyze")]
        public async Task<ActionResult<ApiResponse<EssayAnalysisDto>>> AnalyzeEssay([FromBody] AnalyzeEssayRequest request)
        {
            try
            {
                var analysis = await _essayService.AnalyzeEssayAsync(request.Text);

                return Ok(new ApiResponse<EssayAnalysisDto>
                {
                    Success = true,
                    Message = "Phân tích thành công",
                    Data = analysis
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích bài viết");
                return StatusCode(500, new ApiResponse<EssayAnalysisDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Tạo phản hồi cho bài tự luận
        /// POST: api/essay/feedback
        /// </summary>
        [HttpPost("feedback")]
        public async Task<ActionResult<ApiResponse<string>>> GenerateFeedback([FromBody] GenerateFeedbackRequest request)
        {
            try
            {
                var feedback = await _essayService.GenerateEssayFeedbackAsync(
                    request.QuestionId, 
                    request.StudentAnswer, 
                    request.Score);

                return Ok(new ApiResponse<string>
                {
                    Success = true,
                    Message = "Tạo phản hồi thành công",
                    Data = feedback
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phản hồi");
                return StatusCode(500, new ApiResponse<string>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Validate câu trả lời tự luận
        /// POST: api/essay/validate
        /// </summary>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateEssayAnswer([FromBody] ValidateEssayRequest request)
        {
            try
            {
                var isValid = await _essayService.ValidateEssayAnswerAsync(
                    request.Answer, 
                    request.MinWords, 
                    request.MaxWords);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Message = isValid ? "Câu trả lời hợp lệ" : "Câu trả lời không hợp lệ",
                    Data = isValid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi validate câu trả lời");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lấy danh sách câu hỏi tự luận
        /// GET: api/essay/questions
        /// </summary>
        [HttpGet("questions")]
        public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetEssayQuestions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string search = "",
            [FromQuery] string difficultyLevel = "",
            [FromQuery] int? chapterId = null)
        {
            try
            {
                var query = _context.Questions
                    .Where(q => q.QuestionType == "essay" && q.IsActive)
                    .Include(q => q.Topic)
                    .Include(q => q.Chapter)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(q => q.QuestionText.Contains(search));
                }

                if (!string.IsNullOrEmpty(difficultyLevel))
                {
                    query = query.Where(q => q.DifficultyLevel == difficultyLevel);
                }

                if (chapterId.HasValue)
                {
                    query = query.Where(q => q.ChapterId == chapterId.Value);
                }

                var totalCount = await query.CountAsync();
                var questions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(q => new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Difficulty = q.DifficultyLevel,
                        Topic = q.Topic.TopicName,
                        ChapterId = q.ChapterId ?? 0,
                        CreatedBy = q.CreatedBy,
                        CreatedAt = q.CreatedAt,
                        Explanation = q.Explanation
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<QuestionDto>>
                {
                    Success = true,
                    Message = $"Lấy danh sách {questions.Count}/{totalCount} câu hỏi tự luận",
                    Data = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách câu hỏi tự luận");
                return StatusCode(500, new ApiResponse<List<QuestionDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết câu hỏi tự luận
        /// GET: api/essay/questions/{id}
        /// </summary>
        [HttpGet("questions/{questionId}")]
        public async Task<ActionResult<ApiResponse<EssayQuestionDto>>> GetEssayQuestionDetail(string questionId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .Include(q => q.Chapter)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId && q.QuestionType == "essay");

                if (question == null)
                {
                    return NotFound(new ApiResponse<EssayQuestionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi tự luận"
                    });
                }

                var essayQuestion = new EssayQuestionDto
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Difficulty = question.DifficultyLevel,
                    Topic = question.Topic?.TopicName ?? "",
                    ChapterId = question.ChapterId ?? 0,
                    CreatedBy = question.CreatedBy,
                    CreatedAt = question.CreatedAt,
                    Explanation = question.Explanation,
                    // Essay specific fields
                    MaxWords = 500, // Default values - có thể lưu trong metadata
                    MinWords = 50,
                    GradingCriteria = new List<Dto.EssayGradingCriteria>() // Load from metadata if available
                };

                return Ok(new ApiResponse<EssayQuestionDto>
                {
                    Success = true,
                    Message = "Lấy chi tiết câu hỏi thành công",
                    Data = essayQuestion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết câu hỏi");
                return StatusCode(500, new ApiResponse<EssayQuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Xóa câu hỏi tự luận
        /// DELETE: api/essay/questions/{id}
        /// </summary>
        [HttpDelete("questions/{questionId}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteEssayQuestion(string questionId)
        {
            try
            {
                var question = await _context.Questions
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId && q.QuestionType == "essay");

                if (question == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi tự luận"
                    });
                }

                // Soft delete
                question.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa câu hỏi thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa câu hỏi");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        // ========== PRIVATE METHODS ==========

        private async Task SaveEssayQuestionToDatabase(EssayQuestionDto essayQuestion)
        {
            try
            {
                var question = new Question
                {
                    QuestionId = essayQuestion.QuestionId,
                    QuestionText = essayQuestion.QuestionText,
                    QuestionType = "essay",
                    DifficultyLevel = essayQuestion.Difficulty,
                    TopicId = await GetOrCreateDefaultTopic(),
                    ChapterId = essayQuestion.ChapterId,
                    CreatedBy = await EnsureAISystemUser(),
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AiGenerated = true,
                    AiProvider = "OpenAI",
                    Explanation = essayQuestion.SampleAnswer,
                    // Lưu essay-specific data vào metadata
                    AiGenerationMetadata = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        maxWords = essayQuestion.MaxWords,
                        minWords = essayQuestion.MinWords,
                        keyPoints = essayQuestion.KeyPoints,
                        gradingCriteria = essayQuestion.GradingCriteria,
                        gradingRubric = essayQuestion.GradingRubric
                    })
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã lưu câu hỏi tự luận {essayQuestion.QuestionId} vào database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu câu hỏi tự luận vào database");
                throw;
            }
        }

        private async Task<string> GetOrCreateDefaultTopic()
        {
            var defaultTopic = await _context.PhysicsTopics
                .FirstOrDefaultAsync(t => t.TopicName == "Câu hỏi tự luận");

            if (defaultTopic == null)
            {
                defaultTopic = new PhysicsTopic
                {
                    TopicId = Guid.NewGuid().ToString(),
                    TopicName = "Câu hỏi tự luận",
                    Description = "Chủ đề cho các câu hỏi tự luận được tạo bởi AI",
                    GradeLevel = "10-12",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PhysicsTopics.Add(defaultTopic);
                await _context.SaveChangesAsync();
            }

            return defaultTopic.TopicId;
        }

        private async Task<string> EnsureAISystemUser()
        {
            var aiUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == "AI_SYSTEM");

            if (aiUser == null)
            {
                aiUser = new User
                {
                    UserId = Guid.NewGuid().ToString(),
                    Username = "AI_SYSTEM",
                    Email = "ai@phygens.system",
                    FullName = "AI System",
                    Role = "admin",
                    PasswordHash = "N/A",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };
                _context.Users.Add(aiUser);
                await _context.SaveChangesAsync();
            }

            return aiUser.UserId;
        }
    }

    // ========== REQUEST/RESPONSE DTOs ==========
    public class AnalyzeEssayRequest
    {
        public string Text { get; set; } = string.Empty;
    }

    public class GenerateFeedbackRequest
    {
        public string QuestionId { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public double Score { get; set; }
    }

    public class ValidateEssayRequest
    {
        public string Answer { get; set; } = string.Empty;
        public int MinWords { get; set; }
        public int MaxWords { get; set; }
    }
} 