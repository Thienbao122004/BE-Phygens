using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class AnswerChoice
{
    public string ChoiceId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public string ChoiceLabel { get; set; } = null!;

    public string ChoiceText { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public int? DisplayOrder { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
