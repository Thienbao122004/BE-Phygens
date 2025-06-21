using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BE_Phygens.Models;
using BE_Phygens.Dto;

namespace BE_Phygens.Controllers
{
    [Route("api/analytics")]
    [ApiController]
    [Authorize]
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
        public async Task<ActionResult<ApiResponse<DashboardDto>>> GetDashboard()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var totalQuestions = await _context.Questions.CountAsync();
                var totalExams = await _context.Exams.CountAsync();
                var totalAttempts = await _context.StudentAttempts.CountAsync();

                var recentAttempts = await _context.StudentAttempts
                    .Include(sa => sa.User)
                    .Include(sa => sa.Exam)
                    .OrderByDescending(sa => sa.CreatedAt)
                    .Take(10)
                    .Select(sa => new RecentAttemptDto
                    {
                        AttemptId = sa.AttemptId,
                        UserName = sa.User.FullName,
                        ExamName = sa.Exam.ExamName,
                        Score = sa.TotalScore,
                        MaxScore = sa.MaxScore ?? 10,
                        Status = sa.Status,
                        CreatedAt = sa.CreatedAt
                    })
                    .ToListAsync();

                var dashboard = new DashboardDto
                {
                    TotalUsers = totalUsers,
                    TotalQuestions = totalQuestions,
                    TotalExams = totalExams,
                    TotalAttempts = totalAttempts,
                    RecentAttempts = recentAttempts
                };

                return Ok(new ApiResponse<DashboardDto>
                {
                    Success = true,
                    Message = "Lấy dashboard thành công",
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard");
                return StatusCode(500, new ApiResponse<DashboardDto>
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
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