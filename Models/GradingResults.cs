namespace BE_Phygens.Models
{
    public class QuestionGradingResult
    {
        public string QuestionId { get; set; } = string.Empty;
        public string CorrectChoiceId { get; set; } = string.Empty;
        public string CorrectChoiceLabel { get; set; } = string.Empty;
        public string CorrectChoiceText { get; set; } = string.Empty;
        public string StudentChoiceId { get; set; } = string.Empty;
        public string StudentChoiceLabel { get; set; } = string.Empty;
        public string StudentChoiceText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public double PointsEarned { get; set; }
        public double MaxPoints { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public DateTime GradedAt { get; set; }
    }

    public class StudentAnswerSubmission
    {
        public string QuestionId { get; set; } = string.Empty;
        public string SelectedChoiceId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }

    public class ExamGradingResult
    {
        public string ExamId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int IncorrectAnswers { get; set; }
        public double TotalPointsEarned { get; set; }
        public double MaxPossiblePoints { get; set; }
        public double PercentageScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public TimeSpan? TimeTaken { get; set; }
        public DateTime CompletedAt { get; set; }
        public Dictionary<string, int> DifficultyBreakdown { get; set; } = new();
        public Dictionary<string, double> TopicAccuracy { get; set; } = new();
        public IEnumerable<QuestionGradingResult> QuestionResults { get; set; } = new List<QuestionGradingResult>();
        public ExamAnalysis Analysis { get; set; } = new();
    }

    public class ExamAnalysis
    {
        public string PerformanceLevel { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
        public List<string> StudyPlan { get; set; } = new();
        public Dictionary<string, double> TopicBreakdown { get; set; } = new();
    }
} 