using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class Question
{
    public string QuestionId { get; set; } = null!;

    public string TopicId { get; set; } = null!;

    public string QuestionText { get; set; } = null!;

    public string QuestionType { get; set; } = null!;

    public string DifficultyLevel { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<AnswerChoice> AnswerChoices { get; set; } = new List<AnswerChoice>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public virtual ICollection<Explanation> Explanations { get; set; } = new List<Explanation>();

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    public virtual PhysicsTopic Topic { get; set; } = null!;
}
