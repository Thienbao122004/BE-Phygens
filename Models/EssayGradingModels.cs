using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_Phygens.Models
{
    /// <summary>
    /// 📝 Tiêu chí chấm điểm tự luận cho AI
    /// </summary>
    public class AIEssayGradingCriteria
    {
        public string Subject { get; set; } = "Vật lý";
        public int Grade { get; set; } = 10;
        public string QuestionType { get; set; } = "essay";
        public string DifficultyLevel { get; set; } = "medium";
        public double MaxScore { get; set; } = 10;
        public List<GradingCriterion> Criteria { get; set; } = new List<GradingCriterion>();
    }

    /// <summary>
    /// 📊 Tiêu chí chấm điểm chi tiết
    /// </summary>
    public class GradingCriterion
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Weight { get; set; } = 0; // Phần trăm trọng số
        public double MaxPoints { get; set; } = 0; // Điểm tối đa
    }

    /// <summary>
    /// 🎯 Kết quả chấm điểm tự luận
    /// </summary>
    public class EssayGradingResult
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = "";
        public double Score { get; set; } = 0;
        public double MaxScore { get; set; } = 10;
        public DetailedFeedback? DetailedFeedback { get; set; }
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
        public string GradingMethod { get; set; } = "AI";
    }

    /// <summary>
    /// 📋 Phản hồi chi tiết từ AI
    /// </summary>
    public class DetailedFeedback
    {
        public string OverallFeedback { get; set; } = "";
        public List<string> Strengths { get; set; } = new List<string>();
        public List<string> Improvements { get; set; } = new List<string>();
        public List<string> Suggestions { get; set; } = new List<string>();
        public List<CriteriaScore> CriteriaScores { get; set; } = new List<CriteriaScore>();
    }

    /// <summary>
    /// 📏 Điểm theo từng tiêu chí
    /// </summary>
    public class CriteriaScore
    {
        public string Name { get; set; } = "";
        public double Score { get; set; } = 0;
        public double MaxScore { get; set; } = 0;
        public string Feedback { get; set; } = "";
    }

    /// <summary>
    /// 🤖 Response từ AI (JSON parsing)
    /// </summary>
    public class AIGradingResponse
    {
        public double totalScore { get; set; } = 0;
        public double maxScore { get; set; } = 10;
        public string overallFeedback { get; set; } = "";
        public List<string> strengths { get; set; } = new List<string>();
        public List<string> improvements { get; set; } = new List<string>();
        public List<string> suggestions { get; set; } = new List<string>();
        public List<AIGradingCriteriaScore>? criteriaScores { get; set; }
    }

    /// <summary>
    /// 🤖 Điểm từng tiêu chí từ AI
    /// </summary>
    public class AIGradingCriteriaScore
    {
        public string name { get; set; } = "";
        public double score { get; set; } = 0;
        public double maxScore { get; set; } = 0;
        public string feedback { get; set; } = "";
    }

    /// <summary>
    /// 💾 Lưu trữ kết quả chấm điểm trong database
    /// </summary>
    [Table("EssayGradingRecords")]
    public class EssayGradingRecord
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string QuestionId { get; set; } = "";
        
        [Required]
        public string StudentId { get; set; } = "";
        
        [Required]
        public string StudentAnswer { get; set; } = "";
        
        [Required]
        public double Score { get; set; } = 0;
        
        [Required]
        public double MaxScore { get; set; } = 10;
        
        [Column(TypeName = "text")]
        public string DetailedFeedback { get; set; } = ""; // JSON string
        
        [Required]
        public DateTime GradedAt { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string GradingMethod { get; set; } = "AI"; // AI, Manual, Hybrid
        
        public string? GraderId { get; set; } // ID của người chấm (nếu manual)
        
        public bool IsReviewed { get; set; } = false; // Đã được xem xét lại chưa
        
        public string? ReviewNotes { get; set; } // Ghi chú khi xem xét lại
    }

    /// <summary>
    /// 📊 Thống kê chấm điểm tự luận
    /// </summary>
    public class EssayGradingStatistics
    {
        public int TotalGraded { get; set; } = 0;
        public double AverageScore { get; set; } = 0;
        public double HighestScore { get; set; } = 0;
        public double LowestScore { get; set; } = 0;
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public Dictionary<string, double> CriteriaAverages { get; set; } = new();
        public List<string> CommonStrengths { get; set; } = new();
        public List<string> CommonWeaknesses { get; set; } = new();
    }
} 