using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class ExamMatrix
{
    public string MatrixId { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Topic { get; set; } = null!;

    public int? NumEasy { get; set; }

    public int? NumMedium { get; set; }

    public int? NumHard { get; set; }

    public int? TotalQuestions { get; set; }

    public DateTime? CreatedAt { get; set; }
}
