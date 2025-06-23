using Microsoft.AspNetCore.Mvc;
using BE_Phygens.DTOs;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExamsController : ControllerBase
    {
        private readonly PhygensContext _context;

        public ExamsController(PhygensContext context)
        {
            _context = context;
        }

        private string? ConvertToJsonString(object? value)
        {
            if (value == null) return null;
            if (value is string str)
            {
                if (string.IsNullOrEmpty(str) || str == "string") return null;
                return str;
            }
            return JsonSerializer.Serialize(value);
        }

        // GET: exams
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
                    .ThenInclude(q => q.Topic)
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

        // GET: exams/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamById(string id)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .ThenInclude(q => q.Topic)
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .ThenInclude(q => q.AnswerChoices)
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
                        Topic = eq.Question.Topic?.TopicName ?? "",
                        AnswerChoices = eq.Question.AnswerChoices?.Select(ac => new AnswerChoiceDto
                        {
                            ChoiceId = ac.ChoiceId,
                            ChoiceLabel = ac.ChoiceLabel,
                            ChoiceText = ac.ChoiceText,
                            IsCorrect = ac.IsCorrect,
                            DisplayOrder = ac.DisplayOrder ?? 0
                        }).ToList() ?? new List<AnswerChoiceDto>()
                    } : null
                }).ToList() ?? new List<ExamQuestionDto>()
            };

            return Ok(examDto);
        }

        // POST: exams
        [HttpPost]
        public async Task<IActionResult> CreateExam([FromBody] ExamCreateDto examDto)
        {
            // Check if exam with same name and config already exists
            if (!string.IsNullOrEmpty(examDto.ExamName))
            {
                var existingExam = await _context.Exams
                    .Where(e => e.ExamName == examDto.ExamName && 
                               e.CreatedBy == examDto.CreatedBy &&
                               e.ExamType == examDto.ExamType)
                    .FirstOrDefaultAsync();

                if (existingExam != null)
                {
                    Console.WriteLine($"Exam already exists: {existingExam.ExamId}");
                    return Ok(new { 
                        success = true, 
                        data = existingExam,
                        message = "Exam already exists, returning existing exam" 
                    });
                }
            }
            // FIX: Validate user exists
            var userExists = await _context.Users.AnyAsync(u => u.UserId == examDto.CreatedBy);
            if (!userExists)
            {
                // Try to find any admin/teacher user as fallback
                var fallbackUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Role == "admin" );
                
                if (fallbackUser != null)
                {
                    examDto.CreatedBy = fallbackUser.UserId;
                }
                else
                {
                    return BadRequest(new { 
                        error = "Invalid user and no fallback user available",
                        providedUser = examDto.CreatedBy 
                    });
                }
            }

            // Validate questions exist if provided and create placeholders for AI questions
            if (examDto.Questions != null && examDto.Questions.Any())
            {
                var questionIds = examDto.Questions.Select(q => q.QuestionId).Distinct().ToList();
                var existingQuestions = await _context.Questions
                    .Where(q => questionIds.Contains(q.QuestionId))
                    .Select(q => q.QuestionId)
                    .ToListAsync();
                
                var missingQuestionIds = questionIds.Except(existingQuestions).ToList();
                if (missingQuestionIds.Any())
                {
                    Console.WriteLine($"⚠️ Creating {missingQuestionIds.Count} placeholder questions for AI-generated content...");
                    
                    // Create placeholder questions for AI-generated ones
                    foreach (var missingId in missingQuestionIds)
                    {
                        // Check if it's UUID format (AI-generated)
                        if (Guid.TryParse(missingId, out _))
                        {
                            var placeholderQuestion = new Question
                            {
                                QuestionId = missingId,
                                QuestionText = "[AI Generated Question - Content loaded from frontend]",
                                QuestionType = "multiple_choice",
                                DifficultyLevel = "medium",
                                CreatedBy = "ai_system",
                                CreatedAt = DateTime.UtcNow,
                                TopicId = "TOPIC_001", // Default topic
                                IsActive = true
                            };
                            
                            _context.Questions.Add(placeholderQuestion);
                            Console.WriteLine($"   ✅ Created placeholder: {missingId}");
                        }
                        else
                        {
                            Console.WriteLine($"   ❌ Skipping non-UUID question: {missingId}");
                        }
                    }
                    
                    // Save placeholder questions first
                    await _context.SaveChangesAsync();
                }
            }

            var exam = new Exam
            {
                ExamId = Guid.NewGuid().ToString(),
                ExamName = examDto.ExamName,
                Description = examDto.Description,
                DurationMinutes = examDto.DurationMinutes,
                ExamType = examDto.ExamType,
                CreatedBy = examDto.CreatedBy,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                AiGenerationConfig = ConvertToJsonString(examDto.AiGenerationConfig)
            };

            _context.Exams.Add(exam);

            if (examDto.Questions != null && examDto.Questions.Any())
            {
                // Check for existing ExamQuestions to avoid duplicates
                var existingExamQuestions = await _context.ExamQuestions
                    .Where(eq => eq.ExamId == exam.ExamId)
                    .Select(eq => new { eq.QuestionId, eq.QuestionOrder })
                    .ToListAsync();

                foreach (var questionDto in examDto.Questions)
                {
                    // Skip if this question already exists in this exam
                    var exists = existingExamQuestions.Any(eq => 
                        eq.QuestionId == questionDto.QuestionId);
                    
                    if (exists)
                    {
                        Console.WriteLine($"Skipping duplicate question {questionDto.QuestionId} for exam {exam.ExamId}");
                        continue;
                    }

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

        // PUT: exams/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExam(string id, [FromBody] ExamUpdateDto examDto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Validate questions exist if provided
            if (examDto.Questions != null && examDto.Questions.Any())
            {
                var questionIds = examDto.Questions.Select(q => q.QuestionId).Distinct().ToList();
                var existingQuestions = await _context.Questions
                    .Where(q => questionIds.Contains(q.QuestionId))
                    .Select(q => q.QuestionId)
                    .ToListAsync();
                
                var missingQuestionIds = questionIds.Except(existingQuestions).ToList();
                if (missingQuestionIds.Any())
                {
                    return BadRequest(new { 
                        error = "Some questions do not exist", 
                        missingQuestionIds = missingQuestionIds 
                    });
                }
            }

            exam.ExamName = examDto.ExamName;
            exam.Description = examDto.Description;
            exam.DurationMinutes = examDto.DurationMinutes;
            exam.ExamType = examDto.ExamType;
            exam.IsPublished = examDto.IsPublished;
            exam.AiGenerationConfig = ConvertToJsonString(examDto.AiGenerationConfig);

            // Remove existing questions
            var existingExamQuestions = await _context.ExamQuestions
                .Where(eq => eq.ExamId == id)
                .ToListAsync();
            _context.ExamQuestions.RemoveRange(existingExamQuestions);

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

        // DELETE: exams/{id}
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

        // POST: exams/generate
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
                // FIX: Get questions from database based on difficulty and topic
                var availableQuestions = await _context.Questions
                    .Include(q => q.Topic)
                    .Where(q => q.DifficultyLevel.ToLower() == "easy" && q.Topic.TopicName == matrix.Topic)
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
                    .Where(q => q.DifficultyLevel.ToLower() == "medium" && q.Topic.TopicName == matrix.Topic)
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