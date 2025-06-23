using System;
using System.Collections.Generic;

namespace BE_Phygens.DTOs
{
    public class ExamCreateDto
    {
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } = string.Empty; // "15p", "1tiet", "cuoiky"
        public string CreatedBy { get; set; } = string.Empty; // userId
        public List<ExamQuestionCreateDto> Questions { get; set; } = new();
        public object? AiGenerationConfig { get; set; } 
    }

    public class ExamUpdateDto
    {
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public List<ExamQuestionCreateDto> Questions { get; set; } = new();
        public object? AiGenerationConfig { get; set; } 
    }

    public class ExamGenerateDto
    {
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public List<ExamMatrixDto> Matrix { get; set; } = new(); // Đề xuất: dùng cho sinh đề tự động
    }

    public class ExamMatrixDto
    {
        public string Topic { get; set; } = string.Empty;
        public int NumEasy { get; set; }
        public int NumMedium { get; set; }
        public int NumHard { get; set; }
    }

    public class ExamQuestionCreateDto
    {
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
    }

    public class ExamDto // Dùng để trả về dữ liệu exam chi tiết
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExamQuestionDto> Questions { get; set; } = new();
        public object? AiGenerationConfig { get; set; } 
    }

    public class ExamQuestionDto
    {
        public string ExamQuestionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
        public QuestionDto Question { get; set; } = new();
    }

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
}
