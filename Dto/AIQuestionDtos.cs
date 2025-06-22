using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BE_Phygens.Dto
{
    // Basic DTOs that were removed
    public class QuestionDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<AnswerChoiceDto> AnswerChoices { get; set; } = new();
    }

    public class AnswerChoiceDto
    {
        public string ChoiceId { get; set; } = string.Empty;
        public string ChoiceLabel { get; set; } = string.Empty;
        public string ChoiceText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    // Smart Exam DTOs that were removed
    public class CreateExamMatrixRequest
    {
        public string ExamName { get; set; } = string.Empty;
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
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public List<ExamQuestionDto> Questions { get; set; } = new();
    }
         
    public class ExamQuestionDto
    {
        public string ExamQuestionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
        public QuestionDto Question { get; set; } = new();
    }

    public class ExamMatrixListDto
    {
        public string MatrixId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int Grade { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ExamTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int Grade { get; set; }
        public int Duration { get; set; }
        public int TotalQuestions { get; set; }
        public decimal TotalPoints { get; set; }
        public ChapterDetailDto[] ChapterDetails { get; set; } = Array.Empty<ChapterDetailDto>();
    }

    // Analytics DTOs that were removed
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
        public string AttemptId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class StudentProgressDto
    {
        public string StudentName { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal HighestScore { get; set; }
        public List<ChapterProgressDto> ChapterProgress { get; set; } = new();
        public List<ExamHistoryDto> ExamHistory { get; set; } = new();
    }

    public class ChapterProgressDto
    {
        public string ChapterName { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public decimal AvgScore { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ExamHistoryDto
    {
        public string ExamName { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class ExamStatisticsDto
    {
        public string ExamName { get; set; } = string.Empty;
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
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public double CorrectRate { get; set; }
    }

    public class ScoreRangeDto
    {
        public string Range { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ChapterAnalyticsDto
    {
        public int ChapterId { get; set; }
        public string ChapterName { get; set; } = string.Empty;
        public int Grade { get; set; }
        public int TotalQuestions { get; set; }
        public int EasyQuestions { get; set; }
        public int MediumQuestions { get; set; }
        public int HardQuestions { get; set; }
        public double CoveragePercentage { get; set; }
    }

    // Request DTOs for AI Question Generation
    public class GenerateQuestionRequest
    {
        [Required]
        public int ChapterId { get; set; }
        
        [Required]
        public string DifficultyLevel { get; set; } = "medium"; // easy, medium, hard
        
        [Required]
        public string QuestionType { get; set; } = "multiple_choice"; // multiple_choice, true_false, calculation
        
        public string? SpecificTopic { get; set; }
        
        public bool SaveToDatabase { get; set; } = false;
        
        public string? AdditionalInstructions { get; set; }
        
        public bool IncludeExplanation { get; set; } = true;
        
        public bool IncludeImage { get; set; } = false;
    }

    public class BatchGenerateRequest
    {
        [Required]
        public List<QuestionSpecification> QuestionSpecs { get; set; } = new();
        
        public bool SaveToDatabase { get; set; } = false;
        
        public string? BatchName { get; set; }
        
        public int DelayBetweenRequests { get; set; } = 1000; // milliseconds
    }

    public class QuestionSpecification
    {
        public int ChapterId { get; set; }
        public string DifficultyLevel { get; set; } = "medium";
        public string QuestionType { get; set; } = "multiple_choice";
        public string? SpecificTopic { get; set; }
        public int Count { get; set; } = 1;
    }

    public class ImproveQuestionRequest
    {
        public string? ImprovementType { get; set; } = "general"; // general, clarity, difficulty, accuracy
        
        public string? TargetDifficulty { get; set; }
        
        public string? FocusArea { get; set; } // clarity, scientific_accuracy, answer_choices, explanation
        
        public string? AdditionalInstructions { get; set; }
        
        public bool PreserveOriginalIntent { get; set; } = true;
    }

    public class TopicSuggestionRequest
    {
        [Required]
        public int ChapterId { get; set; }
        
        public string? DifficultyLevel { get; set; }
        
        public int MaxSuggestions { get; set; } = 10;
        
        public string? ExistingTopics { get; set; } // comma-separated list to avoid duplicates
        
        public bool IncludeTrendingTopics { get; set; } = true;
    }

    // Response DTOs
    public class TopicSuggestionDto
    {
        public string TopicName { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string DifficultyLevel { get; set; } = string.Empty;
        
        public List<string> KeyConcepts { get; set; } = new();
        
        public List<string> SampleQuestionTypes { get; set; } = new();
        
        public int EstimatedQuestionCount { get; set; }
        
        public double RelevanceScore { get; set; } // 0-1
    }

    public class QuestionValidationDto
    {
        public bool IsValid { get; set; }
        
        public double QualityScore { get; set; } // 0-1
        
        public string OverallAssessment { get; set; } = string.Empty;
        
        public List<ValidationIssue> Issues { get; set; } = new();
        
        public List<string> Suggestions { get; set; } = new();
        
        public ScientificAccuracy ScientificAccuracy { get; set; } = new();
        
        public AnswerChoiceAnalysis AnswerChoices { get; set; } = new();
        
        public DifficultyAssessment Difficulty { get; set; } = new();
    }

    public class ValidationIssue
    {
        public string Type { get; set; } = string.Empty; // scientific_error, unclear_wording, poor_choices, etc.
        
        public string Severity { get; set; } = string.Empty; // low, medium, high, critical
        
        public string Description { get; set; } = string.Empty;
        
        public string Suggestion { get; set; } = string.Empty;
        
        public string Location { get; set; } = string.Empty; // question, choice_a, choice_b, etc.
    }

    public class ScientificAccuracy
    {
        public bool IsAccurate { get; set; }
        
        public double AccuracyScore { get; set; } // 0-1
        
        public List<string> FactualErrors { get; set; } = new();
        
        public List<string> ConceptualIssues { get; set; } = new();
        
        public string CurriculumAlignment { get; set; } = string.Empty;
    }

    public class AnswerChoiceAnalysis
    {
        public bool HasClearCorrectAnswer { get; set; }
        
        public bool DistractorsAreReasonable { get; set; }
        
        public double ChoiceQualityScore { get; set; } // 0-1
        
        public List<string> ChoiceIssues { get; set; } = new();
        
        public List<string> ImprovementSuggestions { get; set; } = new();
    }

    public class DifficultyAssessment
    {
        public string EstimatedDifficulty { get; set; } = string.Empty;
        
        public string TargetDifficulty { get; set; } = string.Empty;
        
        public bool DifficultyMatches { get; set; }
        
        public double CognitiveLoad { get; set; } // 0-1
        
        public List<string> DifficultyFactors { get; set; } = new();
    }

    public class AIConfigDto
    {
        public string Provider { get; set; } = string.Empty; // OpenAI, Gemini, Claude
        
        public string Model { get; set; } = string.Empty;
        
        public int MaxTokens { get; set; }
        
        public double Temperature { get; set; }
        
        public bool IsConfigured { get; set; }
        
        public int RateLimit { get; set; } // requests per minute
        
        public int DailyQuota { get; set; }
        
        public int UsedToday { get; set; }
        
        public DateTime LastUsed { get; set; }
        
        public List<string> SupportedFeatures { get; set; } = new();
        
        public AIUsageStatistics Usage { get; set; } = new();
    }

    public class AIUsageStatistics
    {
        public int TotalQuestionsGenerated { get; set; }
        
        public int QuestionsGeneratedToday { get; set; }
        
        public int QuestionsGeneratedThisMonth { get; set; }
        
        public double AverageResponseTime { get; set; } // seconds
        
        public double SuccessRate { get; set; } // 0-1
        
        public Dictionary<string, int> QuestionsByDifficulty { get; set; } = new();
        
        public Dictionary<string, int> QuestionsByChapter { get; set; } = new();
    }

    // Smart Exam DTOs
    public class SmartExamGenerationRequest
    {
        [Required]
        public string ExamName { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public int Grade { get; set; } // 10, 11, 12
        
        [Required]
        public string ExamType { get; set; } = string.Empty; // 15p, 1tiet, cuoiky
        
        [Required]
        public int DurationMinutes { get; set; }
        
        [Required]
        public List<ChapterRequirement> ChapterRequirements { get; set; } = new();
        
        public DifficultyDistribution DifficultyDistribution { get; set; } = new();
        
        public bool UseAIGeneration { get; set; } = true;
        
        public bool BalanceTopics { get; set; } = true;
        
        public string? AdditionalInstructions { get; set; }
    }

    public class ChapterRequirement
    {
        public int ChapterId { get; set; }
        
        public int QuestionCount { get; set; }
        
        public List<string>? SpecificTopics { get; set; }
        
        public double Weight { get; set; } = 1.0; // importance weight
    }

    public class DifficultyDistribution
    {
        public int EasyPercentage { get; set; } = 30;
        
        public int MediumPercentage { get; set; } = 50;
        
        public int HardPercentage { get; set; } = 20;
    }

    // Enhanced Question DTOs
    public class EnhancedQuestionDto : QuestionDto
    {
        public string? Explanation { get; set; }
        
        public List<string> Tags { get; set; } = new();
        
        public List<string> LearningObjectives { get; set; } = new();
        
        public string? SolutionMethod { get; set; }
        
        public List<string> RelatedConcepts { get; set; } = new();
        
        public QuestionMetadata Metadata { get; set; } = new();
        
        public AIGenerationInfo? AIInfo { get; set; }
    }

    public class QuestionMetadata
    {
        public DateTime LastModified { get; set; }
        
        public string? ModifiedBy { get; set; }
        
        public int UsageCount { get; set; }
        
        public double AverageScore { get; set; }
        
        public double DifficultyRating { get; set; } // based on student performance
        
        public List<string> FeedbackComments { get; set; } = new();
    }

    public class AIGenerationInfo
    {
        public string Provider { get; set; } = string.Empty;
        
        public string Model { get; set; } = string.Empty;
        
        public DateTime GeneratedAt { get; set; }
        
        public string Prompt { get; set; } = string.Empty;
        
        public double ConfidenceScore { get; set; }
        
        public bool WasImproved { get; set; }
        
        public List<string> GenerationSteps { get; set; } = new();
    }

    // Analytics DTOs
    public class QuestionAnalyticsDto
    {
        public string QuestionId { get; set; } = string.Empty;
        
        public int TotalAttempts { get; set; }
        
        public int CorrectAttempts { get; set; }
        
        public double SuccessRate { get; set; }
        
        public double AverageTimeSpent { get; set; } // seconds
        
        public Dictionary<string, int> ChoiceDistribution { get; set; } = new();
        
        public List<string> CommonMistakes { get; set; } = new();
        
        public double DifficultyIndex { get; set; } // 0-1, based on performance
        
        public double DiscriminationIndex { get; set; } // how well it separates high/low performers
    }

    // Import/Export DTOs
    public class QuestionImportDto
    {
        public string QuestionText { get; set; } = string.Empty;
        
        public string QuestionType { get; set; } = string.Empty;
        
        public string DifficultyLevel { get; set; } = string.Empty;
        
        public string Topic { get; set; } = string.Empty;
        
        public string? ImageUrl { get; set; }
        
        public List<ImportAnswerChoice> AnswerChoices { get; set; } = new();
        
        public string? Explanation { get; set; }
        
        public List<string> Tags { get; set; } = new();
    }

    public class ImportAnswerChoice
    {
        public string Label { get; set; } = string.Empty;
        
        public string Text { get; set; } = string.Empty;
        
        public bool IsCorrect { get; set; }
    }

    public class QuestionExportDto
    {
        public List<EnhancedQuestionDto> Questions { get; set; } = new();
        
        public ExportMetadata Metadata { get; set; } = new();
    }

    public class ExportMetadata
    {
        public DateTime ExportedAt { get; set; }
        
        public string ExportedBy { get; set; } = string.Empty;
        
        public string Format { get; set; } = string.Empty; // JSON, CSV, PDF
        
        public Dictionary<string, object> FilterCriteria { get; set; } = new();
        
        public int TotalQuestions { get; set; }
    }
} 