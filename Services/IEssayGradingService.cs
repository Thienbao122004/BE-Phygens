using BE_Phygens.Dto;
using BE_Phygens.Models;

namespace BE_Phygens.Services
{
    public interface IEssayGradingService
    {
        Task<EssayGradingResultDto> GradeEssayAsync(string questionId, string studentAnswer, string studentId);
        Task<List<EssayGradingResultDto>> BatchGradeEssaysAsync(EssayBatchGradingRequest request);
        Task<EssayAnalysisDto> AnalyzeEssayAsync(string text);
        Task<EssayQuestionDto> GenerateEssayQuestionAsync(Chapter chapter, GenerateEssayQuestionRequest request);
        Task<string> GenerateEssayFeedbackAsync(string questionId, string studentAnswer, double score);
        Task<bool> ValidateEssayAnswerAsync(string answer, int minWords, int maxWords);
    }
} 