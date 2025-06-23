using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;

namespace BE_Phygens.Controllers
{
    [Route("smart-exam")]
    [ApiController]
    // [Authorize] // Temporarily removed for testing
    public class SmartExamController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly ILogger<SmartExamController> _logger;

        public SmartExamController(PhygensContext context, ILogger<SmartExamController> logger)
        {
            _context = context;
            _logger = logger;
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
                    Subject = request.ExamType ?? "Physics", // Map ExamType to Subject
                    Topic = request.ExamName ?? "General", // Use ExamName as Topic
                    NumEasy = request.ChapterDetails.Where(c => c.DifficultyLevel == "easy").Sum(c => c.QuestionCount),
                    NumMedium = request.ChapterDetails.Where(c => c.DifficultyLevel == "medium").Sum(c => c.QuestionCount),
                    NumHard = request.ChapterDetails.Where(c => c.DifficultyLevel == "hard").Sum(c => c.QuestionCount),
                    TotalQuestions = request.ChapterDetails.Sum(c => c.QuestionCount),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Set<ExamMatrix>().Add(examMatrix);

                // Add ExamMatrixDetails - FIX: Use correct property names from SQL schema
                foreach (var detail in request.ChapterDetails)
                {
                    var matrixDetail = new ExamMatrixDetail
                    {
                        ExamMatrixId = matrixId,  // exammatrixid in SQL
                        ChapterId = detail.ChapterId,     // chapterid in SQL
                        QuestionCount = detail.QuestionCount, // questioncount in SQL
                        DifficultyLevel = detail.DifficultyLevel // difficultylevel in SQL
                    };
                    _context.Set<ExamMatrixDetail>().Add(matrixDetail);
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

                // First, generate all questions and save them - SEPARATE TRANSACTION
                var allQuestions = new List<QuestionDto>();
                foreach (var detail in examMatrix.ExamMatrixDetails)
                {
                    var questions = await GetQuestionsForChapter(detail.ChapterId, detail.QuestionCount, detail.DifficultyLevel);
                    allQuestions.AddRange(questions);
                }

                // Log warning if some questions were not found but continue with available questions
                var questionIds = allQuestions.Select(q => q.QuestionId).ToList();
                var existingQuestionIds = await _context.Questions
                    .Where(q => questionIds.Contains(q.QuestionId))
                    .Select(q => q.QuestionId)
                    .ToListAsync();

                var missingQuestionIds = questionIds.Except(existingQuestionIds).ToList();
                if (missingQuestionIds.Any())
                {
                    _logger.LogWarning($"Không tìm thấy {missingQuestionIds.Count} câu hỏi trong database, sử dụng câu hỏi có sẵn");
                }

                // Generate exam
                var examId = Guid.NewGuid().ToString();
                var exam = new Exam
                {
                    ExamId = examId,
                    ExamName = $"{examMatrix.ExamName ?? examMatrix.Subject} - {DateTime.Now:dd/MM/yyyy HH:mm}",
                    Description = $"Đề thi được tạo tự động từ ma trận: {examMatrix.ExamName ?? examMatrix.Subject}",
                    DurationMinutes = 45, // Default duration since not in ExamMatrix
                    ExamType = examMatrix.Topic, // Use Topic as ExamType
                    CreatedBy = GetOrCreateDefaultUser(),
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Exams.Add(exam);

                // Now create exam questions
                var examQuestions = new List<ExamQuestionDto>();
                int questionOrder = 1;

                foreach (var question in allQuestions)
                {
                    var examQuestionId = Guid.NewGuid().ToString();
                    var examQuestion = new ExamQuestion
                    {
                        ExamQuestionId = examQuestionId,
                        ExamId = examId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = questionOrder++,
                        PointsWeight = CalculateQuestionPoints("medium", 10.0m, allQuestions.Count),
                        AddedAt = DateTime.UtcNow
                    };

                    _context.ExamQuestions.Add(examQuestion);

                    examQuestions.Add(new ExamQuestionDto
                    {
                        ExamQuestionId = examQuestionId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = examQuestion.QuestionOrder ?? 1,
                        PointsWeight = examQuestion.PointsWeight,
                        Question = question
                    });
                }

                await _context.SaveChangesAsync();

                var generatedExam = new GeneratedExamDto
                {
                    ExamId = examId,
                    ExamName = exam.ExamName,
                    Description = exam.Description,
                    Duration = exam.DurationMinutes.GetValueOrDefault(45),
                    ExamType = exam.ExamType,
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
            if (chapter == null) return new List<QuestionDto>();

            // Get or create a default topic
            var defaultTopic = await _context.PhysicsTopics.FirstOrDefaultAsync(t => t.TopicName == "Auto Generated");
            if (defaultTopic == null)
            {
                defaultTopic = new PhysicsTopic
                {
                    TopicId = Guid.NewGuid().ToString(),
                    TopicName = "Auto Generated",
                    Description = "Questions generated automatically by AI system",
                    GradeLevel = "10-12",
                    DisplayOrder = 999,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.PhysicsTopics.Add(defaultTopic);
                await _context.SaveChangesAsync();
            }

            // First try to get questions with exact chapter and difficulty
            var exactMatchQuestions = await _context.Questions
                .Where(q => q.ChapterId == chapterId && q.DifficultyLevel.ToLower() == difficulty.ToLower())
                .Include(q => q.AnswerChoices)
                .OrderBy(x => Guid.NewGuid()) // Random order
                .Take(count)
                .Select(q => new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    Topic = chapter.ChapterName,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Difficulty = q.DifficultyLevel,
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

            var questions = new List<QuestionDto>();
            questions.AddRange(exactMatchQuestions);

            // If we still need more questions, try to get from same chapter with any difficulty
            if (questions.Count < count)
            {
                int remainingCount = count - questions.Count;
                var existingQuestionIds = questions.Select(q => q.QuestionId).ToList();
                
                var sameChapterQuestions = await _context.Questions
                    .Where(q => q.ChapterId == chapterId && !existingQuestionIds.Contains(q.QuestionId))
                    .Include(q => q.AnswerChoices)
                    .OrderBy(x => Guid.NewGuid()) // Random order
                    .Take(remainingCount)
                    .Select(q => new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        Topic = chapter.ChapterName,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Difficulty = q.DifficultyLevel,
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

                questions.AddRange(sameChapterQuestions);
            }

            // If we still need more questions, get from any chapter with similar difficulty
            if (questions.Count < count)
            {
                int remainingCount = count - questions.Count;
                var existingQuestionIds = questions.Select(q => q.QuestionId).ToList();
                
                var anyDifficultyQuestions = await _context.Questions
                    .Where(q => q.DifficultyLevel.ToLower() == difficulty.ToLower() && 
                               !existingQuestionIds.Contains(q.QuestionId))
                    .Include(q => q.AnswerChoices)
                    .OrderBy(x => Guid.NewGuid()) // Random order
                    .Take(remainingCount)
                    .ToListAsync();

                var anyDifficultyQuestionDtos = new List<QuestionDto>();
                foreach (var q in anyDifficultyQuestions)
                {
                    var questionChapter = await _context.Set<Chapter>().FindAsync(q.ChapterId);
                    anyDifficultyQuestionDtos.Add(new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        Topic = questionChapter?.ChapterName ?? "Unknown Chapter",
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Difficulty = q.DifficultyLevel,
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
                    });
                }

                questions.AddRange(anyDifficultyQuestionDtos);
            }

            // If we still don't have enough questions, get any available questions
            if (questions.Count < count)
            {
                int remainingCount = count - questions.Count;
                var existingQuestionIds = questions.Select(q => q.QuestionId).ToList();
                
                var anyQuestions = await _context.Questions
                    .Where(q => !existingQuestionIds.Contains(q.QuestionId))
                    .Include(q => q.AnswerChoices)
                    .OrderBy(x => Guid.NewGuid()) // Random order
                    .Take(remainingCount)
                    .ToListAsync();

                var anyQuestionDtos = new List<QuestionDto>();
                foreach (var q in anyQuestions)
                {
                    var questionChapter = await _context.Set<Chapter>().FindAsync(q.ChapterId);
                    anyQuestionDtos.Add(new QuestionDto
                    {
                        QuestionId = q.QuestionId,
                        Topic = questionChapter?.ChapterName ?? "Unknown Chapter",
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Difficulty = q.DifficultyLevel,
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
                    });
                }

                questions.AddRange(anyQuestionDtos);
            }

            // Only create mock questions if we absolutely have no questions in database
            if (questions.Count == 0)
            {
                var questionsToSave = new List<Question>();
                var answersToSave = new List<AnswerChoice>();

                for (int i = 0; i < count; i++)
                {
                    var questionId = Guid.NewGuid().ToString();
                    
                    // Create the Question entity to save to DB
                    var questionEntity = new Question
                    {
                        QuestionId = questionId,
                        TopicId = defaultTopic.TopicId,
                        ChapterId = chapterId,
                        QuestionText = $"Câu hỏi {i + 1} về {chapter.ChapterName} (độ khó: {difficulty})",
                        QuestionType = "multiple_choice",
                        DifficultyLevel = difficulty,
                        CreatedBy = "ai_system",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        AiGenerated = true,
                        AiProvider = "PhyGens_Auto",
                        AiModel = "auto_generator"
                    };
                    questionsToSave.Add(questionEntity);

                    // Create answer choices
                    var answerChoices = new List<AnswerChoiceDto>();
                    string[] labels = { "A", "B", "C", "D" };
                    for (int j = 0; j < 4; j++)
                    {
                        var choiceId = Guid.NewGuid().ToString();
                        
                        var answerChoice = new AnswerChoice
                        {
                            ChoiceId = choiceId,
                            QuestionId = questionId,
                            ChoiceLabel = labels[j],
                            ChoiceText = $"Đáp án {labels[j]}",
                            IsCorrect = j == 0,
                            DisplayOrder = j + 1
                        };
                        answersToSave.Add(answerChoice);

                        answerChoices.Add(new AnswerChoiceDto
                        {
                            ChoiceId = choiceId,
                            ChoiceLabel = labels[j],
                            ChoiceText = $"Đáp án {labels[j]}",
                            IsCorrect = j == 0,
                            DisplayOrder = j + 1
                        });
                    }

                    questions.Add(new QuestionDto
                    {
                        QuestionId = questionId,
                        Topic = chapter.ChapterName,
                        QuestionText = questionEntity.QuestionText,
                        QuestionType = "multiple_choice",
                        Difficulty = difficulty,
                        CreatedBy = "ai_system",
                        CreatedAt = DateTime.UtcNow,
                        AnswerChoices = answerChoices
                    });
                }

                // Save new questions and answers to database
                _context.Questions.AddRange(questionsToSave);
                _context.AnswerChoices.AddRange(answersToSave);
                await _context.SaveChangesAsync();
            }

            return questions.Take(count).ToList();
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