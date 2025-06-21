using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class LearningProgress
{
    public string ProgressId { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string TopicId { get; set; } = null!;

    public int? Attempts { get; set; }

    public decimal? AvgScore { get; set; }

    public DateTime? LastUpdated { get; set; }

    public virtual PhysicsTopic Topic { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
