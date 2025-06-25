using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BE_Phygens.Dto
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
        
        public List<ExamMatrixDto> Matrix { get; set; } = new();
        
        public int? ChapterId { get; set; }
        public int? QuestionCount { get; set; }
        public string? DifficultyLevel { get; set; }
        public bool IncludeMultipleChoice { get; set; } = true;
        public bool IncludeEssay { get; set; } = true;
        public int? Grade { get; set; }
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

    public class ExamDetailsDto // Dùng để trả về dữ liệu exam chi tiết - renamed from ExamDto
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExamQuestionResponseDto> Questions { get; set; } = new(); // renamed
        public object? AiGenerationConfig { get; set; } 
    }

    public class ExamQuestionResponseDto // renamed from ExamQuestionDto to avoid conflict
    {
        public string ExamQuestionId { get; set; } = string.Empty;
        public string QuestionId { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
        public QuestionResponseDto Question { get; set; } = new(); // renamed
    }

    public class QuestionResponseDto // renamed from QuestionDto to avoid conflict
    {
        public string QuestionId { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<AnswerChoiceResponseDto> AnswerChoices { get; set; } = new(); // renamed
    }

    public class AnswerChoiceResponseDto // renamed from AnswerChoiceDto to avoid conflict
    {
        public string ChoiceId { get; set; } = string.Empty;
        public string ChoiceLabel { get; set; } = string.Empty;
        public string ChoiceText { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }
}
