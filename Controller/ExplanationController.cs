using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;

namespace BE_Phygens.Controller
{
    [Route("explanation")]
    [ApiController]
    // [Authorize] // Tạm thời bỏ auth để test
    public class ExplanationController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly ILogger<ExplanationController> _logger;

        public ExplanationController(PhygensContext context, ILogger<ExplanationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/explanation/{questionId} - Lấy explanation của một câu hỏi
        /// </summary>
        [HttpGet("{questionId}")]
        public async Task<IActionResult> GetExplanationByQuestionId(string questionId)
        {
            try
            {
                if (string.IsNullOrEmpty(questionId))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId là bắt buộc" });
                }

                var explanation = await _context.Explanations
                    .Include(e => e.Question)
                    .Include(e => e.Creator)
                    .FirstOrDefaultAsync(e => e.QuestionId == questionId);

                if (explanation == null)
                {
                    return NotFound(new { error = "not_found", message = "Không tìm thấy giải thích cho câu hỏi này" });
                }

                return Ok(new
                {
                    explanationId = explanation.ExplanationId,
                    questionId = explanation.QuestionId,
                    explanationText = explanation.ExplanationText,
                    createdBy = explanation.CreatedBy,
                    createdAt = explanation.CreatedAt,
                    question = new
                    {
                        questionId = explanation.Question.QuestionId,
                        questionText = explanation.Question.QuestionText,
                        difficultyLevel = explanation.Question.DifficultyLevel
                    },
                    creator = new
                    {
                        userId = explanation.Creator.UserId,
                        username = explanation.Creator.Username,
                        fullName = explanation.Creator.FullName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy explanation cho câu hỏi {QuestionId}", questionId);
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy giải thích" });
            }
        }

        /// <summary>
        /// GET: api/explanation - Lấy tất cả explanation với phân trang
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllExplanations([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Explanations
                    .Include(e => e.Question)
                    .Include(e => e.Creator)
                    .AsQueryable();

                // Tìm kiếm theo explanationText hoặc questionText
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e => 
                        e.ExplanationText.Contains(search) || 
                        e.Question.QuestionText.Contains(search));
                }

                var totalItems = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var explanations = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(e => new
                    {
                        explanationId = e.ExplanationId,
                        questionId = e.QuestionId,
                        explanationText = e.ExplanationText,
                        createdBy = e.CreatedBy,
                        createdAt = e.CreatedAt,
                        question = new
                        {
                            questionId = e.Question.QuestionId,
                            questionText = e.Question.QuestionText.Length > 100 
                                ? e.Question.QuestionText.Substring(0, 100) + "..." 
                                : e.Question.QuestionText,
                            difficultyLevel = e.Question.DifficultyLevel
                        },
                        creator = new
                        {
                            userId = e.Creator.UserId,
                            username = e.Creator.Username,
                            fullName = e.Creator.FullName
                        }
                    })
                    .ToListAsync();

                return Ok(new
                {
                    data = explanations,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalItems,
                        totalPages = totalPages,
                        hasNextPage = page < totalPages,
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách explanation");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy danh sách giải thích" });
            }
        }

        /// <summary>
        /// POST: api/explanation - Tạo explanation mới cho câu hỏi
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateExplanation([FromBody] CreateExplanationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.QuestionId) || string.IsNullOrEmpty(request.ExplanationText))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId và ExplanationText là bắt buộc" });
                }

                // Kiểm tra câu hỏi có tồn tại không
                var questionExists = await _context.Questions.AnyAsync(q => q.QuestionId == request.QuestionId);
                if (!questionExists)
                {
                    return BadRequest(new { error = "invalid_question", message = "Câu hỏi không tồn tại" });
                }

                // Kiểm tra đã có explanation cho câu hỏi này chưa
                var existingExplanation = await _context.Explanations
                    .FirstOrDefaultAsync(e => e.QuestionId == request.QuestionId);

                if (existingExplanation != null)
                {
                    return Conflict(new { error = "already_exists", message = "Câu hỏi này đã có giải thích" });
                }

                // Lấy userId từ JWT token (giả sử có trong claims)
                var userId = User.FindFirst("userId")?.Value ?? "system";

                var explanation = new Explanation
                {
                    ExplanationId = Guid.NewGuid().ToString(),
                    QuestionId = request.QuestionId,
                    ExplanationText = request.ExplanationText,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Explanations.Add(explanation);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetExplanationByQuestionId), 
                    new { questionId = request.QuestionId }, 
                    new
                    {
                        explanationId = explanation.ExplanationId,
                        questionId = explanation.QuestionId,
                        explanationText = explanation.ExplanationText,
                        createdBy = explanation.CreatedBy,
                        createdAt = explanation.CreatedAt,
                        message = "Tạo giải thích thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo explanation");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi tạo giải thích" });
            }
        }

        /// <summary>
        /// PUT: api/explanation/{explanationId} - Cập nhật explanation
        /// </summary>
        [HttpPut("{explanationId}")]
        public async Task<IActionResult> UpdateExplanation(string explanationId, [FromBody] UpdateExplanationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(explanationId) || string.IsNullOrEmpty(request.ExplanationText))
                {
                    return BadRequest(new { error = "validation_error", message = "ExplanationId và ExplanationText là bắt buộc" });
                }

                var explanation = await _context.Explanations.FindAsync(explanationId);
                if (explanation == null)
                {
                    return NotFound(new { error = "not_found", message = "Không tìm thấy giải thích" });
                }

                explanation.ExplanationText = request.ExplanationText;
                
                _context.Explanations.Update(explanation);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    explanationId = explanation.ExplanationId,
                    questionId = explanation.QuestionId,
                    explanationText = explanation.ExplanationText,
                    createdBy = explanation.CreatedBy,
                    createdAt = explanation.CreatedAt,
                    message = "Cập nhật giải thích thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật explanation {ExplanationId}", explanationId);
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi cập nhật giải thích" });
            }
        }

        /// <summary>
        /// DELETE: api/explanation/{explanationId} - Xóa explanation
        /// </summary>
        [HttpDelete("{explanationId}")]
        public async Task<IActionResult> DeleteExplanation(string explanationId)
        {
            try
            {
                if (string.IsNullOrEmpty(explanationId))
                {
                    return BadRequest(new { error = "validation_error", message = "ExplanationId là bắt buộc" });
                }

                var explanation = await _context.Explanations.FindAsync(explanationId);
                if (explanation == null)
                {
                    return NotFound(new { error = "not_found", message = "Không tìm thấy giải thích" });
                }

                _context.Explanations.Remove(explanation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa giải thích thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa explanation {ExplanationId}", explanationId);
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi xóa giải thích" });
            }
        }
    }

    // Request DTOs
    public class CreateExplanationRequest
    {
        public string QuestionId { get; set; } = string.Empty;
        public string ExplanationText { get; set; } = string.Empty;
    }

    public class UpdateExplanationRequest
    {
        public string ExplanationText { get; set; } = string.Empty;
    }
} 