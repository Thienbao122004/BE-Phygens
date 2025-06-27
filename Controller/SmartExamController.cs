using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;
using BE_Phygens.Services;

namespace BE_Phygens.Controllers
{
    [Route("smart-exam")]
    [ApiController]
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

        // DTO Classes
        public class CreateExamMatrixDto
        {
            public string ExamName { get; set; } = string.Empty;
            public string? ExamType { get; set; }
            public List<ChapterDetailDto> ChapterDetails { get; set; } = new();
        }

        public class ChapterDetailDto
        {
            public int ChapterId { get; set; }
            public int QuestionCount { get; set; }
            public string DifficultyLevel { get; set; } = "medium";
        }

        public class UpdateExamMatrixDto
        {
            public string? ExamName { get; set; }
            public string? Subject { get; set; }
            public List<ChapterDetailDto>? ChapterDetails { get; set; }
        }

        // GET: smart-exams/matrices
        [HttpGet("matrices")]
        public async Task<IActionResult> GetAllMatrices([FromQuery] int? grade = null)
        {
            try
            {
                var query = _context.Set<ExamMatrix>().AsQueryable();

                if (grade.HasValue)
                {
                    query = query.Where(m => _context.Set<ExamMatrixDetail>()
                        .Include(d => d.Chapter)
                        .Where(d => d.ExamMatrixId == m.MatrixId)
                        .Any(d => d.Chapter.Grade == grade.Value));
                }

                var matrices = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new
                    {
                        matrixId = m.MatrixId,
                        examName = m.ExamName,
                        subject = m.Subject,
                        topic = m.Topic,
                        totalEasy = m.NumEasy,
                        totalMedium = m.NumMedium,
                        totalHard = m.NumHard,
                        createdAt = m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(matrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam matrices");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // GET: smart-exams/matrices/{id}
        [HttpGet("matrices/{id}")]
        public async Task<IActionResult> GetMatrixById(string id)
        {
            try
            {
                var matrix = await _context.Set<ExamMatrix>()
                    .Include(m => m.ExamMatrixDetails)
                    .ThenInclude(d => d.Chapter)
                    .FirstOrDefaultAsync(m => m.MatrixId == id);

                if (matrix == null)
                    return NotFound(new { error = "Exam matrix not found" });

                var matrixDto = new
                {
                    matrixId = matrix.MatrixId,
                    examName = matrix.ExamName,
                    subject = matrix.Subject,
                    topic = matrix.Topic,
                    numEasy = matrix.NumEasy,
                    numMedium = matrix.NumMedium,
                    numHard = matrix.NumHard,
                    createdAt = matrix.CreatedAt,
                    details = matrix.ExamMatrixDetails.Select(d => new
                    {
                        chapterId = d.ChapterId,
                        chapterName = d.Chapter?.ChapterName,
                        questionCount = d.QuestionCount,
                        difficultyLevel = d.DifficultyLevel
                    }).ToList()
                };

                return Ok(matrixDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam matrix by ID");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // POST: smart-exams/matrices
        [HttpPost("matrices")]
        public async Task<IActionResult> CreateMatrix([FromBody] CreateExamMatrixDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ExamName))
                {
                    return BadRequest(new { error = "Exam name is required" });
                }

                if (!request.ChapterDetails.Any())
                {
                    return BadRequest(new { error = "At least one chapter detail is required" });
                }

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

                // Validate chapters exist
                var requestedChapterIds = request.ChapterDetails.Select(c => c.ChapterId).ToList();
                var existingChapters = await _context.Chapters
                    .Where(c => requestedChapterIds.Contains(c.ChapterId))
                    .ToListAsync();

                if (!existingChapters.Any())
                {
                    var allChapters = await _context.Chapters
                        .Where(c => c.IsActive)
                        .Select(c => new { c.ChapterId, c.ChapterName, c.Grade })
                        .ToListAsync();

                    var availableList = allChapters.Any()
                        ? string.Join(", ", allChapters.Select(c => $"ID={c.ChapterId}({c.ChapterName}, Grade {c.Grade})"))
                        : "No chapters available in database";

                    return BadRequest(new { 
                        error = $"Chapters not found with IDs: {string.Join(", ", requestedChapterIds)}",
                        availableChapters = availableList
                    });
                }

                // Create matrix details
                foreach (var detail in request.ChapterDetails)
                {
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
                }

                await _context.SaveChangesAsync();

                var responseDto = new
                {
                    matrixId = examMatrix.MatrixId,
                    examName = examMatrix.ExamName,
                    subject = examMatrix.Subject,
                    topic = examMatrix.Topic,
                    numEasy = examMatrix.NumEasy,
                    numMedium = examMatrix.NumMedium,
                    numHard = examMatrix.NumHard,
                    createdAt = examMatrix.CreatedAt
                };

                return CreatedAtAction(nameof(GetMatrixById), new { id = examMatrix.MatrixId }, responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating exam matrix");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // PUT: smart-exams/matrices/{id}
        [HttpPut("matrices/{id}")]
        public async Task<IActionResult> UpdateMatrix(string id, [FromBody] UpdateExamMatrixDto request)
        {
            try
            {
                var matrix = await _context.Set<ExamMatrix>()
                    .Include(m => m.ExamMatrixDetails)
                    .FirstOrDefaultAsync(m => m.MatrixId == id);

                if (matrix == null)
                    return NotFound(new { error = "Exam matrix not found" });

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.ExamName))
                    matrix.ExamName = request.ExamName;

                if (!string.IsNullOrEmpty(request.Subject))
                    matrix.Subject = request.Subject;

                // Update chapter details if provided
                if (request.ChapterDetails != null && request.ChapterDetails.Any())
                {
                    // Remove existing details
                    _context.Set<ExamMatrixDetail>().RemoveRange(matrix.ExamMatrixDetails);

                    // Add new details
                    foreach (var detail in request.ChapterDetails)
                    {
                        var matrixDetail = new ExamMatrixDetail
                        {
                            ExamMatrixId = id,
                            ChapterId = detail.ChapterId,
                            QuestionCount = detail.QuestionCount,
                            DifficultyLevel = detail.DifficultyLevel
                        };
                        _context.Set<ExamMatrixDetail>().Add(matrixDetail);
                    }

                    // Update counts
                    matrix.NumEasy = request.ChapterDetails.Where(c => c.DifficultyLevel == "easy").Sum(c => c.QuestionCount);
                    matrix.NumMedium = request.ChapterDetails.Where(c => c.DifficultyLevel == "medium").Sum(c => c.QuestionCount);
                    matrix.NumHard = request.ChapterDetails.Where(c => c.DifficultyLevel == "hard").Sum(c => c.QuestionCount);
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating exam matrix");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // DELETE: smart-exams/matrices/{id}
        [HttpDelete("matrices/{id}")]
        public async Task<IActionResult> DeleteMatrix(string id)
        {
            try
            {
                var matrix = await _context.Set<ExamMatrix>()
                    .Include(m => m.ExamMatrixDetails)
                    .FirstOrDefaultAsync(m => m.MatrixId == id);

                if (matrix == null)
                    return NotFound(new { error = "Exam matrix not found" });

                // Remove details first
                _context.Set<ExamMatrixDetail>().RemoveRange(matrix.ExamMatrixDetails);
                
                // Remove matrix
                _context.Set<ExamMatrix>().Remove(matrix);
                
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting exam matrix");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // POST: smart-exams/matrices/{matrixId}/generate
        [HttpPost("matrices/{matrixId}/generate")]
        public async Task<IActionResult> GenerateExamFromMatrix(string matrixId)
        {
            try
            {
                var examMatrix = await _context.Set<ExamMatrix>()
                    .Include(em => em.ExamMatrixDetails)
                    .ThenInclude(emd => emd.Chapter)
                    .FirstOrDefaultAsync(em => em.MatrixId == matrixId);

                if (examMatrix == null)
                    return NotFound(new { error = "Exam matrix not found" });

                var validQuestions = new List<object>();
                
                foreach (var detail in examMatrix.ExamMatrixDetails)
                {
                    try
                    {
                        var existingQuestions = await _context.Questions
                            .AsNoTracking()
                            .Where(q => q.ChapterId == detail.ChapterId && q.IsActive)
                            .Take(detail.QuestionCount)
                            .Select(q => new
                            {
                                questionId = q.QuestionId,
                                questionText = q.QuestionText,
                                questionType = q.QuestionType,
                                difficulty = q.DifficultyLevel,
                                chapterId = q.ChapterId,
                                topic = q.Topic.TopicName
                            })
                            .ToListAsync();

                        validQuestions.AddRange(existingQuestions);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing questions for chapter {detail.ChapterId}");
                    }
                }

                if (!validQuestions.Any())
                {
                    return BadRequest(new { error = "No questions could be generated for this matrix" });
                }

                var generatedExam = new
                {
                    examId = Guid.NewGuid().ToString(),
                    examName = examMatrix.ExamName,
                    subject = examMatrix.Subject,
                    topic = examMatrix.Topic,
                    duration = 45,
                    questionCount = validQuestions.Count,
                    questions = validQuestions,
                    createdAt = DateTime.UtcNow
                };

                return Ok(generatedExam);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating exam from matrix");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // GET: smart-exams/chapters
        [HttpGet("chapters")]
        public async Task<IActionResult> GetChapters()
        {
            try
            {
                var chapters = await _context.Chapters
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Grade)
                    .ThenBy(c => c.DisplayOrder)
                    .Select(c => new
                    {
                        chapterId = c.ChapterId,
                        chapterName = c.ChapterName,
                        grade = c.Grade,
                        description = c.Description,
                        displayOrder = c.DisplayOrder,
                        isActive = c.IsActive
                    })
                    .ToListAsync();

                return Ok(chapters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapters");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // GET: smart-exams/templates
        [HttpGet("templates")]
        public async Task<IActionResult> GetExamTemplates()
        {
            try
            {
                var templates = new List<object>
                {
                    new {
                        templateId = "template_15p",
                        templateName = "Kiểm tra 15 phút",
                        duration = 15,
                        totalQuestions = 10,
                        description = "Template for 15-minute quizzes"
                    },
                    new {
                        templateId = "template_1tiet",
                        templateName = "Kiểm tra 1 tiết",
                        duration = 45,
                        totalQuestions = 20,
                        description = "Template for 1-period exams"
                    },
                    new {
                        templateId = "template_cuoiky",
                        templateName = "Thi cuối kỳ",
                        duration = 90,
                        totalQuestions = 40,
                        description = "Template for final exams"
                    }
                };

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam templates");
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }
    }
} 