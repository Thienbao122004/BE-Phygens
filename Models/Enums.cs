namespace BE_Phygens.Models
{
    public static class UserRoles
    {
        public const string Student = "student";
        public const string Admin = "admin";
    }

    public static class QuestionTypes
    {
        public const string MultipleChoice = "multiple_choice";
        public const string TrueFalse = "true_false";
        public const string Essay = "essay";
    }

    public static class DifficultyLevels
    {
        public const string Easy = "easy";
        public const string Medium = "medium";
        public const string Hard = "hard";
    }

    public static class ExamTypes
    {
        public const string FifteenMinutes = "15p";
        public const string OneLesson = "1tiet";
        public const string FinalExam = "cuoiky";
        public const string AIGenerated = "ai_generated";
        public const string SmartExam = "smart_exam";
        public const string Adaptive = "adaptive";
    }

    public static class AttemptStatuses
    {
        public const string InProgress = "in_progress";
        public const string Completed = "completed";
        public const string Abandoned = "abandoned";
    }
} 