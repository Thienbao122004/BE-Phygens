using System;
using System.Collections.Generic;

namespace BE_Phygens.DTOs
{
    public class ExamCreateDto
    {
        public string ExamName { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; } // "15p", "1tiet", "cuoiky"
        public string CreatedBy { get; set; } // userId
        public List<ExamQuestionCreateDto> Questions { get; set; }
    }

    public class ExamUpdateDto
    {
        public string ExamName { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; }
        public bool IsPublished { get; set; }
        public List<ExamQuestionCreateDto> Questions { get; set; }
    }

    public class ExamGenerateDto
    {
        public string ExamName { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; }
        public string CreatedBy { get; set; }
        public List<ExamMatrixDto> Matrix { get; set; } // Đề xuất: dùng cho sinh đề tự động
    }

    public class ExamMatrixDto
    {
        public string Topic { get; set; }
        public int NumEasy { get; set; }
        public int NumMedium { get; set; }
        public int NumHard { get; set; }
    }

    public class ExamQuestionCreateDto
    {
        public string QuestionId { get; set; }
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
    }

    public class ExamDto // Dùng để trả về dữ liệu exam chi tiết
    {
        public string ExamId { get; set; }
        public string ExamName { get; set; }
        public string Description { get; set; }
        public int DurationMinutes { get; set; }
        public string ExamType { get; set; }
        public string CreatedBy { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ExamQuestionDto> Questions { get; set; }
    }

    public class ExamQuestionDto
    {
        public string ExamQuestionId { get; set; }
        public string QuestionId { get; set; }
        public int QuestionOrder { get; set; }
        public decimal PointsWeight { get; set; }
        public QuestionDto Question { get; set; }
    }

    public class QuestionDto
    {
        public string QuestionId { get; set; }
        public string Topic { get; set; }
        public string QuestionText { get; set; }
        public string QuestionType { get; set; }
        public string Difficulty { get; set; }
        public string ImageUrl { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AnswerChoiceDto> AnswerChoices { get; set; }
    }

    public class AnswerChoiceDto
    {
        public string ChoiceId { get; set; }
        public string ChoiceLabel { get; set; }
        public string ChoiceText { get; set; }
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }
}
