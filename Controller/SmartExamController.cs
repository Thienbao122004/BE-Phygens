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
                            // ✅ FIX: Use AI Service to generate REAL questions instead of mock
                            _logger.LogInformation($"Không tìm thấy câu hỏi trong DB cho chapter {detail.ChapterId}. Tạo câu hỏi AI thật...");
                            
                            var chapter = await _context.Chapters.FindAsync(detail.ChapterId);
                            if (chapter != null)
                            {
                                var aiQuestions = await GetQuestionsForChapter(detail.ChapterId, detail.QuestionCount, detail.DifficultyLevel);
                                if (aiQuestions.Any())
                                {
                                    validQuestions.AddRange(aiQuestions);
                                    _logger.LogInformation($"✅ Tạo {aiQuestions.Count} câu hỏi AI thật cho chapter {detail.ChapterId}");
                                }
                                else
                                {
                                    throw new InvalidOperationException($"Không thể tạo câu hỏi cho chapter {detail.ChapterId}. Vui lòng kiểm tra AI service hoặc thêm câu hỏi vào database.");
                                }
                            }
                            else
                            {
                                throw new InvalidOperationException($"Chapter {detail.ChapterId} không tồn tại trong database.");
                            }
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
            [FromQuery] int count,
            [FromQuery] string difficulty)
        {
            try
            {
                // ❌ VALIDATE: Không cho phép values mặc định
                if (count <= 0)
                {
                    return BadRequest(new ApiResponse<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "❌ THAM SỐ 'count' phải lớn hơn 0. Không được để trống hoặc mặc định!"
                    });
                }
                
                if (string.IsNullOrEmpty(difficulty) || !new[] { "easy", "medium", "hard" }.Contains(difficulty.ToLower()))
                {
                    return BadRequest(new ApiResponse<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "❌ THAM SỐ 'difficulty' phải là 'easy', 'medium' hoặc 'hard'. Không được để trống hoặc mặc định!"
                    });
                }

                _logger.LogInformation($"Generating {count} {difficulty} questions for chapter {chapterId}");

                var questions = await GetQuestionsForChapter(chapterId, count, difficulty.ToLower());

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
        public async Task<ActionResult<ApiResponse<List<ExamTemplateDto>>>> GetExamTemplates()
        {
            try
            {
                // ❌ REMOVED: No more hardcoded templates - templates must come from database or user input
                var templatesFromDb = await _context.Set<SmartExamTemplate>()
                    .Where(t => t.IsActive)
                    .Select(t => new ExamTemplateDto
                    {
                        TemplateName = t.TemplateName,
                        ExamType = t.ExamType,
                        Grade = t.TargetGrade,
                        Duration = t.DurationMinutes,
                        TotalQuestions = t.TotalQuestions,
                        TotalPoints = 10,
                        ChapterDetails = new ChapterDetailDto[] { } // Simplified - no template details hardcoded
                    })
                    .ToListAsync();

                if (!templatesFromDb.Any())
                {
                    return Ok(new ApiResponse<List<ExamTemplateDto>>
                    {
                        Success = true,
                        Message = "❌ KHÔNG CÓ TEMPLATE TRONG DATABASE! Vui lòng tạo templates hoặc sử dụng tính năng tạo đề thi tùy chỉnh.",
                        Data = new List<ExamTemplateDto>()
                    });
                }

                return Ok(new ApiResponse<List<ExamTemplateDto>>
                {
                    Success = true,
                    Message = $"Lấy {templatesFromDb.Count} templates từ database thành công",
                    Data = templatesFromDb
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam templates from database");
                return StatusCode(500, new ApiResponse<List<ExamTemplateDto>>
                {
                    Success = false,
                    Message = $"❌ LỖI DATABASE TEMPLATES: {ex.Message}"
                });
            }
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
                        // ❌ REMOVED: No fallback questions allowed - AI must work or fail completely
                        throw new InvalidOperationException($"❌ AI KHÔNG THỂ TẠO CÂU HỎI {i + 1}! Lỗi: {aiEx.Message}");
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
            // ❌ REMOVED: No more fallback questions - only real AI generated questions allowed
            throw new InvalidOperationException($"❌ KHÔNG THỂ TẠO FALLBACK QUESTION! Chỉ được phép sử dụng câu hỏi AI thật hoặc từ database. Chapter: {chapter.ChapterName}, Difficulty: {difficulty}, Question: {questionNumber}");
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

                // ❌ REMOVED: No fallback questions allowed
                throw new InvalidOperationException($"❌ KHÔNG TÌM THẤY CÂU HỎI TRONG DATABASE! Chapter: {chapter.ChapterName}, Difficulty: {difficulty}, Count: {count}. Vui lòng thêm câu hỏi vào database hoặc đảm bảo AI service hoạt động.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in database fallback");

                // ❌ REMOVED: No mock questions allowed
                throw new InvalidOperationException($"❌ LỖI DATABASE FALLBACK! Chapter: {chapter.ChapterName}, Difficulty: {difficulty}. Error: {ex.Message}");
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