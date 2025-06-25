using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using BE_Phygens.Services;

namespace BE_Phygens.Controllers
{
    [Route("smart-exam")]
    [ApiController]
    // [Authorize] // Temporarily removed for testing
    public class SmartExamController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly ILogger<SmartExamController> _logger;
        private readonly IAIService _aiService;

        public SmartExamController(PhygensContext context, ILogger<SmartExamController> logger, IAIService aiService)
        {
            _context = context;
            _logger = logger;
            _aiService = aiService;
        }

        [HttpPost("create-matrix")]
        public async Task<ActionResult<ApiResponse<ExamMatrix>>> CreateExamMatrix([FromBody] CreateExamMatrixRequest request)
        {
            try
            {
                var matrixId = Guid.NewGuid().ToString();
                var examMatrix = new ExamMatrix
                {
                    MatrixId = matrixId,
                    ExamName = request.ExamName,
                    Subject = request.ExamType ?? "Physics",
                    Topic = request.ExamName ?? "General",
                    NumEasy = request.ChapterDetails.Where(c => c.DifficultyLevel == "easy").Sum(c => c.QuestionCount),
                    NumMedium = request.ChapterDetails.Where(c => c.DifficultyLevel == "medium").Sum(c => c.QuestionCount),
                    NumHard = request.ChapterDetails.Where(c => c.DifficultyLevel == "hard").Sum(c => c.QuestionCount),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<ExamMatrix>().Add(examMatrix);

                // Kiểm tra chapters trước khi tạo với debugging tốt hơn
                var requestedChapterIds = request.ChapterDetails.Select(c => c.ChapterId).ToList();
                _logger.LogInformation($"Checking chapters with IDs: {string.Join(", ", requestedChapterIds)}");

                // Check total chapters in database first
                var totalChapters = await _context.Chapters.CountAsync();
                var activeChapters = await _context.Chapters.Where(c => c.IsActive).CountAsync();
                _logger.LogInformation($"Database contains {totalChapters} total chapters, {activeChapters} active");

                // Get a few sample chapters for debugging
                var sampleChapters = await _context.Chapters
                    .Take(5)
                    .Select(c => new { c.ChapterId, c.ChapterName, c.IsActive, c.Grade })
                    .ToListAsync();
                _logger.LogInformation($"Sample chapters: {string.Join(", ", sampleChapters.Select(c => $"ID={c.ChapterId}({c.ChapterName})"))}");

                var existingChapters = await _context.Chapters
                    .Where(c => requestedChapterIds.Contains(c.ChapterId))
                    .ToListAsync();

                _logger.LogInformation($"Found {existingChapters.Count} chapters matching requested IDs");

                if (!existingChapters.Any())
                {
                    // Show all available chapters for help
                    var allChapters = await _context.Chapters
                        .Where(c => c.IsActive)
                        .Select(c => new { c.ChapterId, c.ChapterName, c.Grade })
                        .ToListAsync();

                    var availableList = allChapters.Any()
                        ? string.Join(", ", allChapters.Select(c => $"ID={c.ChapterId}({c.ChapterName}, Lớp {c.Grade})"))
                        : "Không có chapters nào trong database";

                    return BadRequest(new ApiResponse<ExamMatrix>
                    {
                        Success = false,
                        Message = $"Không tìm thấy chapters với IDs: {string.Join(", ", requestedChapterIds)}. Chapters có sẵn: {availableList}"
                    });
                }

                foreach (var detail in request.ChapterDetails)
                {
                    // Chỉ tạo cho chapters tồn tại
                    if (existingChapters.Any(c => c.ChapterId == detail.ChapterId))
                    {
                        var matrixDetail = new ExamMatrixDetail
                        {
                            ExamMatrixId = matrixId,
                            ChapterId = detail.ChapterId,
                            QuestionCount = detail.QuestionCount,
                            DifficultyLevel = detail.DifficultyLevel
                        };
                        _context.Set<ExamMatrixDetail>().Add(matrixDetail);
                    }
                    else
                    {
                        _logger.LogWarning($"Skipping chapter {detail.ChapterId} - not found in database");
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<ExamMatrix>
                {
                    Success = true,
                    Message = "Tạo ma trận đề thi thành công",
                    Data = examMatrix
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam matrix");
                return StatusCode(500, new ApiResponse<ExamMatrix>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpPost("generate-exam/{matrixId}")]
        public async Task<ActionResult<ApiResponse<GeneratedExamDto>>> GenerateExam(string matrixId)
        {
            try
            {
                // Get exam matrix with details
                var examMatrix = await _context.Set<ExamMatrix>()
                    .Include(em => em.ExamMatrixDetails)
                    .ThenInclude(emd => emd.Chapter)
                    .FirstOrDefaultAsync(em => em.MatrixId == matrixId);

                if (examMatrix == null)
                    return NotFound(new ApiResponse<GeneratedExamDto>
                    {
                        Success = false,
                        Message = "Ma trận đề thi không tồn tại"
                    });

                // ✅ SIMPLIFIED FIX: Use existing questions from database to avoid all tracking issues
                var validQuestions = new List<QuestionDto>();
                
                foreach (var detail in examMatrix.ExamMatrixDetails)
                {
                    try
                    {
                        _logger.LogInformation($"Tìm {detail.QuestionCount} câu hỏi có sẵn cho chapter {detail.ChapterId}");

                        // Get existing questions from database for this chapter
                        var existingQuestions = await _context.Questions
                            .AsNoTracking() // Important: use AsNoTracking
                            .Where(q => q.ChapterId == detail.ChapterId && q.IsActive)
                            .Take(detail.QuestionCount)
                            .Select(q => new QuestionDto
                            {
                                QuestionId = q.QuestionId,
                                Topic = detail.ChapterId.ToString(),
                                QuestionText = q.QuestionText,
                                QuestionType = q.QuestionType,
                                Difficulty = q.DifficultyLevel,
                                ChapterId = q.ChapterId ?? 0,
                                CreatedBy = q.CreatedBy,
                                CreatedAt = q.CreatedAt
                            })
                            .ToListAsync();

                        if (existingQuestions.Any())
                        {
                            validQuestions.AddRange(existingQuestions);
                            _logger.LogInformation($"✅ Tìm thấy {existingQuestions.Count} câu hỏi có sẵn cho chapter {detail.ChapterId}");
                        }
                        else
                        {
                            // Create minimal mock questions without saving to DB
                            for (int i = 0; i < detail.QuestionCount; i++)
                            {
                                var mockQuestion = new QuestionDto
                                {
                                    QuestionId = $"MOCK_{detail.ChapterId}_{i + 1}_{Guid.NewGuid().ToString()[..8]}",
                                    Topic = $"Chapter {detail.ChapterId}",
                                    QuestionText = $"Mock question {i + 1} for chapter {detail.ChapterId}",
                                    QuestionType = "multiple_choice",
                                    Difficulty = detail.DifficultyLevel,
                                    ChapterId = detail.ChapterId,
                                    CreatedBy = "mock_system",
                                    CreatedAt = DateTime.UtcNow
                                };
                                
                                // First, insert this question to database with raw SQL
                                var sql = @"
                                    INSERT INTO question (questionid, questiontext, questiontype, difficultylevel, createdby, createdat, topicid, chapterid, isactive, aigenerated)
                                    VALUES (@questionId, @questionText, @questionType, @difficultyLevel, @createdBy, @createdAt, @topicId, @chapterId, @isActive, @aiGenerated)
                                    ON CONFLICT (questionid) DO NOTHING";
                                
                                await _context.Database.ExecuteSqlRawAsync(sql,
                                    new Npgsql.NpgsqlParameter("@questionId", mockQuestion.QuestionId),
                                    new Npgsql.NpgsqlParameter("@questionText", mockQuestion.QuestionText),
                                    new Npgsql.NpgsqlParameter("@questionType", "multiple_choice"),
                                    new Npgsql.NpgsqlParameter("@difficultyLevel", detail.DifficultyLevel),
                                    new Npgsql.NpgsqlParameter("@createdBy", "mock_system"),
                                    new Npgsql.NpgsqlParameter("@createdAt", DateTime.UtcNow),
                                    new Npgsql.NpgsqlParameter("@topicId", "TOPIC_001"),
                                    new Npgsql.NpgsqlParameter("@chapterId", detail.ChapterId),
                                    new Npgsql.NpgsqlParameter("@isActive", true),
                                    new Npgsql.NpgsqlParameter("@aiGenerated", true)
                                );
                                
                                validQuestions.Add(mockQuestion);
                            }
                            _logger.LogInformation($"✅ Tạo {detail.QuestionCount} mock questions cho chapter {detail.ChapterId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi xử lý questions cho chapter {detail.ChapterId}: {ex.Message}");
                    }
                }

                _logger.LogInformation($"✅ Chuẩn bị {validQuestions.Count} questions để tạo exam");

                // ✅ FIX: Create exam using raw SQL to completely avoid EF tracking
                var examId = Guid.NewGuid().ToString();
                var defaultUser = "ai_system"; // Use hardcoded default to avoid GetOrCreateDefaultUser tracking
                
                var examSql = @"
                    INSERT INTO exam (examid, examname, description, durationminutes, examtype, createdby, ispublished, createdat)
                    VALUES (@examId, @examName, @description, @durationMinutes, @examType, @createdBy, @isPublished, @createdAt)";
                
                await _context.Database.ExecuteSqlRawAsync(examSql,
                    new Npgsql.NpgsqlParameter("@examId", examId),
                    new Npgsql.NpgsqlParameter("@examName", $"{examMatrix.ExamName ?? examMatrix.Subject} - {DateTime.Now:dd/MM/yyyy HH:mm}"),
                    new Npgsql.NpgsqlParameter("@description", $"Đề thi được tạo tự động từ ma trận: {examMatrix.ExamName ?? examMatrix.Subject}"),
                    new Npgsql.NpgsqlParameter("@durationMinutes", 45),
                    new Npgsql.NpgsqlParameter("@examType", "smart_exam"),
                    new Npgsql.NpgsqlParameter("@createdBy", defaultUser),
                    new Npgsql.NpgsqlParameter("@isPublished", false),
                    new Npgsql.NpgsqlParameter("@createdAt", DateTime.UtcNow)
                );
                
                _logger.LogInformation($"✅ Created exam using raw SQL: {examId}");
                var examQuestions = new List<ExamQuestionDto>();
                int questionOrder = 1;

                foreach (var question in validQuestions)
                {
                    var examQuestionId = Guid.NewGuid().ToString();
                    var pointsWeight = CalculateQuestionPoints("medium", 10.0m, validQuestions.Count);
                    
                    // ✅ FIX: Insert ExamQuestion using raw SQL to avoid tracking conflicts
                    var examQuestionSql = @"
                        INSERT INTO examquestion (examquestionid, examid, questionid, questionorder, pointsweight, addedat)
                        VALUES (@examQuestionId, @examId, @questionId, @questionOrder, @pointsWeight, @addedAt)";
                    
                    await _context.Database.ExecuteSqlRawAsync(examQuestionSql,
                        new Npgsql.NpgsqlParameter("@examQuestionId", examQuestionId),
                        new Npgsql.NpgsqlParameter("@examId", examId),
                        new Npgsql.NpgsqlParameter("@questionId", question.QuestionId),
                        new Npgsql.NpgsqlParameter("@questionOrder", questionOrder),
                        new Npgsql.NpgsqlParameter("@pointsWeight", pointsWeight),
                        new Npgsql.NpgsqlParameter("@addedAt", DateTime.UtcNow)
                    );

                    examQuestions.Add(new ExamQuestionDto
                    {
                        ExamQuestionId = examQuestionId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = questionOrder,
                        PointsWeight = pointsWeight,
                        Question = question
                    });
                    
                    questionOrder++;
                    _logger.LogInformation($"✅ Created ExamQuestion: {examQuestionId} for Question: {question.QuestionId}");
                }

                _logger.LogInformation($"✅ Created {examQuestions.Count} exam questions using raw SQL");

                var generatedExam = new GeneratedExamDto
                {
                    ExamId = examId,
                    ExamName = $"{examMatrix.ExamName ?? examMatrix.Subject} - {DateTime.Now:dd/MM/yyyy HH:mm}",
                    Description = $"Đề thi được tạo tự động từ ma trận: {examMatrix.ExamName ?? examMatrix.Subject}",
                    Duration = 45,
                    ExamType = "smart_exam",
                    TotalQuestions = examQuestions.Count,
                    TotalPoints = 10.0m, // Default total points
                    Questions = examQuestions
                };

                return Ok(new ApiResponse<GeneratedExamDto>
                {
                    Success = true,
                    Message = "Tạo đề thi thành công",
                    Data = generatedExam
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exam");
                return StatusCode(500, new ApiResponse<GeneratedExamDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("matrices")]
        public async Task<ActionResult<ApiResponse<List<ExamMatrixListDto>>>> GetExamMatrices([FromQuery] int? grade = null)
        {
            try
            {
                var query = _context.Set<ExamMatrix>().AsQueryable();

                // Since we don't have Grade in the actual DB schema, we'll use all matrices for now
                // if (grade.HasValue)
                //     query = query.Where(em => em.Grade == grade.Value);

                var matrices = await query
                    .OrderByDescending(em => em.CreatedAt)
                    .Select(em => new ExamMatrixListDto
                    {
                        MatrixId = em.MatrixId,
                        ExamName = em.ExamName ?? em.Subject, // Use Subject if ExamName is null
                        ExamType = em.Topic, // Use Topic as ExamType
                        Grade = 10, // Default grade since we don't have it in DB
                        Duration = 45, // Default duration
                        TotalQuestions = em.TotalQuestions,
                        TotalPoints = 10, // Default points
                        CreatedAt = em.CreatedAt,
                        CreatedBy = "system" // Default creator
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ExamMatrixListDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách ma trận thành công",
                    Data = matrices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam matrices");
                return StatusCode(500, new ApiResponse<List<ExamMatrixListDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("chapters")]
        public async Task<ActionResult<ApiResponse<List<ChapterDto>>>> GetChapters()
        {
            try
            {
                var chapters = await _context.Chapters
                    .Where(c => c.IsActive == true)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new ChapterDto
                    {
                        ChapterId = c.ChapterId,
                        ChapterName = c.ChapterName,
                        Grade = c.Grade,
                        Description = c.Description
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ChapterDto>>
                {
                    Success = true,
                    Message = $"Lấy thành công {chapters.Count} chapters",
                    Data = chapters
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters");
                return StatusCode(500, new ApiResponse<List<ChapterDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("questions/chapter/{chapterId}")]
        public async Task<ActionResult<ApiResponse<List<QuestionDto>>>> GetQuestionsForChapterEndpoint(
            int chapterId,
            [FromQuery] int count = 5,
            [FromQuery] string difficulty = "medium")
        {
            try
            {
                _logger.LogInformation($"Generating {count} {difficulty} questions for chapter {chapterId}");

                var questions = await GetQuestionsForChapter(chapterId, count, difficulty);

                return Ok(new ApiResponse<List<QuestionDto>>
                {
                    Success = true,
                    Message = $"Tạo thành công {questions.Count} câu hỏi cho chương {chapterId}",
                    Data = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating questions for chapter {chapterId}");
                return StatusCode(500, new ApiResponse<List<QuestionDto>>
                {
                    Success = false,
                    Message = $"Lỗi tạo câu hỏi: {ex.Message}"
                });
            }
        }

        [HttpGet("templates")]
        public ActionResult<ApiResponse<List<ExamTemplateDto>>> GetExamTemplates()
        {
            var templates = new List<ExamTemplateDto>
            {
                new()
                {
                    TemplateName = "Kiểm tra 15 phút - Cơ học",
                    ExamType = "15p",
                    Grade = 10,
                    Duration = 15,
                    TotalQuestions = 10,
                    TotalPoints = 10,
                    ChapterDetails = new[]
                    {
                        new ChapterDetailDto { ChapterId = 1, QuestionCount = 10, DifficultyLevel = "easy" }
                    }
                },
                new()
                {
                    TemplateName = "Kiểm tra 1 tiết - Cơ nhiệt",
                    ExamType = "1tiet",
                    Grade = 10,
                    Duration = 45,
                    TotalQuestions = 25,
                    TotalPoints = 10,
                    ChapterDetails = new[]
                    {
                        new ChapterDetailDto { ChapterId = 1, QuestionCount = 15, DifficultyLevel = "medium" },
                        new ChapterDetailDto { ChapterId = 2, QuestionCount = 10, DifficultyLevel = "easy" }
                    }
                },
                new()
                {
                    TemplateName = "Thi giữa kì - Vật lý 11",
                    ExamType = "giuaki",
                    Grade = 11,
                    Duration = 90,
                    TotalQuestions = 40,
                    TotalPoints = 10,
                    ChapterDetails = new[]
                    {
                        new ChapterDetailDto { ChapterId = 3, QuestionCount = 20, DifficultyLevel = "medium" },
                        new ChapterDetailDto { ChapterId = 4, QuestionCount = 15, DifficultyLevel = "medium" },
                        new ChapterDetailDto { ChapterId = 1, QuestionCount = 5, DifficultyLevel = "hard" }
                    }
                }
            };

            return Ok(new ApiResponse<List<ExamTemplateDto>>
            {
                Success = true,
                Message = "Lấy template thành công",
                Data = templates
            });
        }

        private async Task<List<QuestionDto>> GetQuestionsForChapter(int chapterId, int count, string difficulty)
        {
            var chapter = await _context.Set<Chapter>().FindAsync(chapterId);
            if (chapter == null)
            {
                _logger.LogWarning($"Chapter with ID {chapterId} not found");
                return new List<QuestionDto>();
            }

            _logger.LogInformation($"Generating {count} {difficulty} questions for chapter: {chapter.ChapterName}");

            var questions = new List<QuestionDto>();

            try
            {
                // Generate questions using AI Service
                for (int i = 0; i < count; i++)
                {
                    var request = new GenerateQuestionRequest
                    {
                        ChapterId = chapterId,
                        DifficultyLevel = difficulty,
                        QuestionType = "multiple_choice",
                        SpecificTopic = null,
                        AdditionalInstructions = $"Tạo câu hỏi cho chương: {chapter.ChapterName}, lớp {chapter.Grade}"
                    };

                    try
                    {
                        _logger.LogInformation($"Generating question {i + 1}/{count} using AI service...");
                        var aiQuestion = await _aiService.GenerateQuestionAsync(chapter, request);

                        if (aiQuestion != null)
                        {
                            _logger.LogInformation($"Successfully generated AI question {i + 1}: {aiQuestion.QuestionText?.Substring(0, Math.Min(50, aiQuestion.QuestionText?.Length ?? 0))}...");
                            questions.Add(aiQuestion);
                        }
                        else
                        {
                            _logger.LogWarning($"AI service returned null for question {i + 1}");
                        }
                    }
                    catch (Exception aiEx)
                    {
                        _logger.LogError(aiEx, $"Error generating AI question {i + 1}: {aiEx.Message}");

                        // Create a fallback question if AI fails
                        var fallbackQuestion = CreateFallbackQuestion(chapter, difficulty, i + 1);
                        questions.Add(fallbackQuestion);
                    }

                    // Small delay to avoid rate limiting
                    if (i < count - 1)
                    {
                        await Task.Delay(500);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in AI question generation process: {ex.Message}");

                // If AI fails completely, try to get from database as fallback
                _logger.LogInformation("Falling back to database questions...");
                return await GetQuestionsFromDatabaseFallback(chapterId, count, difficulty, chapter);
            }

            _logger.LogInformation($"Successfully generated {questions.Count} questions using AI service");
            return questions;
        }

        private QuestionDto CreateFallbackQuestion(Chapter chapter, string difficulty, int questionNumber)
        {
            var questionId = Guid.NewGuid().ToString();

            return new QuestionDto
            {
                QuestionId = questionId,
                Topic = chapter.ChapterName,
                QuestionText = $"Câu hỏi {questionNumber} về {chapter.ChapterName} (Lớp {chapter.Grade}) - Độ khó: {difficulty}",
                QuestionType = "multiple_choice",
                Difficulty = difficulty,
                ChapterId = chapter.ChapterId,
                CreatedBy = "AI_System_Fallback",
                CreatedAt = DateTime.UtcNow,
                AnswerChoices = new List<AnswerChoiceDto>
                {
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "A", ChoiceText = "Đáp án A", IsCorrect = true, DisplayOrder = 1 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "B", ChoiceText = "Đáp án B", IsCorrect = false, DisplayOrder = 2 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "C", ChoiceText = "Đáp án C", IsCorrect = false, DisplayOrder = 3 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "D", ChoiceText = "Đáp án D", IsCorrect = false, DisplayOrder = 4 }
                }
            };
        }

        private async Task<List<QuestionDto>> GetQuestionsFromDatabaseFallback(int chapterId, int count, string difficulty, Chapter chapter)
        {
            try
            {
                // Try to get questions from database as last resort
                var dbQuestions = await _context.Questions
                    .Where(q => q.ChapterId == chapterId)
                    .Include(q => q.AnswerChoices)
                    .OrderBy(x => Guid.NewGuid())
                    .Take(count)
                    .Select(q => new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        Topic = chapter.ChapterName,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Difficulty = q.DifficultyLevel,
                        ChapterId = q.ChapterId ?? 0,
                        CreatedBy = q.CreatedBy,
                        CreatedAt = q.CreatedAt,
                        AnswerChoices = q.AnswerChoices.Select(ac => new AnswerChoiceDto
                        {
                            ChoiceId = ac.ChoiceId,
                            ChoiceLabel = ac.ChoiceLabel,
                            ChoiceText = ac.ChoiceText,
                            IsCorrect = ac.IsCorrect,
                            DisplayOrder = ac.DisplayOrder ?? 0
                        }).ToList()
                    })
                    .ToListAsync();

                if (dbQuestions.Count > 0)
                {
                    _logger.LogInformation($"Found {dbQuestions.Count} questions from database");
                    return dbQuestions;
                }

                // If no database questions either, create fallback questions
                _logger.LogWarning("No questions found in database, creating fallback questions");
                var fallbackQuestions = new List<QuestionDto>();
                for (int i = 0; i < count; i++)
                {
                    fallbackQuestions.Add(CreateFallbackQuestion(chapter, difficulty, i + 1));
                }
                return fallbackQuestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in database fallback");

                // Last resort: create mock questions
                var mockQuestions = new List<QuestionDto>();
                for (int i = 0; i < count; i++)
                {
                    mockQuestions.Add(CreateFallbackQuestion(chapter, difficulty, i + 1));
                }
                return mockQuestions;
            }
        }

        private decimal CalculateQuestionPoints(string difficulty, decimal totalPoints, int totalQuestions)
        {
            var basePoints = totalPoints / totalQuestions;
            return difficulty.ToLower() switch
            {
                "easy" => basePoints * 0.8m,
                "medium" => basePoints * 1.0m,
                "hard" => basePoints * 1.2m,
                _ => basePoints
            };
        }

        private string GetOrCreateDefaultUser()
        {
            try
            {
                // Try to get ai_system user first - FIX: Role can be admin/teacher/student according to SQL schema
                var aiSystemUser = _context.Users.FirstOrDefault(u => u.UserId == "ai_system" && (u.Role == "admin"));
                if (aiSystemUser != null)
                {
                    return aiSystemUser.UserId;
                }

                // If no ai_system, get any admin/teacher user
                var adminUser = _context.Users.FirstOrDefault(u => u.Role == "admin");
                if (adminUser != null)
                {
                    return adminUser.UserId;
                }

                // If no admin/teacher user found, get first user
                var anyUser = _context.Users.FirstOrDefault();
                if (anyUser != null)
                {
                    return anyUser.UserId;
                }

                // If no user found, return default
                _logger.LogWarning("No user found for smart exam creation, using default");
                return "ai_system";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default user for smart exam");
                return "ai_system";
            }
        }
    }
}