using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;

namespace BE_Phygens.Controllers
{
    [Route("api/smart-exam")]
    [ApiController]
    [Authorize]
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
                    ExamType = request.ExamType,
                    Grade = request.Grade,
                    Duration = request.Duration,
                    TotalQuestions = request.ChapterDetails.Sum(c => c.QuestionCount),
                    TotalPoints = request.TotalPoints,
                    Description = request.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.Set<ExamMatrix>().Add(examMatrix);

                // Add ExamMatrixDetails
                foreach (var detail in request.ChapterDetails)
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

                // Generate exam
                var examId = Guid.NewGuid().ToString();
                var exam = new Exam
                {
                    ExamId = examId,
                    ExamName = $"{examMatrix.ExamName} - {DateTime.Now:dd/MM/yyyy HH:mm}",
                    Description = $"Đề thi được tạo tự động từ ma trận: {examMatrix.ExamName}",
                    DurationMinutes = examMatrix.Duration,
                    ExamType = examMatrix.ExamType,
                    CreatedBy = User.Identity?.Name ?? "AI_System",
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Exams.Add(exam);

                // Generate questions for each chapter
                var examQuestions = new List<ExamQuestionDto>();
                int questionOrder = 1;

                foreach (var detail in examMatrix.ExamMatrixDetails)
                {
                    var questions = await GetQuestionsForChapter(detail.ChapterId, detail.QuestionCount, detail.DifficultyLevel);
                    
                    foreach (var question in questions)
                    {
                        var examQuestionId = Guid.NewGuid().ToString();
                        var examQuestion = new ExamQuestion
                        {
                            ExamQuestionId = examQuestionId,
                            ExamId = examId,
                            QuestionId = question.QuestionId,
                            QuestionOrder = questionOrder++,
                            PointsWeight = CalculateQuestionPoints(detail.DifficultyLevel, examMatrix.TotalPoints, examMatrix.TotalQuestions),
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
                    TotalPoints = examMatrix.TotalPoints,
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

                if (grade.HasValue)
                    query = query.Where(em => em.Grade == grade.Value);

                var matrices = await query
                    .Where(em => em.IsActive)
                    .OrderByDescending(em => em.CreatedAt)
                    .Select(em => new ExamMatrixListDto
                    {
                        MatrixId = em.MatrixId,
                        ExamName = em.ExamName,
                        ExamType = em.ExamType,
                        Grade = em.Grade,
                        Duration = em.Duration,
                        TotalQuestions = em.TotalQuestions,
                        TotalPoints = em.TotalPoints,
                        CreatedAt = em.CreatedAt,
                        CreatedBy = em.CreatedBy ?? ""
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
            // For demo - return mock questions
            var chapter = await _context.Set<Chapter>().FindAsync(chapterId);
            if (chapter == null) return new List<QuestionDto>();

            var questions = new List<QuestionDto>();
            for (int i = 0; i < count; i++)
            {
                questions.Add(new QuestionDto
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    Topic = chapter.ChapterName,
                    QuestionText = $"Câu hỏi {i + 1} về {chapter.ChapterName} (độ khó: {difficulty})",
                    QuestionType = "multiple_choice",
                    Difficulty = difficulty,
                    CreatedBy = "AI_System",
                    CreatedAt = DateTime.UtcNow,
                    AnswerChoices = new List<AnswerChoiceDto>
                    {
                        new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "A", ChoiceText = "Đáp án A" },
                        new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "B", ChoiceText = "Đáp án B" },
                        new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "C", ChoiceText = "Đáp án C" },
                        new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "D", ChoiceText = "Đáp án D" }
                    }
                });
            }

            return questions;
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
    }
} 