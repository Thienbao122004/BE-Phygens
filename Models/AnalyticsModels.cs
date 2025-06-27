namespace BE_Phygens.Models
{
    public class QuestionAnalytics
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public int CorrectAttempts { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> ChoiceDistribution { get; set; } = new();
        public List<string> CommonWrongChoices { get; set; } = new();
        public TimeSpan AverageTimeSpent { get; set; }
        public string PerformanceLevel { get; set; } = string.Empty;
        public List<string> ImprovementSuggestions { get; set; } = new();
    }

    public class StudentPerformance
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalQuestionsAttempted { get; set; }
        public int TotalCorrectAnswers { get; set; }
        public double OverallAccuracy { get; set; }
        public Dictionary<string, double> AccuracyByDifficulty { get; set; } = new();
        public Dictionary<string, double> AccuracyByTopic { get; set; } = new();
        public Dictionary<string, double> AccuracyByQuestionType { get; set; } = new();
        public List<WeakArea> WeakAreas { get; set; } = new();
        public List<Strength> Strengths { get; set; } = new();
        public List<RecentExam> RecentExams { get; set; } = new();
        public LearningTrend LearningTrend { get; set; } = new();
        public DateTime LastActivity { get; set; }
    }

    public class WeakArea
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public double AccuracyRate { get; set; }
        public int QuestionsAttempted { get; set; }
        public List<string> CommonMistakes { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public int Priority { get; set; }
    }

    public class Strength
    {
        public string TopicId { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public double AccuracyRate { get; set; }
        public int QuestionsAttempted { get; set; }
        public string ConsistencyLevel { get; set; } = string.Empty;
    }

    public class RecentExam
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string ExamType { get; set; } = string.Empty;
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public double Percentage { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan TimeTaken { get; set; }
    }

    public class LearningTrend
    {
        public string Trend { get; set; } = string.Empty;
        public double ProgressRate { get; set; }
        public List<LearningDataPoint> DataPoints { get; set; } = new();
    }

    public class LearningDataPoint
    {
        public DateTime Date { get; set; }
        public double AccuracyRate { get; set; }
        public int QuestionsAnswered { get; set; }
    }

    public class CommonMistake
    {
        public string QuestionId { get; set; } = string.Empty;
        public string WrongChoiceId { get; set; } = string.Empty;
        public string WrongChoiceText { get; set; } = string.Empty;
        public int TimesSelected { get; set; }
        public double SelectionRate { get; set; }
        public string ReasonForMistake { get; set; } = string.Empty;
        public string Correction { get; set; } = string.Empty;
        public List<string> StudyTips { get; set; } = new();
    }

    public class ExamStatistics
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
        public double HighestScore { get; set; }
        public double LowestScore { get; set; }
        public TimeSpan AverageCompletionTime { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public Dictionary<string, double> DifficultyAnalysis { get; set; } = new();
        public List<QuestionPerformance> QuestionPerformances { get; set; } = new();
    }

    public class QuestionPerformance
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public double SuccessRate { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
        public string PerformanceRating { get; set; } = string.Empty;
    }
} 