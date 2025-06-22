using BE_Phygens.Dto;
using BE_Phygens.Models;

namespace BE_Phygens.Services
{
    /// <summary>
    /// Interface for AI-powered question generation and analysis
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Generate a single physics question using AI
        /// </summary>
        Task<QuestionDto> GenerateQuestionAsync(Chapter chapter, GenerateQuestionRequest request);

        /// <summary>
        /// Generate multiple questions in batch
        /// </summary>
        Task<List<QuestionDto>> GenerateBatchQuestionsAsync(List<QuestionSpecification> specs);

        /// <summary>
        /// Improve an existing question using AI
        /// </summary>
        Task<QuestionDto> ImproveQuestionAsync(Question existingQuestion, ImproveQuestionRequest request);

        /// <summary>
        /// Validate question quality using AI
        /// </summary>
        Task<QuestionValidationDto> ValidateQuestionAsync(Question question);

        /// <summary>
        /// Get topic suggestions for a chapter
        /// </summary>
        Task<List<TopicSuggestionDto>> GetTopicSuggestionsAsync(Chapter chapter, TopicSuggestionRequest request);

        /// <summary>
        /// Generate explanation for a question
        /// </summary>
        Task<string> GenerateExplanationAsync(Question question, string correctAnswer);

        /// <summary>
        /// Analyze student performance and suggest adaptive questions
        /// </summary>
        Task<List<QuestionDto>> GetAdaptiveQuestionsAsync(string studentId, int chapterId, int count);

        /// <summary>
        /// Generate exam based on curriculum standards
        /// </summary>
        Task<GeneratedExamDto> GenerateSmartExamAsync(SmartExamGenerationRequest request);

        /// <summary>
        /// Get AI provider status and configuration
        /// </summary>
        Task<AIConfigDto> GetAIStatusAsync();

        /// <summary>
        /// Test AI connection and capabilities
        /// </summary>
        Task<bool> TestAIConnectionAsync();
    }
} 