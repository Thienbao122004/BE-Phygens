using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class Exam
{
    public string ExamId { get; set; } = null!;

    public string ExamName { get; set; } = null!;

    public string? Description { get; set; }

    public int? DurationMinutes { get; set; }

    public string ExamType { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public bool? IsPublished { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<ExamQuestion> ExamQuestions { get; set; } = new List<ExamQuestion>();

    public virtual ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
}
