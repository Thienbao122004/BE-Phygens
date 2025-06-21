using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class StudentAnswer
{
    public string AnswerId { get; set; } = null!;

    public string AttemptId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public string? SelectedChoiceId { get; set; }

    public string? StudentTextAnswer { get; set; }

    public bool? IsCorrect { get; set; }

    public decimal? PointsEarned { get; set; }

    public DateTime? AnsweredAt { get; set; }

    public virtual StudentAttempt Attempt { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;

    public virtual AnswerChoice? SelectedChoice { get; set; }
}
