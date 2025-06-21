namespace BE_Phygens.Dto
{
    public class GenerateQuestionRequest
    {
        public int ChapterId { get; set; }
        public string TopicId { get; set; } = "";
        public string DifficultyLevel { get; set; } = "medium"; // easy, medium, hard
        public string QuestionType { get; set; } = "multiple_choice";
    }

    public class GenerateBulkRequest
    {
        public List<BulkQuestionItem> Questions { get; set; } = new();
    }

    public class BulkQuestionItem
    {
        public int ChapterId { get; set; }
        public string DifficultyLevel { get; set; } = "medium";
        public string QuestionType { get; set; } = "multiple_choice";
        public int Count { get; set; } = 1;
    }

    public class AIQuestionResponse
    {
        public string QuestionText { get; set; } = "";
        public List<AIChoice> Choices { get; set; } = new();
        public string? Explanation { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class AIChoice
    {
        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }
    }

    public class OpenAIResponse
    {
        public List<OpenAIChoice>? choices { get; set; }
    }

    public class OpenAIChoice
    {
        public OpenAIMessage? message { get; set; }
    }

    public class OpenAIMessage
    {
        public string? content { get; set; }
    }

    public class QuestionDto
    {
        public string QuestionId { get; set; } = "";
        public string Topic { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string CreatedBy { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<AnswerChoiceDto> AnswerChoices { get; set; } = new();
    }

    public class AnswerChoiceDto
    {
        public string ChoiceId { get; set; } = "";
        public string ChoiceLabel { get; set; } = "";
        public string ChoiceText { get; set; } = "";
    }

    // Smart Exam DTOs
    public class CreateExamMatrixRequest
    {
        public string ExamName { get; set; } = "";
        public string ExamType { get; set; } = "1tiet";
        public int Grade { get; set; } = 10;
        public int Duration { get; set; } = 45;
        public decimal TotalPoints { get; set; } = 10;
        public string? Description { get; set; }
        public List<ChapterDetailDto> ChapterDetails { get; set; } = new();
    }

    public class ChapterDetailDto
    {
        public int ChapterId { get; set; }
        public int QuestionCount { get; set; }
        public string DifficultyLevel { get; set; } = "medium";
    }

    public class GeneratedExamDto
    {
        public string ExamId { get; set; } = "";
        public string ExamName { get; set; } = "";
        public string Description { get; set; } = "";
        public int Duration { get; set; }
        public string ExamType { get; set; } = "";
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public List<ExamQuestionDto> Questions { get; set; } = new();
    }

    public class ExamQuestionDto
    {
        public string ExamQuestionId { get; set; } = "";
        public string QuestionId { get; set; } = "";
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
        public QuestionDto Question { get; set; } = new();
    }

    public class ExamMatrixListDto
    {
        public string MatrixId { get; set; } = "";
        public string ExamName { get; set; } = "";
        public string ExamType { get; set; } = "";
        public int Grade { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
    }

    public class ExamTemplateDto
    {
        public string TemplateName { get; set; } = "";
        public string ExamType { get; set; } = "";
        public int Grade { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public ChapterDetailDto[] ChapterDetails { get; set; } = Array.Empty<ChapterDetailDto>();
    }

    // Analytics DTOs
    public class DashboardDto
    {
        public int TotalUsers { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalExams { get; set; }
        public int TotalAttempts { get; set; }
        public List<RecentAttemptDto> RecentAttempts { get; set; } = new();
    }

    public class RecentAttemptDto
    {
        public string AttemptId { get; set; } = "";
        public string UserName { get; set; } = "";
        public string ExamName { get; set; } = "";
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class StudentProgressDto
    {
        public string StudentName { get; set; } = "";
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public List<ChapterProgressDto> ChapterProgress { get; set; } = new();
        public List<ExamHistoryDto> ExamHistory { get; set; } = new();
    }

    public class ChapterProgressDto
    {
        public string ChapterName { get; set; } = "";
        public int Attempts { get; set; }
        public decimal AvgScore { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ExamHistoryDto
    {
        public string ExamName { get; set; } = "";
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class ExamStatisticsDto
    {
        public string ExamName { get; set; } = "";
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public double PassRate { get; set; }
        public List<QuestionStatDto> QuestionStatistics { get; set; } = new();
        public List<ScoreRangeDto> ScoreDistribution { get; set; } = new();
    }

    public class QuestionStatDto
    {
        public string QuestionId { get; set; } = "";
        public string QuestionText { get; set; } = "";
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public double CorrectRate { get; set; }
    }

    public class ScoreRangeDto
    {
        public string Range { get; set; } = "";
        public int Count { get; set; }
    }

    public class ChapterAnalyticsDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = "";
        public int Grade { get; set; }
        public int TotalQuestions { get; set; }
        public int EasyQuestions { get; set; }
        public int MediumQuestions { get; set; }
        public int HardQuestions { get; set; }
        public double CoveragePercentage { get; set; }
    }
} 