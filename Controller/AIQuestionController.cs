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
    // [Authorize] // Bỏ auth để test
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
        /// POST: ai-questions/generate
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuestion([FromBody] GenerateQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating question for ChapterId: {request.ChapterId}");
                
                Chapter chapter;
                
                // Handle special cases
                if (request.ChapterId <= 0)
                {
                    // Get available chapters and suggest one
                    var availableChapters = await _context.Chapters
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Grade)
                        .ThenBy(c => c.DisplayOrder)
                        .Take(5)
                        .ToListAsync();
                    
                    if (!availableChapters.Any())
                    {
                        return BadRequest(new { 
                            error = "No chapters available", 
                            message = "Không có chapter nào trong database. Vui lòng seed sample data hoặc tạo chapters trước." 
                        });
                    }
                    
                    var chapterList = string.Join(", ", availableChapters.Select(c => $"ID={c.ChapterId} ({c.ChapterName})"));
                    return BadRequest(new { 
                        error = "Invalid chapter ID", 
                        message = $"ChapterId phải > 0. Các chapters có sẵn: {chapterList}" 
                    });
                }
                
                // Find specific chapter
                chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);
                
                if (chapter == null)
                {
                    // Get available chapters for helpful error message
                    var availableChapters = await _context.Chapters
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Grade)
                        .ThenBy(c => c.DisplayOrder)
                        .Take(10)
                        .ToListAsync();
                    
                    var chapterList = availableChapters.Any() 
                        ? string.Join(", ", availableChapters.Select(c => $"ID={c.ChapterId} ({c.ChapterName})"))
                        : "Không có chapters nào. Vui lòng seed sample data.";
                    
                    _logger.LogError($"Chapter {request.ChapterId} not found in database");
                    return BadRequest(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = $"Chapter ID {request.ChapterId} không tồn tại. Các chapters có sẵn: {chapterList}"
                    });
                }
                
                _logger.LogInformation($"Using chapter: {chapter.ChapterName} (Grade {chapter.Grade})");

                // Generate question using AI service
                var questionDto = await _aiService.GenerateQuestionAsync(chapter, request);

                // Save to database if requested
                if (request.SaveToDatabase)
                {
                    _logger.LogInformation($"Saving question {questionDto.QuestionId} to database...");
                    await SaveQuestionToDatabase(questionDto);
                }

                return Ok(new {
                    questionId = questionDto.QuestionId,
                    questionText = questionDto.QuestionText,
                    questionType = questionDto.QuestionType,
                    difficulty = questionDto.Difficulty,
                    topic = questionDto.Topic,
                    chapterId = questionDto.ChapterId,
                    answerChoices = questionDto.AnswerChoices?.Select(ac => new {
                        choiceLabel = ac.ChoiceLabel,
                        choiceText = ac.ChoiceText,
                        isCorrect = ac.IsCorrect
                    }).ToList(),
                    explanation = questionDto.Explanation,
                    createdBy = questionDto.CreatedBy,
                    createdAt = questionDto.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI question");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
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
                    var chapter = await _context.Chapters
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
                var chapter = await _context.Chapters
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
        /// GET: ai-question
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<QuestionListResponse>>> GetQuestions(
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
                    .Include(q => q.Topic)
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
                    Topic = q.Topic?.TopicName ?? q.SpecificTopic ?? "Chưa phân loại",
                    QuestionType = q.QuestionType,
                    CreatedAt = q.CreatedAt,
                    CreatedBy = q.CreatedBy ?? "system",
                    ChapterId = q.ChapterId ?? 0,
                    ImageUrl = q.ImageUrl ?? "",
                    AnswerChoices = q.AnswerChoices?.Select(ac => new AnswerChoiceDto
                    {
                        ChoiceId = ac.ChoiceId,
                        ChoiceText = ac.ChoiceText,
                        IsCorrect = ac.IsCorrect,
                        ChoiceLabel = ac.ChoiceLabel ?? "A",
                        DisplayOrder = ac.DisplayOrder
                    }).ToList() ?? new List<AnswerChoiceDto>()
                }).ToList();

                var response = new QuestionListResponse
                {
                    Questions = questionDtos,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(new ApiResponse<QuestionListResponse>
                {
                    Success = true,
                    Message = $"Lấy {questionDtos.Count} câu hỏi thành công",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting questions");
                return StatusCode(500, new ApiResponse<QuestionListResponse>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Create new question
        /// POST: ai-question
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> CreateQuestion([FromBody] GenerateQuestionRequest request)
        {
            try
            {
                if (request.ChapterId <= 0)
                {
                    return BadRequest(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = "ChapterId phải lớn hơn 0"
                    });
                }

                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.ChapterId == request.ChapterId && c.IsActive);

                if (chapter == null)
                {
                    return NotFound(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = "Chapter không tồn tại"
                    });
                }

                var question = await _aiService.GenerateQuestionAsync(chapter, request);

                if (request.SaveToDatabase)
                {
                    await SaveQuestionToDatabase(question);
                }

                return Ok(new ApiResponse<QuestionDto>
                {
                    Success = true,
                    Message = "Tạo câu hỏi thành công",
                    Data = question
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question");
                return StatusCode(500, new ApiResponse<QuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get question by ID
        /// GET: ai-question/{id}
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> GetQuestionById(string id)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.AnswerChoices)
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == id && q.IsActive);

                if (question == null)
                {
                    return NotFound(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = "Câu hỏi không tồn tại"
                    });
                }

                var questionDto = new QuestionDto
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    QuestionType = question.QuestionType,
                    Difficulty = question.DifficultyLevel,
                    DifficultyLevel = question.DifficultyLevel,
                    Topic = question.Topic?.TopicName ?? "Chưa phân loại",
                    ImageUrl = question.ImageUrl ?? "",
                    CreatedBy = question.CreatedBy,
                    CreatedAt = question.CreatedAt,
                    Explanation = question.Explanation,
                    AnswerChoices = question.AnswerChoices?.Select(ac => new AnswerChoiceDto
                    {
                        ChoiceId = ac.ChoiceId,
                        ChoiceLabel = ac.ChoiceLabel ?? "A",
                        ChoiceText = ac.ChoiceText,
                        IsCorrect = ac.IsCorrect,
                        DisplayOrder = ac.DisplayOrder
                    }).ToList() ?? new List<AnswerChoiceDto>()
                };

                return Ok(new ApiResponse<QuestionDto>
                {
                    Success = true,
                    Message = "Lấy câu hỏi thành công",
                    Data = questionDto
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question {QuestionId}", id);
                return StatusCode(500, new ApiResponse<QuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Update question
        /// PUT: ai-question/{id}
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<QuestionDto>>> UpdateQuestion(string id, [FromBody] UpdateQuestionRequest request)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.AnswerChoices)
                    .FirstOrDefaultAsync(q => q.QuestionId == id && q.IsActive);

                if (question == null)
                {
                    return NotFound(new ApiResponse<QuestionDto>
                    {
                        Success = false,
                        Message = "Câu hỏi không tồn tại"
                    });
                }

                // Update question properties
                if (!string.IsNullOrEmpty(request.QuestionText))
                    question.QuestionText = request.QuestionText;

                if (!string.IsNullOrEmpty(request.DifficultyLevel))
                    question.DifficultyLevel = request.DifficultyLevel;

                if (!string.IsNullOrEmpty(request.Explanation))
                    question.Explanation = request.Explanation;

                // Update answer choices if provided
                if (request.AnswerChoices?.Any() == true)
                {
                    // Remove old choices
                    _context.AnswerChoices.RemoveRange(question.AnswerChoices);

                    // Add new choices
                    foreach (var choice in request.AnswerChoices)
                    {
                        _context.AnswerChoices.Add(new AnswerChoice
                        {
                            ChoiceId = Guid.NewGuid().ToString(),
                            QuestionId = id,
                            ChoiceLabel = choice.ChoiceLabel,
                            ChoiceText = choice.ChoiceText,
                            IsCorrect = choice.IsCorrect,
                            DisplayOrder = choice.DisplayOrder
                        });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<QuestionDto>
                {
                    Success = true,
                    Message = "Cập nhật câu hỏi thành công",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId}", id);
                return StatusCode(500, new ApiResponse<QuestionDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Delete question (soft delete)
        /// DELETE: ai-question/{id}
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteQuestion(string id)
        {
            try
            {
                var question = await _context.Questions
                    .FirstOrDefaultAsync(q => q.QuestionId == id && q.IsActive);

                if (question == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Câu hỏi không tồn tại"
                    });
                }

                question.IsActive = false;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Xóa câu hỏi thành công",
                    Data = null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId}", id);
                return StatusCode(500, new ApiResponse<object>
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
                _logger.LogInformation("Getting chapters from database...");
                
                // First check total count using Chapters DbSet
                var totalCount = await _context.Chapters.CountAsync();
                _logger.LogInformation($"Total chapters in database: {totalCount}");
                
                // Check active count
                var activeCount = await _context.Chapters.Where(c => c.IsActive).CountAsync();
                _logger.LogInformation($"Active chapters: {activeCount}");
                
                // Debug: Log some sample chapters
                var sampleChapters = await _context.Chapters
                    .Take(3)
                    .Select(c => new { c.ChapterId, c.ChapterName, c.IsActive, c.Grade })
                    .ToListAsync();
                
                _logger.LogInformation($"Sample chapters: {string.Join(", ", sampleChapters.Select(c => $"ID={c.ChapterId},Name={c.ChapterName},Active={c.IsActive},Grade={c.Grade}"))}");
                
                var chapters = await _context.Chapters
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Grade)
                    .ThenBy(c => c.DisplayOrder)
                    .ToListAsync();

                _logger.LogInformation($"Retrieved {chapters.Count} chapters successfully");

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

        [HttpGet("debug/database-status")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<object>>> GetDatabaseStatus()
        {
            try
            {
                _logger.LogInformation("Checking database status...");

                var dbStatus = new
                {
                    CanConnect = await _context.Database.CanConnectAsync(),
                    
                    Tables = new
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        ActiveUsers = await _context.Users.Where(u => u.IsActive).CountAsync(),
                        
                        TotalChapters = await _context.Chapters.CountAsync(),
                        ActiveChapters = await _context.Chapters.Where(c => c.IsActive).CountAsync(),
                        
                        TotalQuestions = await _context.Questions.CountAsync(),
                        ActiveQuestions = await _context.Questions.Where(q => q.IsActive).CountAsync(),
                        
                        TotalExams = await _context.Exams.CountAsync(),
                        
                        TotalPhysicsTopics = await _context.PhysicsTopics.CountAsync(),
                        ActivePhysicsTopics = await _context.PhysicsTopics.Where(t => t.IsActive).CountAsync()
                    },
                    
                    SampleChapters = await _context.Chapters
                        .Where(c => c.IsActive)
                        .Take(10)
                        .Select(c => new { 
                            c.ChapterId, 
                            c.ChapterName, 
                            c.Grade, 
                            c.DisplayOrder,
                            c.IsActive,
                            c.CreatedAt
                        })
                        .ToListAsync(),
                    
                    SampleUsers = await _context.Users
                        .Where(u => u.IsActive)
                        .Take(5)
                        .Select(u => new { 
                            u.UserId, 
                            u.Username, 
                            u.Role, 
                            u.IsActive 
                        })
                        .ToListAsync(),
                    
                    SamplePhysicsTopics = await _context.PhysicsTopics
                        .Where(t => t.IsActive)
                        .Take(5)
                        .Select(t => new { 
                            t.TopicId, 
                            t.TopicName, 
                            t.GradeLevel,
                            t.IsActive 
                        })
                        .ToListAsync()
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Database status retrieved successfully",
                    Data = dbStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database status");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Lỗi kiểm tra database: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// Generate explanation for a question using AI
        /// POST: ai-question/generate-explanation
        /// </summary>
        [HttpPost("generate-explanation")]
        public async Task<IActionResult> GenerateExplanation([FromBody] GenerateExplanationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.QuestionId))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId là bắt buộc" });
                }

                // Lấy thông tin câu hỏi
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId);

                if (question == null)
                {
                    return NotFound(new { error = "not_found", message = "Không tìm thấy câu hỏi" });
                }

                // Lấy answer choices
                var answerChoices = await _context.AnswerChoices
                    .Where(ac => ac.QuestionId == request.QuestionId)
                    .OrderBy(ac => (int?)ac.DisplayOrder ?? 1)
                    .ThenBy(ac => ac.ChoiceLabel)
                    .ToListAsync();

                // Tìm đáp án đúng
                var correctChoice = answerChoices.FirstOrDefault(ac => ac.IsCorrect);
                var correctAnswer = correctChoice != null 
                    ? $"{correctChoice.ChoiceLabel}. {correctChoice.ChoiceText}"
                    : "Không xác định được đáp án đúng";

                // Tạo explanation bằng AI service
                var explanation = await _aiService.GenerateExplanationAsync(question, correctAnswer);

                if (string.IsNullOrEmpty(explanation))
                {
                    return StatusCode(500, new { error = "ai_error", message = "Không thể tạo giải thích bằng AI" });
                }

                return Ok(new
                {
                    questionId = request.QuestionId,
                    explanation = explanation,
                    generatedAt = DateTime.UtcNow,
                    message = "Tạo giải thích thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo explanation cho câu hỏi {QuestionId}", request.QuestionId);
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi tạo giải thích" });
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

        #region Helper Methods for ExamMatrix

        private bool IsValidExamType(string examType)
        {
            var validTypes = GetValidExamTypes();
            return validTypes.Contains(examType.ToLower());
        }

        private string[] GetValidExamTypes()
        {
            return new[] { "15p", "1tiet", "cuoiky", "giua_ki", "kiem_tra" };
        }

        private string GenerateMatrixId(string examType, int grade)
        {
            var prefix = examType.ToUpper();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            return $"MATRIX_{prefix}_G{grade}_{timestamp}";
        }

        private async Task<List<QuestionDto>> GetExistingQuestionsForChapter(
            int chapterId, 
            string difficultyLevel, 
            int count,
            List<string>? preferredTypes = null)
        {
            try
            {
                var query = _context.Questions
                    .Include(q => q.AnswerChoices)
                    .Include(q => q.Topic)
                    .Where(q => q.ChapterId == chapterId && 
                               q.DifficultyLevel == difficultyLevel && 
                               q.IsActive);

                if (preferredTypes?.Any() == true)
                {
                    query = query.Where(q => preferredTypes.Contains(q.QuestionType));
                }

                var questions = await query
                    .Take(count)
                    .ToListAsync();

                return questions.Select(q => new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Difficulty = q.DifficultyLevel,
                    DifficultyLevel = q.DifficultyLevel,
                    Topic = q.Topic?.TopicName ?? "Chưa phân loại",
                    ImageUrl = q.ImageUrl ?? "",
                    CreatedBy = q.CreatedBy,
                    CreatedAt = q.CreatedAt,
                    AnswerChoices = q.AnswerChoices?.Select(ac => new AnswerChoiceDto
                    {
                        ChoiceId = ac.ChoiceId,
                        ChoiceLabel = ac.ChoiceLabel ?? "A",
                        ChoiceText = ac.ChoiceText,
                        IsCorrect = ac.IsCorrect,
                        DisplayOrder = ac.DisplayOrder
                    }).ToList() ?? new List<AnswerChoiceDto>()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting existing questions for chapter {chapterId}");
                return new List<QuestionDto>();
            }
        }

        private decimal CalculatePointsWeight(string difficultyLevel)
        {
            return difficultyLevel.ToLower() switch
            {
                "easy" => 0.5m,
                "medium" => 1.0m,
                "hard" => 1.5m,
                _ => 1.0m
            };
        }

        private string GetRandomQuestionType(List<string>? preferredTypes = null)
        {
            var availableTypes = preferredTypes?.Any() == true 
                ? preferredTypes 
                : new List<string> { "multiple_choice", "true_false", "essay" };

            var random = new Random();
            return availableTypes[random.Next(availableTypes.Count)];
        }

        private List<ExamQuestionDto> ShuffleQuestions(List<ExamQuestionDto> questions)
        {
            var random = new Random();
            var shuffled = questions.OrderBy(x => random.Next()).ToList();
            
            // Update question order after shuffling
            for (int i = 0; i < shuffled.Count; i++)
            {
                shuffled[i].QuestionOrder = i + 1;
            }
            
            return shuffled;
        }

        private int GetDefaultDuration(string? examName)
        {
            if (string.IsNullOrEmpty(examName))
                return 45;

            var name = examName.ToLower();
            if (name.Contains("15p")) return 15;
            if (name.Contains("1tiet")) return 45;
            if (name.Contains("cuoiky") || name.Contains("cuối kì")) return 90;
            if (name.Contains("giua_ki") || name.Contains("giữa kì")) return 60;
            
            return 45; // Default
        }

        private string ExtractExamTypeFromMatrix(ExamMatrix matrix)
        {
            var name = matrix.ExamName?.ToLower() ?? "";
            if (name.Contains("15p")) return "15p";
            if (name.Contains("1tiet") || name.Contains("1 tiết")) return "1tiet";
            if (name.Contains("cuoiky") || name.Contains("cuối kì")) return "cuoiky";
            if (name.Contains("giua_ki") || name.Contains("giữa kì")) return "giua_ki";
            
            return "1tiet"; // Default
        }

        private decimal CalculateTotalPoints(ExamMatrix matrix)
        {
            var easyPoints = matrix.NumEasy * 0.5m;
            var mediumPoints = matrix.NumMedium * 1.0m;
            var hardPoints = matrix.NumHard * 1.5m;
            
            return easyPoints + mediumPoints + hardPoints;
        }

        #endregion

        #region Helper Methods for Enhanced Questions

        private string BuildAdvancedInstructions(EnhancedGenerateRequest request, List<Chapter> chapters)
        {
            var instructions = new List<string>();
            
            if (request.IncludeLearningObjectives)
            {
                instructions.Add("Bao gồm mục tiêu học tập rõ ràng");
            }
            
            if (request.IncludeRelatedConcepts)
            {
                instructions.Add("Liệt kê các khái niệm liên quan");
            }
            
            if (chapters.Count > 1)
            {
                var chapterNames = string.Join(", ", chapters.Select(c => c.ChapterName));
                instructions.Add($"Kết hợp kiến thức từ các chương: {chapterNames}");
            }
            
            if (request.Tags?.Any() == true)
            {
                instructions.Add($"Sử dụng các tag: {string.Join(", ", request.Tags)}");
            }
            
            if (!string.IsNullOrEmpty(request.AdditionalInstructions))
            {
                instructions.Add(request.AdditionalInstructions);
            }
            
            return string.Join(". ", instructions);
        }

        private async Task<EnhancedQuestionDto> EnhanceQuestion(QuestionDto baseQuestion, EnhancedGenerateRequest request, List<Chapter> chapters)
        {
            var enhanced = new EnhancedQuestionDto
            {
                QuestionId = baseQuestion.QuestionId,
                Topic = baseQuestion.Topic,
                QuestionText = baseQuestion.QuestionText,
                QuestionType = baseQuestion.QuestionType,
                Difficulty = baseQuestion.Difficulty,
                DifficultyLevel = baseQuestion.DifficultyLevel,
                ImageUrl = baseQuestion.ImageUrl,
                CreatedBy = baseQuestion.CreatedBy,
                CreatedAt = baseQuestion.CreatedAt,
                AnswerChoices = baseQuestion.AnswerChoices,
                Explanation = baseQuestion.Explanation,
                Tags = request.Tags ?? new List<string>(),
                LearningObjectives = GenerateLearningObjectives(baseQuestion, chapters),
                RelatedConcepts = GenerateRelatedConcepts(baseQuestion, chapters),
                Metadata = new QuestionMetadata
                {
                    LastModified = DateTime.UtcNow,
                    ModifiedBy = "ai_system",
                    UsageCount = 0,
                    AverageScore = 0,
                    DifficultyRating = GetDifficultyRating(baseQuestion.DifficultyLevel),
                    FeedbackComments = new List<string>()
                },
                AIInfo = new AIGenerationInfo
                {
                    Provider = "Enhanced AI",
                    Model = "enhanced-model",
                    GeneratedAt = DateTime.UtcNow,
                    Prompt = request.AdditionalInstructions ?? "",
                    ConfidenceScore = 0.85,
                    WasImproved = true,
                    GenerationSteps = new List<string> { "Base generation", "Enhancement", "Validation" }
                }
            };

            return enhanced;
        }

        private List<string> GenerateLearningObjectives(QuestionDto question, List<Chapter> chapters)
        {
            var objectives = new List<string>();
            
            foreach (var chapter in chapters)
            {
                objectives.Add($"Áp dụng kiến thức {chapter.ChapterName}");
                objectives.Add($"Phân tích và giải quyết vấn đề liên quan đến {chapter.ChapterName}");
            }
            
            return objectives.Take(3).ToList(); // Limit to 3 objectives
        }

        private List<string> GenerateRelatedConcepts(QuestionDto question, List<Chapter> chapters)
        {
            var concepts = new List<string>();
            
            foreach (var chapter in chapters)
            {
                concepts.Add($"Khái niệm cơ bản - {chapter.ChapterName}");
                concepts.Add($"Ứng dụng thực tế - {chapter.ChapterName}");
            }
            
            return concepts.Take(5).ToList(); // Limit to 5 concepts
        }

        private double GetDifficultyRating(string difficultyLevel)
        {
            return difficultyLevel.ToLower() switch
            {
                "easy" => 0.3,
                "medium" => 0.6,
                "hard" => 0.9,
                _ => 0.5
            };
        }

        private async Task SaveEnhancedQuestionToDatabase(EnhancedQuestionDto enhanced)
        {
            // Convert to regular QuestionDto for saving
            var questionDto = new QuestionDto
            {
                QuestionId = enhanced.QuestionId,
                Topic = enhanced.Topic,
                QuestionText = enhanced.QuestionText,
                QuestionType = enhanced.QuestionType,
                Difficulty = enhanced.Difficulty,
                DifficultyLevel = enhanced.DifficultyLevel,
                Explanation = enhanced.Explanation,
                ImageUrl = enhanced.ImageUrl,
                CreatedBy = enhanced.CreatedBy,
                CreatedAt = enhanced.CreatedAt,
                AnswerChoices = enhanced.AnswerChoices
            };

            await SaveQuestionToDatabase(questionDto);
        }

        private ExamTemplateDto? GetExamTemplate(string templateName)
        {
            // Predefined templates - could be moved to database later
            var templates = new Dictionary<string, ExamTemplateDto>
            {
                ["15p_lop10"] = new ExamTemplateDto
                {
                    TemplateName = "Kiểm tra 15 phút - Lớp 10",
                    ExamType = "15p",
                    Grade = 10,
                    Duration = 15,
                    TotalQuestions = 5,
                    TotalPoints = 5,
                    ChapterDetails = new[]
                    {
                        new ChapterDetailDto { ChapterId = 1, QuestionCount = 3, DifficultyLevel = "easy" },
                        new ChapterDetailDto { ChapterId = 2, QuestionCount = 2, DifficultyLevel = "medium" }
                    }
                },
                ["1tiet_lop10"] = new ExamTemplateDto
                {
                    TemplateName = "Kiểm tra 1 tiết - Lớp 10",
                    ExamType = "1tiet",
                    Grade = 10,
                    Duration = 45,
                    TotalQuestions = 15,
                    TotalPoints = 10,
                    ChapterDetails = new[]
                    {
                        new ChapterDetailDto { ChapterId = 1, QuestionCount = 5, DifficultyLevel = "easy" },
                        new ChapterDetailDto { ChapterId = 2, QuestionCount = 7, DifficultyLevel = "medium" },
                        new ChapterDetailDto { ChapterId = 3, QuestionCount = 3, DifficultyLevel = "hard" }
                    }
                }
            };

            return templates.TryGetValue(templateName.ToLower(), out var template) ? template : null;
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