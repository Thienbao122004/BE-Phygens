using Microsoft.AspNetCore.Mvc;
using BE_Phygens.DTOs;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController : ControllerBase
    {
        private readonly PhygensContext _context;

        public ExamsController(PhygensContext context)
        {
            _context = context;
        }

        // GET: api/exams
        [HttpGet]
        public async Task<IActionResult> GetAllExams()
        {
            try
            {
                // Test database connection first
                await _context.Database.CanConnectAsync();
                
                var exams = await _context.Exams
                    .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                    .ToListAsync();
            
            var examDtos = exams.Select(e => new ExamDto
            {
                ExamId = e.ExamId,
                ExamName = e.ExamName,
                Description = e.Description,
                DurationMinutes = e.DurationMinutes ?? 0,
                ExamType = e.ExamType,
                CreatedBy = e.CreatedBy,
                IsPublished = e.IsPublished,
                CreatedAt = e.CreatedAt,
                Questions = e.ExamQuestions?.Select(eq => new ExamQuestionDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    QuestionOrder = eq.QuestionOrder ?? 0,
                    PointsWeight = eq.PointsWeight,
                    Question = eq.Question != null ? new QuestionDto
                    {
                        QuestionId = eq.Question.QuestionId,
                        QuestionText = eq.Question.QuestionText,
                        Difficulty = eq.Question.DifficultyLevel,
                        Topic = eq.Question.Topic?.TopicName ?? ""
                    } : null
                }).ToList() ?? new List<ExamQuestionDto>()
            }).ToList();

                return Ok(examDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message,
                    details = ex.InnerException?.Message 
                });
            }
        }

        // GET: api/exams/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamById(string id)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.ExamId == id);

            if (exam == null) return NotFound();

            var examDto = new ExamDto
            {
                ExamId = exam.ExamId,
                ExamName = exam.ExamName,
                Description = exam.Description,
                DurationMinutes = exam.DurationMinutes ?? 0,
                ExamType = exam.ExamType,
                CreatedBy = exam.CreatedBy,
                IsPublished = exam.IsPublished,
                CreatedAt = exam.CreatedAt,
                Questions = exam.ExamQuestions?.Select(eq => new ExamQuestionDto
                {
                    ExamQuestionId = eq.ExamQuestionId,
                    QuestionId = eq.QuestionId,
                    QuestionOrder = eq.QuestionOrder ?? 0,
                    PointsWeight = eq.PointsWeight,
                    Question = eq.Question != null ? new QuestionDto
                    {
                        QuestionId = eq.Question.QuestionId,
                        QuestionText = eq.Question.QuestionText,
                        Difficulty = eq.Question.DifficultyLevel,
                        Topic = eq.Question.Topic?.TopicName ?? ""
                    } : null
                }).ToList() ?? new List<ExamQuestionDto>()
            };

            return Ok(examDto);
        }

        // POST: api/exams
        [HttpPost]
        public async Task<IActionResult> CreateExam([FromBody] ExamCreateDto examDto)
        {
            var exam = new Exam
            {
                ExamId = Guid.NewGuid().ToString(),
                ExamName = examDto.ExamName,
                Description = examDto.Description,
                DurationMinutes = examDto.DurationMinutes,
                ExamType = examDto.ExamType,
                CreatedBy = examDto.CreatedBy,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(exam);

            if (examDto.Questions != null)
            {
                foreach (var questionDto in examDto.Questions)
                {
                    var examQuestion = new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        ExamId = exam.ExamId,
                        QuestionId = questionDto.QuestionId,
                        QuestionOrder = questionDto.QuestionOrder,
                        PointsWeight = questionDto.PointsWeight,
                        AddedAt = DateTime.UtcNow
                    };
                    _context.ExamQuestions.Add(examQuestion);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExamById), new { id = exam.ExamId }, exam);
        }

        // PUT: api/exams/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExam(string id, [FromBody] ExamUpdateDto examDto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            exam.ExamName = examDto.ExamName;
            exam.Description = examDto.Description;
            exam.DurationMinutes = examDto.DurationMinutes;
            exam.ExamType = examDto.ExamType;
            exam.IsPublished = examDto.IsPublished;

            // Remove existing questions
            var existingQuestions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id)
                .ToListAsync();
            _context.ExamQuestions.RemoveRange(existingQuestions);

            // Add new questions
            if (examDto.Questions != null)
            {
                foreach (var questionDto in examDto.Questions)
                {
                    var examQuestion = new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        ExamId = exam.ExamId,
                        QuestionId = questionDto.QuestionId,
                        QuestionOrder = questionDto.QuestionOrder,
                        PointsWeight = questionDto.PointsWeight,
                        AddedAt = DateTime.UtcNow
                    };
                    _context.ExamQuestions.Add(examQuestion);
                }
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/exams/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExam(string id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Remove related exam questions first
            var examQuestions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id)
                .ToListAsync();
            _context.ExamQuestions.RemoveRange(examQuestions);

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/exams/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateExam([FromBody] ExamGenerateDto generateDto)
        {
            var exam = new Exam
            {
                ExamId = Guid.NewGuid().ToString(),
                ExamName = generateDto.ExamName,
                Description = generateDto.Description,
                DurationMinutes = generateDto.DurationMinutes,
                ExamType = generateDto.ExamType,
                CreatedBy = generateDto.CreatedBy,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(exam);

            // Generate questions based on matrix
            var questions = new List<ExamQuestion>();
            int order = 1;
            foreach (var matrix in generateDto.Matrix)
            {
                // Get questions from database based on difficulty and topic
                var availableQuestions = await _context.Questions
                    .Include(q => q.Topic)
                    .Where(q => q.DifficultyLevel == "Easy" && q.Topic.TopicName == matrix.Topic)
                    .Take(matrix.NumEasy)
                    .ToListAsync();

                foreach (var question in availableQuestions)
                {
                    questions.Add(new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        ExamId = exam.ExamId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = order++,
                        PointsWeight = 1,
                        AddedAt = DateTime.UtcNow
                    });
                }

                // Add medium questions
                var mediumQuestions = await _context.Questions
                    .Include(q => q.Topic)
                    .Where(q => q.DifficultyLevel == "Medium" && q.Topic.TopicName == matrix.Topic)
                    .Take(matrix.NumMedium)
                    .ToListAsync();

                foreach (var question in mediumQuestions)
                {
                    questions.Add(new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        ExamId = exam.ExamId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = order++,
                        PointsWeight = 1,
                        AddedAt = DateTime.UtcNow
                    });
                }

                // Add hard questions
                var hardQuestions = await _context.Questions
                    .Include(q => q.Topic)
                    .Where(q => q.DifficultyLevel == "Hard" && q.Topic.TopicName == matrix.Topic)
                    .Take(matrix.NumHard)
                    .ToListAsync();

                foreach (var question in hardQuestions)
                {
                    questions.Add(new ExamQuestion
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        ExamId = exam.ExamId,
                        QuestionId = question.QuestionId,
                        QuestionOrder = order++,
                        PointsWeight = 1,
                        AddedAt = DateTime.UtcNow
                    });
                }
            }

            _context.ExamQuestions.AddRange(questions);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetExamById), new { id = exam.ExamId }, exam);
        }
    }
}