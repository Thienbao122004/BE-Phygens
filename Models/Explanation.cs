using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class Explanation
{
    public string ExplanationId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public string ExplanationText { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual Question Question { get; set; } = null!;
}
