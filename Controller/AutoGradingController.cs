using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BE_Phygens.Services;
using BE_Phygens.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BE_Phygens.Controller
{
    [ApiController]
    [Route("auto-grading")]
    [Authorize]
    public class AutoGradingController : ControllerBase
    {
        private readonly IAutoGradingService _gradingService;
        private readonly ILogger<AutoGradingController> _logger;

        public AutoGradingController(IAutoGradingService gradingService, ILogger<AutoGradingController> logger)
        {
            _gradingService = gradingService;
            _logger = logger;
        }

        /// <summary>
        /// POST: auto-grading/questions/grade - Chấm điểm một câu hỏi đơn lẻ
        /// </summary>
        [HttpPost("questions/grade")]
        public async Task<IActionResult> GradeSingleQuestion([FromBody] GradeQuestionRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.QuestionId) || string.IsNullOrEmpty(request.StudentChoiceId))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId và StudentChoiceId là bắt buộc" });
                }

                var result = await _gradingService.GradeSingleQuestionAsync(
                    request.QuestionId, 
                    request.StudentChoiceId, 
                    request.StudentUserId
                );

                return Ok(new
                {
                    questionId = result.QuestionId,
                    correctChoiceId = result.CorrectChoiceId,
                    correctChoiceLabel = result.CorrectChoiceLabel,
                    correctChoiceText = result.CorrectChoiceText,
                    studentChoiceId = result.StudentChoiceId,
                    studentChoiceLabel = result.StudentChoiceLabel,
                    studentChoiceText = result.StudentChoiceText,
                    isCorrect = result.IsCorrect,
                    pointsEarned = result.PointsEarned,
                    maxPoints = result.MaxPoints,
                    feedback = result.Feedback,
                    explanation = result.Explanation,
                    difficultyLevel = result.DifficultyLevel,
                    questionType = result.QuestionType,
                    gradedAt = result.GradedAt
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm câu hỏi đơn lẻ");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi chấm điểm" });
            }
        }

        /// <summary>
        /// POST: auto-grading/exams/grade - Chấm điểm toàn bộ bài thi
        /// </summary>
        [HttpPost("exams/grade")]
        public async Task<IActionResult> GradeExam([FromBody] GradeExamRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ExamId) || string.IsNullOrEmpty(request.StudentUserId))
                {
                    return BadRequest(new { error = "validation_error", message = "ExamId và StudentUserId là bắt buộc" });
                }

                if (request.StudentAnswers == null || !request.StudentAnswers.Any())
                {
                    return BadRequest(new { error = "validation_error", message = "Phải có ít nhất một câu trả lời" });
                }

                var result = await _gradingService.GradeExamAsync(
                    request.ExamId, 
                    request.StudentAnswers, 
                    request.StudentUserId
                );

                return Ok(new
                {
                    examId = result.ExamId,
                    studentId = result.StudentId,
                    examName = result.ExamName,
                    examType = result.ExamType,
                    totalQuestions = result.TotalQuestions,
                    correctAnswers = result.CorrectAnswers,
                    incorrectAnswers = result.IncorrectAnswers,
                    totalPointsEarned = result.TotalPointsEarned,
                    maxPossiblePoints = result.MaxPossiblePoints,
                    percentageScore = result.PercentageScore,
                    grade = result.Grade,
                    timeTaken = result.TimeTaken?.ToString(@"hh\:mm\:ss"),
                    completedAt = result.CompletedAt,
                    difficultyBreakdown = result.DifficultyBreakdown,
                    topicAccuracy = result.TopicAccuracy,
                    questionResults = result.QuestionResults.Select(qr => new
                    {
                        questionId = qr.QuestionId,
                        isCorrect = qr.IsCorrect,
                        pointsEarned = qr.PointsEarned,
                        maxPoints = qr.MaxPoints,
                        feedback = qr.Feedback,
                        difficultyLevel = qr.DifficultyLevel,
                        questionType = qr.QuestionType
                    }),
                    analysis = new
                    {
                        performanceLevel = result.Analysis.PerformanceLevel,
                        recommendations = result.Analysis.Recommendations,
                        studyPlan = result.Analysis.StudyPlan,
                        topicBreakdown = result.Analysis.TopicBreakdown
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm bài thi");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi chấm bài thi" });
            }
        }

        /// <summary>
        /// POST: auto-grading/attempts/{attemptId}/grade - Chấm điểm dựa trên attempt có sẵn
        /// </summary>
        [HttpPost("attempts/{attemptId}/grade")]
        public async Task<IActionResult> GradeExamAttempt(string attemptId)
        {
            try
            {
                if (string.IsNullOrEmpty(attemptId))
                {
                    return BadRequest(new { error = "validation_error", message = "AttemptId là bắt buộc" });
                }

                var result = await _gradingService.GradeExamAttemptAsync(attemptId);

                return Ok(new
                {
                    examId = result.ExamId,
                    studentId = result.StudentId,
                    examName = result.ExamName,
                    totalQuestions = result.TotalQuestions,
                    correctAnswers = result.CorrectAnswers,
                    incorrectAnswers = result.IncorrectAnswers,
                    totalPointsEarned = result.TotalPointsEarned,
                    maxPossiblePoints = result.MaxPossiblePoints,
                    percentageScore = result.PercentageScore,
                    grade = result.Grade,
                    timeTaken = result.TimeTaken?.ToString(@"hh\:mm\:ss"),
                    completedAt = result.CompletedAt,
                    analysis = new
                    {
                        performanceLevel = result.Analysis.PerformanceLevel,
                        recommendations = result.Analysis.Recommendations
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm attempt");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi chấm attempt" });
            }
        }

        /// <summary>
        /// GET: auto-grading/questions/{questionId}/analytics - Lấy thống kê phân tích câu hỏi
        /// </summary>
        [HttpGet("questions/{questionId}/analytics")]
        public async Task<IActionResult> GetQuestionAnalytics(string questionId)
        {
            try
            {
                if (string.IsNullOrEmpty(questionId))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId là bắt buộc" });
                }

                var analytics = await _gradingService.GetQuestionAnalyticsAsync(questionId);

                return Ok(analytics.Select(a => new
                {
                    questionId = a.QuestionId,
                    questionText = a.QuestionText,
                    difficultyLevel = a.DifficultyLevel,
                    topicName = a.TopicName,
                    totalAttempts = a.TotalAttempts,
                    correctAttempts = a.CorrectAttempts,
                    successRate = a.SuccessRate,
                    choiceDistribution = a.ChoiceDistribution,
                    commonWrongChoices = a.CommonWrongChoices,
                    averageTimeSpent = a.AverageTimeSpent,
                    performanceLevel = a.PerformanceLevel,
                    improvementSuggestions = a.ImprovementSuggestions
                }));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê câu hỏi");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy thống kê câu hỏi" });
            }
        }

        /// <summary>
        /// GET: auto-grading/students/{studentId}/performance - Lấy báo cáo hiệu suất học sinh
        /// </summary>
        [HttpGet("students/{studentId}/performance")]
        public async Task<IActionResult> GetStudentPerformance(string studentId, [FromQuery] string? examId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(studentId))
                {
                    return BadRequest(new { error = "validation_error", message = "StudentId là bắt buộc" });
                }

                var performance = await _gradingService.GetStudentPerformanceAsync(studentId, examId);

                return Ok(new
                {
                    studentId = performance.StudentId,
                    studentName = performance.StudentName,
                    email = performance.Email,
                    totalQuestionsAttempted = performance.TotalQuestionsAttempted,
                    totalCorrectAnswers = performance.TotalCorrectAnswers,
                    overallAccuracy = performance.OverallAccuracy,
                    accuracyByDifficulty = performance.AccuracyByDifficulty,
                    accuracyByTopic = performance.AccuracyByTopic,
                    accuracyByQuestionType = performance.AccuracyByQuestionType,
                    weakAreas = performance.WeakAreas.Select(wa => new
                    {
                        topicId = wa.TopicId,
                        topicName = wa.TopicName,
                        accuracyRate = wa.AccuracyRate,
                        questionsAttempted = wa.QuestionsAttempted,
                        commonMistakes = wa.CommonMistakes,
                        recommendations = wa.Recommendations,
                        priority = wa.Priority
                    }),
                    strengths = performance.Strengths.Select(s => new
                    {
                        topicId = s.TopicId,
                        topicName = s.TopicName,
                        accuracyRate = s.AccuracyRate,
                        questionsAttempted = s.QuestionsAttempted,
                        consistencyLevel = s.ConsistencyLevel
                    }),
                    recentExams = performance.RecentExams.Select(re => new
                    {
                        examId = re.ExamId,
                        examName = re.ExamName,
                        examType = re.ExamType,
                        score = re.Score,
                        maxScore = re.MaxScore,
                        percentage = re.Percentage,
                        completedAt = re.CompletedAt,
                        timeTaken = re.TimeTaken.ToString(@"hh\:mm\:ss")
                    }),
                    learningTrend = new
                    {
                        trend = performance.LearningTrend.Trend,
                        progressRate = performance.LearningTrend.ProgressRate,
                        dataPoints = performance.LearningTrend.DataPoints.Select(dp => new
                        {
                            date = dp.Date,
                            accuracyRate = dp.AccuracyRate,
                            questionsAnswered = dp.QuestionsAnswered
                        })
                    },
                    lastActivity = performance.LastActivity
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy báo cáo hiệu suất học sinh");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy báo cáo hiệu suất" });
            }
        }

        /// <summary>
        /// GET: auto-grading/questions/{questionId}/mistakes - Lấy phân tích các lỗi thường gặp
        /// </summary>
        [HttpGet("questions/{questionId}/mistakes")]
        public async Task<IActionResult> GetCommonMistakes(string questionId)
        {
            try
            {
                if (string.IsNullOrEmpty(questionId))
                {
                    return BadRequest(new { error = "validation_error", message = "QuestionId là bắt buộc" });
                }

                var mistakes = await _gradingService.GetCommonMistakesAsync(questionId);

                return Ok(mistakes.Select(m => new
                {
                    questionId = m.QuestionId,
                    wrongChoiceId = m.WrongChoiceId,
                    wrongChoiceText = m.WrongChoiceText,
                    timesSelected = m.TimesSelected,
                    selectionRate = m.SelectionRate,
                    reasonForMistake = m.ReasonForMistake,
                    correction = m.Correction,
                    studyTips = m.StudyTips
                }));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy phân tích lỗi thường gặp");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy phân tích lỗi" });
            }
        }

        /// <summary>
        /// GET: auto-grading/exams/{examId}/statistics - Lấy thống kê bài thi
        /// </summary>
        [HttpGet("exams/{examId}/statistics")]
        public async Task<IActionResult> GetExamStatistics(string examId)
        {
            try
            {
                if (string.IsNullOrEmpty(examId))
                {
                    return BadRequest(new { error = "validation_error", message = "ExamId là bắt buộc" });
                }

                var statistics = await _gradingService.GetExamStatisticsAsync(examId);

                return Ok(new
                {
                    examId = statistics.ExamId,
                    examName = statistics.ExamName,
                    totalAttempts = statistics.TotalAttempts,
                    averageScore = statistics.AverageScore,
                    highestScore = statistics.HighestScore,
                    lowestScore = statistics.LowestScore,
                    averageCompletionTime = statistics.AverageCompletionTime.ToString(@"hh\:mm\:ss"),
                    gradeDistribution = statistics.GradeDistribution,
                    difficultyAnalysis = statistics.DifficultyAnalysis,
                    questionPerformances = statistics.QuestionPerformances.Select(qp => new
                    {
                        questionId = qp.QuestionId,
                        questionText = qp.QuestionText,
                        successRate = qp.SuccessRate,
                        difficultyLevel = qp.DifficultyLevel,
                        performanceRating = qp.PerformanceRating
                    })
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = "invalid_input", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thống kê bài thi");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi lấy thống kê bài thi" });
            }
        }

        /// <summary>
        /// POST: auto-grading/questions/batch-grade - Chấm điểm nhiều câu hỏi cùng lúc
        /// </summary>
        [HttpPost("questions/batch-grade")]
        public async Task<IActionResult> BatchGradeQuestions([FromBody] BatchGradeRequest request)
        {
            try
            {
                if (request.Questions == null || !request.Questions.Any())
                {
                    return BadRequest(new { error = "validation_error", message = "Phải có ít nhất một câu hỏi để chấm" });
                }

                var results = new List<object>();

                foreach (var question in request.Questions)
                {
                    try
                    {
                        var result = await _gradingService.GradeSingleQuestionAsync(
                            question.QuestionId,
                            question.StudentChoiceId,
                            request.StudentUserId
                        );

                        results.Add(new
                        {
                            questionId = result.QuestionId,
                            isCorrect = result.IsCorrect,
                            pointsEarned = result.PointsEarned,
                            maxPoints = result.MaxPoints,
                            feedback = result.Feedback,
                            difficultyLevel = result.DifficultyLevel
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Lỗi khi chấm câu hỏi {question.QuestionId}");
                        results.Add(new
                        {
                            questionId = question.QuestionId,
                            error = "grading_failed",
                            message = ex.Message
                        });
                    }
                }

                return Ok(new
                {
                    totalQuestions = request.Questions.Count,
                    successfullyGraded = results.Count(r => !((dynamic)r).ToString().Contains("error")),
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm điểm hàng loạt");
                return StatusCode(500, new { error = "internal_error", message = "Có lỗi xảy ra khi chấm điểm hàng loạt" });
            }
        }
    }

    // Request DTOs
    public class GradeQuestionRequest
    {
        public string QuestionId { get; set; } = string.Empty;
        public string StudentChoiceId { get; set; } = string.Empty;
        public string? StudentUserId { get; set; }
    }

    public class GradeExamRequest
    {
        public string ExamId { get; set; } = string.Empty;
        public string StudentUserId { get; set; } = string.Empty;
        public List<StudentAnswerSubmission> StudentAnswers { get; set; } = new();
        public TimeSpan? TimeTaken { get; set; }
    }

    public class BatchGradeRequest
    {
        public List<GradeQuestionRequest> Questions { get; set; } = new();
        public string? StudentUserId { get; set; }
    }
} 