using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;

namespace BE_Phygens.Controllers
{
    [Route("analytics")]
    [ApiController]
    [AllowAnonymous]
    public class AnalyticsController : ControllerBase
    {
        private readonly PhygensContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(PhygensContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<DashboardDataDto>>> GetDashboard()
        {
            try
            {
                // Get dashboard statistics
                var totalExams = await _context.Exams.CountAsync();
                var totalUsers = await _context.Users.CountAsync();
                var totalQuestions = await _context.Questions.CountAsync();
                var totalChapters = await _context.Chapters.CountAsync();

                // Get recent activities (last 30 days)
                var recentExams = await _context.Exams
                    .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(10)
                    .Select(e => new RecentActivityDto
                    {
                        Id = e.ExamId,
                        Type = "exam_created",
                        Title = e.ExamName,
                        Description = $"Đề thi {e.ExamName} đã được tạo",
                        CreatedAt = e.CreatedAt,
                        CreatedBy = e.CreatedBy
                    })
                    .ToListAsync();

                var dashboardData = new DashboardDataDto
                {
                    TotalExams = totalExams,
                    TotalUsers = totalUsers,
                    TotalQuestions = totalQuestions,
                    TotalChapters = totalChapters,
                    RecentActivities = recentExams
                };

                return Ok(new ApiResponse<DashboardDataDto>
                {
                    Success = true,
                    Message = "Dashboard data retrieved successfully",
                    Data = dashboardData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new ApiResponse<DashboardDataDto>
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                });
            }
        }

        [HttpGet("recent-activities")]
        public async Task<ActionResult<ApiResponse<List<RecentActivityDto>>>> GetRecentActivities([FromQuery] int limit = 10)
        {
            try
            {
                var activities = new List<RecentActivityDto>();

                // Get recent exams
                var recentExams = await _context.Exams
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit / 2)
                    .Select(e => new RecentActivityDto
                    {
                        Id = e.ExamId,
                        Type = "exam_created",
                        Title = e.ExamName,
                        Description = $"Đã tạo đề thi {e.ExamName}",
                        CreatedAt = e.CreatedAt,
                        CreatedBy = e.CreatedBy,
                        Icon = "rocket"
                    })
                    .ToListAsync();

                activities.AddRange(recentExams);

                // Get recent questions
                var recentQuestions = await _context.Questions
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(limit / 2)
                    .Select(q => new RecentActivityDto
                    {
                        Id = q.QuestionId,
                        Type = "question_created",
                        Title = "Câu hỏi mới",
                        Description = $"Đã tạo câu hỏi: {q.QuestionText.Substring(0, Math.Min(50, q.QuestionText.Length))}...",
                        CreatedAt = q.CreatedAt,
                        CreatedBy = q.CreatedBy,
                        Icon = "question"
                    })
                    .ToListAsync();

                activities.AddRange(recentQuestions);

                // Sort by creation time and take limit
                activities = activities.OrderByDescending(a => a.CreatedAt).Take(limit).ToList();

                return Ok(new ApiResponse<List<RecentActivityDto>>
                {
                    Success = true,
                    Message = $"Retrieved {activities.Count} recent activities",
                    Data = activities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, new ApiResponse<List<RecentActivityDto>>
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                });
            }
        }

        [HttpGet("filtered-exams")]
        public async Task<ActionResult<ApiResponse<List<ExamListDto>>>> GetFilteredExams(
            [FromQuery] int? grade = null,
            [FromQuery] string? topic = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] int limit = 20)
        {
            try
            {
                var query = _context.Exams.AsQueryable();

                // Apply filters based on parameters
                if (grade.HasValue)
                {
                    // Filter by exams that contain chapters of specific grade
                    query = query.Where(e => _context.ExamQuestions
                        .Include(eq => eq.Question)
                        .Where(eq => eq.ExamId == e.ExamId && eq.Question.ChapterId.HasValue)
                        .Join(_context.Chapters, 
                            eq => eq.Question.ChapterId, 
                            c => c.ChapterId, 
                            (eq, c) => c.Grade)
                        .Any(g => g == grade));
                }

                if (!string.IsNullOrEmpty(topic))
                {
                    query = query.Where(e => e.ExamName.Contains(topic) || e.Description.Contains(topic));
                }

                if (!string.IsNullOrEmpty(difficulty))
                {
                    // Filter by exam difficulty based on questions
                    query = query.Where(e => _context.ExamQuestions
                        .Include(eq => eq.Question)
                        .Where(eq => eq.ExamId == e.ExamId)
                        .Any(eq => eq.Question.DifficultyLevel == difficulty));
                }

                var exams = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit)
                    .Select(e => new ExamListDto
                    {
                        ExamId = e.ExamId,
                        ExamName = e.ExamName,
                        Description = e.Description,
                        ExamType = e.ExamType,
                        Duration = e.DurationMinutes ?? 45,
                        CreatedAt = e.CreatedAt,
                        CreatedBy = e.CreatedBy,
                        IsPublished = e.IsPublished
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<ExamListDto>>
                {
                    Success = true,
                    Message = $"Retrieved {exams.Count} filtered exams",
                    Data = exams
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered exams");
                return StatusCode(500, new ApiResponse<List<ExamListDto>>
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                });
            }
        }

        [HttpGet("exam-stats")]
        public async Task<ActionResult<ApiResponse<ExamStatsDto>>> GetExamStats()
        {
            try
            {
                var stats = new ExamStatsDto
                {
                    TotalExams = await _context.Exams.CountAsync(),
                    PublishedExams = await _context.Exams.CountAsync(e => e.IsPublished),
                    DraftExams = await _context.Exams.CountAsync(e => !e.IsPublished),
                    TotalQuestions = await _context.Questions.CountAsync(),
                    AiGeneratedQuestions = await _context.Questions.CountAsync(q => q.AiGenerated == true),
                    ExamsByType = await _context.Exams
                        .GroupBy(e => e.ExamType)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.Type, x => x.Count)
                };

                return Ok(new ApiResponse<ExamStatsDto>
                {
                    Success = true,
                    Message = "Exam statistics retrieved successfully",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam stats");
                return StatusCode(500, new ApiResponse<ExamStatsDto>
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                });
            }
        }

        [HttpGet("sample-exams")]
        public async Task<ActionResult<ApiResponse<List<SampleExamDto>>>> GetSampleExams(
            [FromQuery] int? grade = null,
            [FromQuery] string? subject = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var query = _context.Exams.AsQueryable();

                // Filter by published exams only for sample exams
                query = query.Where(e => e.IsPublished);

                // Apply grade filter
                if (grade.HasValue)
                {
                    query = query.Where(e => _context.ExamQuestions
                        .Include(eq => eq.Question)
                        .Where(eq => eq.ExamId == e.ExamId && eq.Question.ChapterId.HasValue)
                        .Join(_context.Chapters, 
                            eq => eq.Question.ChapterId, 
                            c => c.ChapterId, 
                            (eq, c) => c.Grade)
                        .Any(g => g == grade));
                }

                // Apply subject filter (search in exam name and description)
                if (!string.IsNullOrEmpty(subject))
                {
                    query = query.Where(e => e.ExamName.Contains(subject) || 
                                           e.Description.Contains(subject) ||
                                           e.ExamType.Contains(subject));
                }

                // Apply difficulty filter
                if (!string.IsNullOrEmpty(difficulty))
                {
                    query = query.Where(e => _context.ExamQuestions
                        .Include(eq => eq.Question)
                        .Where(eq => eq.ExamId == e.ExamId)
                        .Any(eq => eq.Question.DifficultyLevel == difficulty));
                }

                var sampleExams = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit)
                    .Select(e => new SampleExamDto
                    {
                        ExamId = e.ExamId,
                        ExamName = e.ExamName,
                        Description = e.Description,
                        ExamType = e.ExamType,
                        Duration = e.DurationMinutes ?? 45,
                        QuestionCount = _context.ExamQuestions.Count(eq => eq.ExamId == e.ExamId),
                        Difficulty = _context.ExamQuestions
                            .Include(eq => eq.Question)
                            .Where(eq => eq.ExamId == e.ExamId)
                            .Select(eq => eq.Question.DifficultyLevel)
                            .FirstOrDefault() ?? "medium",
                        Grade = _context.ExamQuestions
                            .Include(eq => eq.Question)
                            .Where(eq => eq.ExamId == e.ExamId && eq.Question.ChapterId.HasValue)
                            .Join(_context.Chapters, 
                                eq => eq.Question.ChapterId, 
                                c => c.ChapterId, 
                                (eq, c) => c.Grade)
                            .FirstOrDefault(),
                        Subject = "Vật lý", 
                        CreatedAt = e.CreatedAt,
                        IsPopular = _context.ExamQuestions.Count(eq => eq.ExamId == e.ExamId) >= 10
                    })
                    .ToListAsync();

                return Ok(new ApiResponse<List<SampleExamDto>>
                {
                    Success = true,
                    Message = $"Retrieved {sampleExams.Count} sample exams",
                    Data = sampleExams
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sample exams");
                return StatusCode(500, new ApiResponse<List<SampleExamDto>>
                {
                    Success = false,
                    Message = $"Server error: {ex.Message}"
                });
            }
        }

        private string GetSubjectFromExamName(string examName)
        {
            if (string.IsNullOrEmpty(examName)) return "Vật lý";
            
            examName = examName.ToLower();
            if (examName.Contains("cơ học") || examName.Contains("co hoc")) return "Cơ học";
            if (examName.Contains("điện học") || examName.Contains("dien hoc")) return "Điện học";
            if (examName.Contains("quang học") || examName.Contains("quang hoc")) return "Quang học";
            if (examName.Contains("nhiệt học") || examName.Contains("nhiet hoc")) return "Nhiệt học";
            if (examName.Contains("sóng") || examName.Contains("song")) return "Sóng";
            if (examName.Contains("hạt nhân") || examName.Contains("hat nhan")) return "Hạt nhân";
            
            return "Vật lý";
        }

        [HttpGet("student-progress/{userId}")]
        public async Task<ActionResult<ApiResponse<StudentProgressDto>>> GetStudentProgress(string userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new ApiResponse<StudentProgressDto>
                    {
                        Success = false,
                        Message = "Học sinh không tồn tại"
                    });

                var attempts = await _context.StudentAttempts
                    .Include(sa => sa.Exam)
                    .Where(sa => sa.UserId == userId && sa.Status == "completed")
                    .OrderBy(sa => sa.CreatedAt)
                    .ToListAsync();

                var progressByChapter = await _context.LearningProgresses
                    .Where(lp => lp.UserId == userId)
                    .Select(lp => new ChapterProgressDto
                    {
                        ChapterName = "Topic " + lp.TopicId, // Simplified
                        Attempts = lp.Attempts,
                        AvgScore = lp.AvgScore,
                        LastUpdated = lp.LastUpdated
                    })
                    .ToListAsync();

                var examHistory = attempts.Select(a => new ExamHistoryDto
                {
                    ExamName = a.Exam.ExamName,
                    Score = a.TotalScore,
                    MaxScore = a.MaxScore ?? 10,
                    Percentage = (a.MaxScore ?? 10) > 0 ? (a.TotalScore / (a.MaxScore ?? 10)) * 100 : 0,
                    CompletedAt = a.EndTime ?? a.CreatedAt
                }).ToList();

                var studentProgress = new StudentProgressDto
                {
                    StudentName = user.FullName,
                    TotalAttempts = attempts.Count,
                    AverageScore = attempts.Any() ? attempts.Average(a => a.TotalScore) : 0,
                    HighestScore = attempts.Any() ? attempts.Max(a => a.TotalScore) : 0,
                    ChapterProgress = progressByChapter,
                    ExamHistory = examHistory
                };

                return Ok(new ApiResponse<StudentProgressDto>
                {
                    Success = true,
                    Message = "Lấy tiến độ học tập thành công",
                    Data = studentProgress
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student progress");
                return StatusCode(500, new ApiResponse<StudentProgressDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("exam-statistics/{examId}")]
        public async Task<ActionResult<ApiResponse<ExamStatisticsDto>>> GetExamStatistics(string examId)
        {
            try
            {
                var exam = await _context.Exams.FindAsync(examId);
                if (exam == null)
                    return NotFound(new ApiResponse<ExamStatisticsDto>
                    {
                        Success = false,
                        Message = "Đề thi không tồn tại"
                    });

                var attempts = await _context.StudentAttempts
                    .Include(sa => sa.User)
                    .Where(sa => sa.ExamId == examId && sa.Status == "completed")
                    .ToListAsync();

                var questionStats = await _context.StudentAnswers
                    .Include(sa => sa.Question)
                    .Where(sa => attempts.Select(a => a.AttemptId).Contains(sa.AttemptId))
                    .GroupBy(sa => sa.QuestionId)
                    .Select(g => new QuestionStatDto
                    {
                        QuestionId = g.Key,
                        QuestionText = g.First().Question.QuestionText,
                        TotalAnswers = g.Count(),
                        CorrectAnswers = g.Count(sa => sa.IsCorrect),
                        CorrectRate = g.Count() > 0 ? (double)g.Count(sa => sa.IsCorrect) / g.Count() * 100 : 0
                    })
                    .ToListAsync();

                var statistics = new ExamStatisticsDto
                {
                    ExamName = exam.ExamName,
                    TotalAttempts = attempts.Count,
                    AverageScore = attempts.Any() ? attempts.Average(a => a.TotalScore) : 0,
                    HighestScore = attempts.Any() ? attempts.Max(a => a.TotalScore) : 0,
                    LowestScore = attempts.Any() ? attempts.Min(a => a.TotalScore) : 0,
                    PassRate = attempts.Count > 0 ? (double)attempts.Count(a => a.TotalScore >= 5) / attempts.Count * 100 : 0,
                    QuestionStatistics = questionStats,
                    ScoreDistribution = CalculateScoreDistribution(attempts)
                };

                return Ok(new ApiResponse<ExamStatisticsDto>
                {
                    Success = true,
                    Message = "Lấy thống kê đề thi thành công",
                    Data = statistics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting exam statistics");
                return StatusCode(500, new ApiResponse<ExamStatisticsDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        [HttpGet("chapter-analytics")]
        public async Task<ActionResult<ApiResponse<List<ChapterAnalyticsDto>>>> GetChapterAnalytics([FromQuery] int? grade = null)
        {
            try
            {
                var chaptersQuery = _context.Set<Chapter>().AsQueryable();

                if (grade.HasValue)
                    chaptersQuery = chaptersQuery.Where(c => c.Grade == grade.Value);

                var chapters = await chaptersQuery.ToListAsync();

                var analytics = new List<ChapterAnalyticsDto>();

                foreach (var chapter in chapters)
                {
                    var questionCount = await _context.Questions
                        .Where(q => q.TopicId == chapter.ChapterId.ToString())
                        .CountAsync();

                    var easyCount = await _context.Questions
                        .Where(q => q.TopicId == chapter.ChapterId.ToString() && q.DifficultyLevel == "easy")
                        .CountAsync();

                    var mediumCount = await _context.Questions
                        .Where(q => q.TopicId == chapter.ChapterId.ToString() && q.DifficultyLevel == "medium")
                        .CountAsync();

                    var hardCount = await _context.Questions
                        .Where(q => q.TopicId == chapter.ChapterId.ToString() && q.DifficultyLevel == "hard")
                        .CountAsync();

                    analytics.Add(new ChapterAnalyticsDto
                    {
                        ChapterId = chapter.ChapterId,
                        ChapterName = chapter.ChapterName,
                        Grade = chapter.Grade,
                        TotalQuestions = questionCount,
                        EasyQuestions = easyCount,
                        MediumQuestions = mediumCount,
                        HardQuestions = hardCount,
                        CoveragePercentage = CalculateCoveragePercentage(questionCount)
                    });
                }

                return Ok(new ApiResponse<List<ChapterAnalyticsDto>>
                {
                    Success = true,
                    Message = "Lấy phân tích chương thành công",
                    Data = analytics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chapter analytics");
                return StatusCode(500, new ApiResponse<List<ChapterAnalyticsDto>>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        private List<ScoreRangeDto> CalculateScoreDistribution(List<StudentAttempt> attempts)
        {
            if (!attempts.Any()) return new List<ScoreRangeDto>();

            var ranges = new[]
            {
                new ScoreRangeDto { Range = "0-2", Count = 0 },
                new ScoreRangeDto { Range = "2-4", Count = 0 },
                new ScoreRangeDto { Range = "4-6", Count = 0 },
                new ScoreRangeDto { Range = "6-8", Count = 0 },
                new ScoreRangeDto { Range = "8-10", Count = 0 }
            };

            foreach (var attempt in attempts)
            {
                var score = attempt.TotalScore;
                if (score < 2) ranges[0].Count++;
                else if (score < 4) ranges[1].Count++;
                else if (score < 6) ranges[2].Count++;
                else if (score < 8) ranges[3].Count++;
                else ranges[4].Count++;
            }

            return ranges.ToList();
        }

        private double CalculateCoveragePercentage(int questionCount)
        {
            // Assume minimum 20 questions per chapter for good coverage
            const int minQuestionsForGoodCoverage = 20;
            return Math.Min((double)questionCount / minQuestionsForGoodCoverage * 100, 100);
        }
    }
} 