using System;
using System.Collections.Generic;

namespace BE_Phygens.Models;

public partial class PhysicsTopic
{
    public string TopicId { get; set; } = null!;

    public string TopicName { get; set; } = null!;

    public string? Description { get; set; }

    public string GradeLevel { get; set; } = null!;

    public int? DisplayOrder { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<LearningProgress> LearningProgresses { get; set; } = new List<LearningProgress>();

    public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
}
