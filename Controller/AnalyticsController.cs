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

        // GET: analytics/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<object>>> GetDashboard()
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
                    .Select(e => new
                    {
                        id = e.ExamId,
                        type = "exam_created",
                        title = e.ExamName,
                        description = $"Đề thi {e.ExamName} đã được tạo",
                        createdAt = e.CreatedAt,
                        createdBy = e.CreatedBy
                    })
                    .ToListAsync();

                var dashboardData = new
                {
                    totalExams = totalExams,
                    totalUsers = totalUsers,
                    totalQuestions = totalQuestions,
                    totalChapters = totalChapters,
                    recentActivities = recentExams
                };

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Dashboard data retrieved successfully",
                    Data = dashboardData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message,
                    Data = null
                });
            }
        }

        // GET: analytics/activities
        [HttpGet("recent")]
        public async Task<ActionResult<ApiResponse<List<object>>>> GetRecentActivities([FromQuery] int limit = 10)
        {
            try
            {
                var activities = new List<object>();

                // Get recent exams
                var recentExams = await _context.Exams
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit / 2)
                    .Select(e => new
                    {
                        id = e.ExamId,
                        type = "exam_created",
                        title = $"Đề thi '{e.ExamName}' đã được tạo",
                        description = $"Đã tạo đề thi {e.ExamName}",
                        createdAt = e.CreatedAt,
                        createdBy = e.CreatedBy,
                        icon = "rocket"
                    })
                    .ToListAsync();

                activities.AddRange(recentExams);

                // Get recent questions
                var recentQuestions = await _context.Questions
                    .OrderByDescending(q => q.CreatedAt)
                    .Take(limit / 2)
                    .Select(q => new
                    {
                        id = q.QuestionId,
                        type = "question_created",
                        title = "Câu hỏi mới được thêm",
                        description = $"Đã tạo câu hỏi: {q.QuestionText.Substring(0, Math.Min(50, q.QuestionText.Length))}...",
                        createdAt = q.CreatedAt,
                        createdBy = q.CreatedBy,
                        icon = "question"
                    })
                    .ToListAsync();

                activities.AddRange(recentQuestions);

                // Sort by creation time and take limit
                var sortedActivities = activities.OrderByDescending(a => ((dynamic)a).createdAt).Take(limit).ToList();

                return Ok(new ApiResponse<List<object>>
                {
                    Success = true,
                    Message = "Recent activities retrieved successfully",
                    Data = sortedActivities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, new ApiResponse<List<object>>
                {
                    Success = false,
                    Message = "Internal server error: " + ex.Message,
                    Data = null
                });
            }
        }

        // GET: analytics/chart-data
        [HttpGet("chart-data")]
        public async Task<IActionResult> GetChartData([FromQuery] string period = "7days")
        {
            try
            {
                var days = period switch
                {
                    "7days" => 7,
                    "30days" => 30,
                    "90days" => 90,
                    _ => 7
                };

                var startDate = DateTime.UtcNow.AddDays(-days);
                var chartData = new List<object>();

                for (int i = days - 1; i >= 0; i--)
                {
                    var date = DateTime.UtcNow.AddDays(-i);
                    var dayStart = date.Date;
                    var dayEnd = dayStart.AddDays(1);

                    var usersCount = await _context.Users
                        .Where(u => u.CreatedAt >= dayStart && u.CreatedAt < dayEnd)
                        .CountAsync();

                    var questionsCount = await _context.Questions
                        .Where(q => q.CreatedAt >= dayStart && q.CreatedAt < dayEnd)
                        .CountAsync();

                    var examsCount = await _context.Exams
                        .Where(e => e.CreatedAt >= dayStart && e.CreatedAt < dayEnd)
                        .CountAsync();

                    chartData.Add(new
                    {
                        name = date.ToString("dd/MM"),
                        users = usersCount,
                        questions = questionsCount,
                        exams = examsCount
                    });
                }

                return Ok(chartData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data");
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message
                });
            }
        }

        // GET: analytics/recent-attempts
        [HttpGet("attempts/recent")]
        public async Task<IActionResult> GetRecentAttempts([FromQuery] int limit = 10)
        {
            try
            {
                var attempts = await _context.StudentAttempts
                    .Include(sa => sa.User)
                    .Include(sa => sa.Exam)
                    .OrderByDescending(sa => sa.CreatedAt)
                    .Take(limit)
                    .Select(sa => new
                    {
                        attemptId = sa.AttemptId,
                        userName = sa.User.FullName ?? sa.User.Username,
                        examName = sa.Exam.ExamName,
                        score = sa.TotalScore,
                        maxScore = sa.MaxScore ?? 10,
                        status = sa.Status,
                        createdAt = sa.CreatedAt
                    })
                    .ToListAsync();

                return Ok(attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent attempts");
                return StatusCode(500, new
                {
                    error = "Internal server error",
                    message = ex.Message
                });
            }
        }

        // GET: analytics/exams
        [HttpGet("exam-statistics")]
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

        [HttpGet("chapters")]
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
        [HttpGet("sample-exams")]
        public async Task<ActionResult<ApiResponse<List<SampleExamDto>>>> GetSampleExams(
            [FromQuery] int? grade = null,
            [FromQuery] string? chapterId = null,
            [FromQuery] string? difficulty = null,
            [FromQuery] int limit = 10)
        {
            try
            {
                var query = _context.Exams.AsQueryable();
                
                if (grade.HasValue || !string.IsNullOrEmpty(chapterId) || !string.IsNullOrEmpty(difficulty))
                {
                    query = query.Where(e => _context.ExamQuestions.Any(eq => eq.ExamId == e.ExamId));
                    _logger.LogInformation($"After filter count: {await query.CountAsync()}");
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
                        Difficulty = difficulty ?? "medium", 
                        Grade = grade ?? 10, 
                        Subject = "Vật lý",
                        CreatedAt = e.CreatedAt,
                        IsPopular = _context.ExamQuestions.Count(eq => eq.ExamId == e.ExamId) >= 10
                    })
                    .ToListAsync();

                _logger.LogInformation($"Final sample exams count: {sampleExams.Count}");

                return Ok(new ApiResponse<List<SampleExamDto>>
                {
                    Success = true,
                    Message = $"Retrieved {sampleExams.Count} sample exams with filters: grade={grade}, chapterId={chapterId}, difficulty={difficulty}",
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
    }
}