using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class ExamQuestion
{
    public string ExamQuestionId { get; set; } = null!;

    public string ExamId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public int? QuestionOrder { get; set; }

    public decimal? PointsWeight { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
