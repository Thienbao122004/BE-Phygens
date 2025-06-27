using BE_Phygens.Models;

namespace BE_Phygens.Services
{
    public interface IAutoGradingService
    {
        Task<QuestionGradingResult> GradeSingleQuestionAsync(string questionId, string studentChoiceId, string? studentUserId);
        Task<DetailedFeedback> GetDetailedFeedbackAsync(string questionId, string studentChoiceId);
        Task<ExamGradingResult> GradeExamAsync(string examId, List<StudentAnswerSubmission> studentAnswers, string studentUserId);
        Task<ExamGradingResult> GradeExamAttemptAsync(string attemptId);
        Task<List<QuestionGradingResult>> BatchGradeQuestionsAsync(List<StudentAnswerSubmission> submissions, string? studentUserId);
        Task<List<QuestionAnalytics>> GetQuestionAnalyticsAsync(string questionId);
        Task<StudentPerformance> GetStudentPerformanceAsync(string studentId, string? examId);
        Task<List<CommonMistake>> GetCommonMistakesAsync(string questionId);
        Task<ExamStatistics> GetExamStatisticsAsync(string examId);
    }

    public class DetailedFeedback
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string StudentAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public string CommonMistakeWarning { get; set; } = string.Empty;
        public string StudyTip { get; set; } = string.Empty;
        public string RelatedTopics { get; set; } = string.Empty;
    }
} 