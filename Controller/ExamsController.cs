using Microsoft.AspNetCore.Mvc;
using BE_Phygens.Dto;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using BE_Phygens.Services;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("exams")]
    public class ExamsController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly INotificationService _notificationService;

        public ExamsController(PhygensContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        private static readonly string[] ValidExamTypes = { "15p", "1tiet", "cuoiky", "ai_generated", "smart_exam", "adaptive" };

        private string ValidateExamType(string examType, string fallback = "ai_generated")
        {
            return ValidExamTypes.Contains(examType) ? examType : fallback;
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
        public async Task<IActionResult> GetAllExams([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string sortBy = "createdAt", [FromQuery] string sortDirection = "desc")
        {
            try
            {
                // Test database connection first
                await _context.Database.CanConnectAsync();

                var query = _context.Exams
                    .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Topic)
                    .AsNoTracking();
                if (sortBy == "createdAt")
                {
                    query = sortDirection == "asc"
                        ? query.OrderBy(e => e.CreatedAt)
                        : query.OrderByDescending(e => e.CreatedAt);
                }
                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var exams = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var examDtos = exams.Select(e => new ExamDetailsDto
                {
                    ExamId = e.ExamId,
                    ExamName = e.ExamName,
                    Description = e.Description,
                    DurationMinutes = e.DurationMinutes ?? 1,
                    ExamType = e.ExamType,
                    CreatedBy = e.CreatedBy,
                    IsPublished = e.IsPublished,
                    CreatedAt = e.CreatedAt,
                    Questions = e.ExamQuestions?.Select(eq => new ExamQuestionResponseDto
                    {
                        ExamQuestionId = eq.ExamQuestionId,
                        QuestionId = eq.QuestionId,
                        QuestionOrder = eq.QuestionOrder ?? 0,
                        PointsWeight = eq.PointsWeight,
                        Question = eq.Question != null ? new QuestionResponseDto
                        {
                            QuestionId = eq.Question.QuestionId,
                            QuestionText = eq.Question.QuestionText,
                            QuestionType = eq.Question.QuestionType,
                            Difficulty = eq.Question.DifficultyLevel,
                            Topic = eq.Question.Topic?.TopicName ?? "",
                            MinWords = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<int?>(eq.Question.AiGenerationMetadata, "minWords") ?? 50 : null,
                            MaxWords = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<int?>(eq.Question.AiGenerationMetadata, "maxWords") ?? 300 : null,
                            EssayStyle = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<string>(eq.Question.AiGenerationMetadata, "essayStyle") ?? "analytical" : null
                        } : null
                    }).ToList() ?? new List<ExamQuestionResponseDto>()
                }).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Exams retrieved successfully",
                    data = examDtos,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }

        // GET: exams/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetExamById(string id)
        {
            try
            {
                var exam = await _context.Exams
                    .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Topic)
                    .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.AnswerChoices)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.ExamId == id);

                if (exam == null)
                    return NotFound(new
                    {
                        success = false,
                        message = $"Exam with ID {id} not found"
                    });

                var examDto = new ExamDetailsDto
                {
                    ExamId = exam.ExamId,
                    ExamName = exam.ExamName,
                    Description = exam.Description,
                    DurationMinutes = exam.DurationMinutes ?? 1,
                    ExamType = exam.ExamType,
                    CreatedBy = exam.CreatedBy,
                    IsPublished = exam.IsPublished,
                    CreatedAt = exam.CreatedAt,
                    Questions = exam.ExamQuestions?.Select(eq => new ExamQuestionResponseDto
                    {
                        ExamQuestionId = eq.ExamQuestionId,
                        QuestionId = eq.QuestionId,
                        QuestionOrder = eq.QuestionOrder ?? 0,
                        PointsWeight = eq.PointsWeight,
                        Question = eq.Question != null ? new QuestionResponseDto
                        {
                            QuestionId = eq.Question.QuestionId,
                            QuestionText = eq.Question.QuestionText,
                            QuestionType = eq.Question.QuestionType,
                            Difficulty = eq.Question.DifficultyLevel,
                            Topic = eq.Question.Topic?.TopicName ?? "",
                            AnswerChoices = eq.Question.AnswerChoices?.Select(ac => new AnswerChoiceResponseDto
                            {
                                ChoiceId = ac.ChoiceId,
                                ChoiceLabel = ac.ChoiceLabel,
                                ChoiceText = ac.ChoiceText,
                                IsCorrect = ac.IsCorrect,
                                DisplayOrder = ac.DisplayOrder
                            }).ToList() ?? new List<AnswerChoiceResponseDto>(),
                            MinWords = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<int?>(eq.Question.AiGenerationMetadata, "minWords") ?? 1 : null,
                            MaxWords = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<int?>(eq.Question.AiGenerationMetadata, "maxWords") ?? 300 : null,
                            EssayStyle = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<string>(eq.Question.AiGenerationMetadata, "essayStyle") ?? "analytical" : null,
                            KeyPoints = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<List<string>>(eq.Question.AiGenerationMetadata, "keyPoints") : null,
                            GradingRubric = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<string>(eq.Question.AiGenerationMetadata, "gradingRubric") : null,
                            SampleAnswer = eq.Question.QuestionType == "essay" ? GetEssayMetadataProperty<string>(eq.Question.AiGenerationMetadata, "sampleAnswer") : null
                        } : null
                    }).ToList() ?? new List<ExamQuestionResponseDto>()
                };

                return Ok(new
                {
                    success = true,
                    message = "Exam retrieved successfully",
                    data = examDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        // POST: exams
        [HttpPost]
        public async Task<IActionResult> CreateExam([FromBody] ExamCreateDto examDto)
        {
            // Validate ExamType to ensure it matches database constraint
            var examType = ValidateExamType(examDto.ExamType);

            // Get or create default user if CreatedBy is not provided
            var createdBy = !string.IsNullOrEmpty(examDto.CreatedBy) ? examDto.CreatedBy : GetOrCreateDefaultUser();

            // Check if exam with same name and config already exists
            if (!string.IsNullOrEmpty(examDto.ExamName))
            {
                var existingExam = await _context.Exams
                    .Where(e => e.ExamName == examDto.ExamName &&
                               e.CreatedBy == createdBy &&
                               e.ExamType == examType)
                    .FirstOrDefaultAsync();

                if (existingExam != null)
                {
                    return BadRequest(new
                    {
                        error = "Exam with same name and configuration already exists",
                        existingExamId = existingExam.ExamId
                    });
                }
            }

            // Create questions từ mini editor hoặc validate existing questions
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
                    Console.WriteLine($"⚠️ Creating {missingQuestionIds.Count} questions from mini editor...");

                    // Create questions from mini editor data
                    foreach (var questionDto in examDto.Questions.Where(q => missingQuestionIds.Contains(q.QuestionId)))
                    {
                        if (questionDto.QuestionData != null)
                        {
                            // Tạo question từ mini editor
                            var question = new Question
                            {
                                QuestionId = questionDto.QuestionId,
                                QuestionText = questionDto.QuestionData.QuestionText,
                                QuestionType = questionDto.QuestionData.QuestionType,
                                DifficultyLevel = questionDto.QuestionData.DifficultyLevel,
                                CreatedBy = createdBy,
                                CreatedAt = DateTime.UtcNow,
                                TopicId = "TOPIC_001", // Default topic
                                IsActive = true
                            };

                            _context.Questions.Add(question);
                            Console.WriteLine($"   ✅ Created question: {questionDto.QuestionId}");

                            // Tạo answer choices nếu có
                            if (questionDto.QuestionData.AnswerChoices != null && questionDto.QuestionData.AnswerChoices.Any())
                            {
                                var displayOrder = 1;
                                foreach (var choice in questionDto.QuestionData.AnswerChoices)
                                {
                                    var answerChoice = new AnswerChoice
                                    {
                                        ChoiceId = Guid.NewGuid().ToString(),
                                        QuestionId = questionDto.QuestionId,
                                        ChoiceLabel = choice.ChoiceLabel,
                                        ChoiceText = choice.ChoiceText,
                                        IsCorrect = choice.IsCorrect,
                                        DisplayOrder = displayOrder++
                                    };

                                    _context.AnswerChoices.Add(answerChoice);
                                }
                            }
                        }
                        else
                        {
                            // Fallback: tạo placeholder nếu không có question data
                            var placeholderQuestion = new Question
                            {
                                QuestionId = questionDto.QuestionId,
                                QuestionText = "[Question content not provided]",
                                QuestionType = "multiple_choice",
                                DifficultyLevel = "medium",
                                CreatedBy = createdBy,
                                CreatedAt = DateTime.UtcNow,
                                TopicId = "TOPIC_001",
                                IsActive = true
                            };

                            _context.Questions.Add(placeholderQuestion);
                            Console.WriteLine($"   ⚠️ Created placeholder: {questionDto.QuestionId}");
                        }
                    }

                    // Save questions first
                    await _context.SaveChangesAsync();
                }
            }

            var exam = new Exam
            {
                ExamId = Guid.NewGuid().ToString(),
                ExamName = examDto.ExamName,
                Description = examDto.Description,
                DurationMinutes = examDto.DurationMinutes > 0 ? examDto.DurationMinutes : 45, // ✅ Default 45 phút nếu <= 0
                ExamType = examType,
                CreatedBy = createdBy,
                IsPublished = false,
                IsAiGenerated = examType == "ai_generated" || examType == "smart_exam",
                CreatedAt = DateTime.UtcNow,
                AiGenerationConfig = ConvertToJsonString(examDto.AiGenerationConfig)
            };

            _context.Exams.Add(exam);

            if (examDto.Questions != null && examDto.Questions.Any())
            {
                // Remove duplicates from the request itself
                var uniqueQuestions = examDto.Questions
                    .GroupBy(q => q.QuestionId)
                    .Select(g => g.First())
                    .ToList();

                Console.WriteLine($"📝 Processing {uniqueQuestions.Count} unique questions for exam {exam.ExamId}");

                // Since this is a new exam, no existing ExamQuestions to check
                // Just verify questions exist in database and add them
                var questionIds = uniqueQuestions.Select(q => q.QuestionId).ToList();
                var existingQuestionIds = await _context.Questions
                    .Where(q => questionIds.Contains(q.QuestionId))
                    .Select(q => q.QuestionId)
                    .ToListAsync();

                var validQuestions = uniqueQuestions
                    .Where(q => existingQuestionIds.Contains(q.QuestionId))
                    .ToList();
                foreach (var questionDto in validQuestions)
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

                var missingQuestions = uniqueQuestions
                    .Where(q => !existingQuestionIds.Contains(q.QuestionId))
                    .Select(q => q.QuestionId)
                    .ToList();

                if (missingQuestions.Any())
                {
                    Console.WriteLine($"⚠️ Questions not found in database: {string.Join(", ", missingQuestions)}");
                }
            }

            await _context.SaveChangesAsync();

            // Create notification for exam creation
            await _notificationService.CreateExamNotificationAsync(
                exam.ExamName,
                "exam_created",
                new { examId = exam.ExamId, examType = exam.ExamType }
            );

            return Ok(new
            {
                success = true,
                message = $"Exam '{exam.ExamName}' created successfully",
                data = new
                {
                    examId = exam.ExamId,
                    examName = exam.ExamName,
                    description = exam.Description,
                    durationMinutes = exam.DurationMinutes,
                    examType = exam.ExamType,
                    createdAt = exam.CreatedAt,
                    isPublished = exam.IsPublished
                }
            });
        }

        // PUT: exams/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExam(string id, [FromBody] ExamUpdateDto examDto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            // Validate ExamType to ensure it matches database constraint
            var examType = ValidateExamType(examDto.ExamType, exam.ExamType);

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
                    return BadRequest(new
                    {
                        error = "Some questions do not exist",
                        missingQuestionIds = missingQuestionIds
                    });
                }
            }

            exam.ExamName = examDto.ExamName;
            exam.Description = examDto.Description;
            exam.DurationMinutes = examDto.DurationMinutes > 0 ? examDto.DurationMinutes : 45; // ✅ Default 45 phút nếu <= 0
            exam.ExamType = examType; // ✅ Sử dụng validated ExamType
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

            return Ok(new
            {
                success = true,
                message = $"Exam '{exam.ExamName}' updated successfully",
                data = new
                {
                    examId = exam.ExamId,
                    examName = exam.ExamName,
                    description = exam.Description,
                    durationMinutes = exam.DurationMinutes,
                    examType = exam.ExamType,
                    isPublished = exam.IsPublished
                }
            });
        }

        // DELETE: exams/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExam(string id)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(id);
                if (exam == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Exam with ID {id} not found"
                    });
                }

                // Remove related exam questions first
                var examQuestions = await _context.ExamQuestions
                    .Where(eq => eq.ExamId == id)
                    .ToListAsync();
                _context.ExamQuestions.RemoveRange(examQuestions);

                _context.Exams.Remove(exam);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Exam '{exam.ExamName}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deleting exam",
                    error = ex.Message
                });
            }
        }

        // POST: exams/generate
        [HttpPost("generations")]
        public async Task<IActionResult> GenerateExam([FromBody] ExamGenerateDto generateDto)
        {
            // Validate ExamType to ensure it matches database constraint
            var examType = ValidateExamType(generateDto.ExamType);

            // Get or create default user if CreatedBy is not provided
            var createdBy = !string.IsNullOrEmpty(generateDto.CreatedBy) ? generateDto.CreatedBy : GetOrCreateDefaultUser();

            var exam = new Exam
            {
                ExamId = Guid.NewGuid().ToString(),
                ExamName = generateDto.ExamName,
                Description = generateDto.Description,
                DurationMinutes = generateDto.DurationMinutes > 0 ? generateDto.DurationMinutes : 45, // ✅ Default 45 phút nếu <= 0
                ExamType = examType,
                CreatedBy = createdBy, // ✅ Sử dụng validated CreatedBy
                IsPublished = false,
                IsAiGenerated = examType == "ai_generated" || examType == "smart_exam",
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(exam);

            // Generate questions based on different input formats
            var questions = new List<ExamQuestion>();
            int order = 1;

            // ✅ Handle multi-chapter format (NEW)
            if (generateDto.IsMultiChapter && generateDto.ChapterAllocations != null && generateDto.ChapterAllocations.Any())
            {
                foreach (var allocation in generateDto.ChapterAllocations)
                {
                    var chapter = await _context.Chapters.FindAsync(allocation.ChapterId);
                    if (chapter != null)
                    {
                        // Try to get questions from database first
                        var dbQuestions = await _context.Questions
                            .Where(q => q.ChapterId == allocation.ChapterId && q.IsActive)
                            .OrderBy(x => Guid.NewGuid()) // Random order
                            .Take(allocation.QuestionCount)
                            .ToListAsync();

                        foreach (var question in dbQuestions)
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

                        // If not enough questions from DB, create placeholder questions
                        int remaining = allocation.QuestionCount - dbQuestions.Count;
                        if (remaining > 0)
                        {
                            var placeholderQuestions = new List<Question>();

                            // Calculate how many of each type to create
                            int multipleChoiceCount = 0;
                            int essayCount = 0;

                            if (generateDto.IncludeMultipleChoice && generateDto.IncludeEssay)
                            {
                                decimal mcPercentage = generateDto.CustomRatio ?
                                    (decimal)generateDto.MultipleChoicePercentage / 100 : 0.7m;
                                multipleChoiceCount = (int)(remaining * mcPercentage);
                                essayCount = remaining - multipleChoiceCount;
                            }
                            else if (generateDto.IncludeMultipleChoice)
                            {
                                multipleChoiceCount = remaining;
                            }
                            else if (generateDto.IncludeEssay)
                            {
                                essayCount = remaining;
                            }
                            else
                            {
                                multipleChoiceCount = remaining;
                            }

                            int questionCounter = 1;

                            // Create multiple choice questions
                            for (int i = 0; i < multipleChoiceCount; i++)
                            {
                                var placeholderQuestion = new Question
                                {
                                    QuestionId = Guid.NewGuid().ToString(),
                                    QuestionText = $"[AI Generated Multi-Chapter Question {questionCounter}] - {chapter.ChapterName}",
                                    QuestionType = "multiple_choice",
                                    DifficultyLevel = allocation.DifficultyLevel,
                                    CreatedBy = createdBy,
                                    CreatedAt = DateTime.UtcNow,
                                    TopicId = "TOPIC_001",
                                    ChapterId = allocation.ChapterId,
                                    IsActive = true,
                                    AiGenerated = true
                                };

                                _context.Questions.Add(placeholderQuestion);
                                placeholderQuestions.Add(placeholderQuestion);
                                questionCounter++;
                            }

                            // Create essay questions
                            for (int i = 0; i < essayCount; i++)
                            {
                                var placeholderQuestion = new Question
                                {
                                    QuestionId = Guid.NewGuid().ToString(),
                                    QuestionText = $"[AI Generated Essay Question {questionCounter}] - {chapter.ChapterName}",
                                    QuestionType = "essay",
                                    DifficultyLevel = allocation.DifficultyLevel,
                                    CreatedBy = createdBy,
                                    CreatedAt = DateTime.UtcNow,
                                    TopicId = "TOPIC_001",
                                    ChapterId = allocation.ChapterId,
                                    IsActive = true,
                                    AiGenerated = true,
                                    AiGenerationMetadata = JsonSerializer.Serialize(new
                                    {
                                        questionType = "essay",
                                        minWords = 50,
                                        maxWords = 300,
                                        essayStyle = "analytical",
                                        generatedAt = DateTime.UtcNow
                                    })
                                };

                                _context.Questions.Add(placeholderQuestion);
                                placeholderQuestions.Add(placeholderQuestion);
                                questionCounter++;
                            }

                            // Save placeholder questions to database first
                            await _context.SaveChangesAsync();

                            // Now create ExamQuestions with valid QuestionIds
                            foreach (var placeholderQuestion in placeholderQuestions)
                            {
                                questions.Add(new ExamQuestion
                                {
                                    ExamQuestionId = Guid.NewGuid().ToString(),
                                    ExamId = exam.ExamId,
                                    QuestionId = placeholderQuestion.QuestionId,
                                    QuestionOrder = order++,
                                    PointsWeight = 1,
                                    AddedAt = DateTime.UtcNow
                                });
                            }
                        }
                    }
                }
            }
            // ✅ Handle single chapter + questionCount format (from frontend)  
            else if (generateDto.ChapterId.HasValue && generateDto.QuestionCount.HasValue)
            {
                var chapter = await _context.Chapters.FindAsync(generateDto.ChapterId.Value);
                if (chapter != null)
                {
                    // Try to get questions from database first
                    var dbQuestions = await _context.Questions
                        .Where(q => q.ChapterId == generateDto.ChapterId.Value && q.IsActive)
                        .OrderBy(x => Guid.NewGuid()) // Random order
                        .Take(generateDto.QuestionCount.Value)
                        .ToListAsync();

                    foreach (var question in dbQuestions)
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

                    // If not enough questions from DB, create placeholder questions
                    int remaining = generateDto.QuestionCount.Value - dbQuestions.Count;
                    if (remaining > 0)
                    {
                        // ✅ Create placeholder questions in DB first, then add to exam
                        var placeholderQuestions = new List<Question>();

                        // Calculate how many of each type to create
                        int multipleChoiceCount = 0;
                        int essayCount = 0;

                        if (generateDto.IncludeMultipleChoice && generateDto.IncludeEssay)
                        {
                            // Use custom ratio if specified, otherwise default 70% multiple choice
                            decimal mcPercentage = generateDto.CustomRatio ?
                                (decimal)generateDto.MultipleChoicePercentage / 100 : 0.7m;
                            multipleChoiceCount = (int)(remaining * mcPercentage);
                            essayCount = remaining - multipleChoiceCount;

                            Console.WriteLine($"📊 Custom ratio: MC={generateDto.MultipleChoicePercentage}%, Essay={100 - generateDto.MultipleChoicePercentage}%");
                        }
                        else if (generateDto.IncludeMultipleChoice)
                        {
                            multipleChoiceCount = remaining;
                        }
                        else if (generateDto.IncludeEssay)
                        {
                            essayCount = remaining;
                        }
                        else
                        {
                            // Default fallback - create multiple choice
                            multipleChoiceCount = remaining;
                        }

                        int questionCounter = 1;

                        // Create multiple choice questions
                        for (int i = 0; i < multipleChoiceCount; i++)
                        {
                            var placeholderQuestion = new Question
                            {
                                QuestionId = Guid.NewGuid().ToString(),
                                QuestionText = $"[AI Generated Multiple Choice Question {questionCounter}] - {chapter.ChapterName}",
                                QuestionType = "multiple_choice",
                                DifficultyLevel = generateDto.DifficultyLevel ?? "medium",
                                CreatedBy = createdBy,
                                CreatedAt = DateTime.UtcNow,
                                TopicId = "TOPIC_001", // Default topic
                                ChapterId = generateDto.ChapterId.Value,
                                IsActive = true,
                                AiGenerated = true
                            };

                            _context.Questions.Add(placeholderQuestion);
                            placeholderQuestions.Add(placeholderQuestion);
                            questionCounter++;
                        }

                        // Create essay questions
                        for (int i = 0; i < essayCount; i++)
                        {
                            var placeholderQuestion = new Question
                            {
                                QuestionId = Guid.NewGuid().ToString(),
                                QuestionText = $"[AI Generated Essay Question {questionCounter}] - {chapter.ChapterName}",
                                QuestionType = "essay",
                                DifficultyLevel = generateDto.DifficultyLevel ?? "medium",
                                CreatedBy = createdBy,
                                CreatedAt = DateTime.UtcNow,
                                TopicId = "TOPIC_001", // Default topic
                                ChapterId = generateDto.ChapterId.Value,
                                IsActive = true,
                                AiGenerated = true,
                                // Add essay-specific metadata
                                AiGenerationMetadata = JsonSerializer.Serialize(new
                                {
                                    questionType = "essay",
                                    minWords = 50,
                                    maxWords = 300,
                                    essayStyle = "analytical",
                                    generatedAt = DateTime.UtcNow
                                })
                            };

                            _context.Questions.Add(placeholderQuestion);
                            placeholderQuestions.Add(placeholderQuestion);
                            questionCounter++;
                        }

                        // ✅ Save placeholder questions to database first
                        await _context.SaveChangesAsync();

                        // ✅ Now create ExamQuestions with valid QuestionIds
                        foreach (var placeholderQuestion in placeholderQuestions)
                        {
                            questions.Add(new ExamQuestion
                            {
                                ExamQuestionId = Guid.NewGuid().ToString(),
                                ExamId = exam.ExamId,
                                QuestionId = placeholderQuestion.QuestionId, // ✅ Now exists in DB
                                QuestionOrder = order++,
                                PointsWeight = 1,
                                AddedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
            // ✅ OLD: Handle Matrix format (legacy)
            else if (generateDto.Matrix != null && generateDto.Matrix.Any())
            {
                foreach (var matrix in generateDto.Matrix)
                {
                    // Get questions from database based on difficulty and topic
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

                    // Add medium and hard questions...
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
            }

            _context.ExamQuestions.AddRange(questions);
            await _context.SaveChangesAsync();

            // Create notification for AI exam generation
            await _notificationService.CreateExamNotificationAsync(
                exam.ExamName,
                "exam_created",
                new { examId = exam.ExamId, examType = exam.ExamType, questionCount = questions.Count, isAiGenerated = true }
            );

            return Ok(new
            {
                success = true,
                message = $"AI exam '{exam.ExamName}' generated successfully with {questions.Count} questions",
                data = new
                {
                    examId = exam.ExamId,
                    examName = exam.ExamName,
                    description = exam.Description,
                    durationMinutes = exam.DurationMinutes,
                    examType = exam.ExamType,
                    createdAt = exam.CreatedAt,
                    questionCount = questions.Count,
                    questions = questions.Select(q => new
                    {
                        questionId = q.QuestionId,
                        questionOrder = q.QuestionOrder,
                        pointsWeight = q.PointsWeight
                    }).ToList()
                }
            });
        }

        private T GetEssayMetadataProperty<T>(string? jsonMetadata, string propertyName)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonMetadata))
                    return default(T);

                var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonMetadata);
                if (metadata != null && metadata.ContainsKey(propertyName))
                {
                    var element = metadata[propertyName];
                    return JsonSerializer.Deserialize<T>(element.GetRawText());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing essay metadata: {ex.Message}");
            }
            return default(T);
        }

        private string GetOrCreateDefaultUser()
        {
            // ✅ LUÔN SỬ DỤNG ai_system - Đơn giản và đáng tin cậy
            return "ai_system";
        }

        // GET: exams/history/{userId}
        [Authorize]
        [HttpGet("histories/{userId}")]
        public async Task<IActionResult> GetUserExamHistory(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.StudentAttempts
                    .Include(a => a.Exam)
                    .Include(a => a.StudentAnswers)
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.StartTime)
                    .AsNoTracking();

                // Get total count
                var totalCount = await query.CountAsync();

                var attempts = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new
                    {
                        id = a.AttemptId,
                        score = a.TotalScore,
                        total = a.MaxScore ?? 10,
                        subject = a.Exam.ExamName,
                        correct = a.StudentAnswers.Count(sa => sa.IsCorrect),
                        totalQuestions = a.StudentAnswers.Count,
                        time = a.EndTime.HasValue
                            ? $"{(a.EndTime.Value - a.StartTime).TotalMinutes:0} phút"
                            : "Đang làm bài",
                        date = a.StartTime.ToString("HH:mm dd/MM/yyyy"),
                        difficulty = a.Exam.ExamType == "15p" ? "Dễ" :
                                   a.Exam.ExamType == "1tiet" ? "Trung bình" : "Khó",
                        accuracy = a.StudentAnswers.Any()
                            ? (decimal)a.StudentAnswers.Count(sa => sa.IsCorrect) / a.StudentAnswers.Count * 100
                            : 0
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "User exam history retrieved successfully",
                    data = attempts,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Internal server error",
                    error = ex.Message,
                    details = ex.InnerException?.Message
                });
            }
        }
    }
}