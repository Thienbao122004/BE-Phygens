using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class StudentAttempt
{
    public string AttemptId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string ExamId { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? TotalScore { get; set; }

    public decimal? MaxScore { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Exam Exam { get; set; } = null!;

    public virtual ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();

    public virtual User User { get; set; } = null!;
}
