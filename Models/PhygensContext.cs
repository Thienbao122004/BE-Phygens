using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Models
{
    public class PhygensContext : DbContext
    {
        public PhygensContext(DbContextOptions<PhygensContext> options) : base(options)
        {
        }

        // DbSets cho tất cả các models
        public DbSet<User> Users { get; set; }
        public DbSet<PhysicsTopic> PhysicsTopics { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<AnswerChoice> AnswerChoices { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamMatrix> ExamMatrices { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<Explanation> Explanations { get; set; }
        public DbSet<LearningProgress> LearningProgresses { get; set; }
        public DbSet<StudentAttempt> StudentAttempts { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        
        // AI-related DbSets
        public DbSet<AiGenerationHistory> AiGenerationHistories { get; set; }
        public DbSet<AiUsageStats> AiUsageStats { get; set; }
        public DbSet<QuestionQualityFeedback> QuestionQualityFeedbacks { get; set; }
        public DbSet<AdaptiveLearningData> AdaptiveLearningData { get; set; }
        public DbSet<SmartExamTemplate> SmartExamTemplates { get; set; }
        public DbSet<AiModelConfig> AiModelConfigs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cấu hình constraints và indexes
            
            // User constraints
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasCheckConstraint("CK_User_Role", "role IN ('teacher', 'student', 'admin')");
            });

            // PhysicsTopic constraints
            modelBuilder.Entity<PhysicsTopic>(entity =>
            {
                entity.HasIndex(e => e.TopicName).IsUnique();
                entity.HasCheckConstraint("CK_PhysicsTopic_DisplayOrder", "displayorder > 0");
            });

            // Question constraints
            modelBuilder.Entity<Question>(entity =>
            {
                entity.HasCheckConstraint("CK_Question_QuestionType", "questiontype IN ('multiple_choice', 'true_false', 'essay')");
                entity.HasCheckConstraint("CK_Question_DifficultyLevel", "difficultylevel IN ('easy', 'medium', 'hard')");
                entity.HasCheckConstraint("CK_Question_QualityScore", "qualityscore >= 0 AND qualityscore <= 10");
                entity.HasCheckConstraint("CK_Question_AiValidationStatus", "aivalidationstatus IN ('pending', 'validated', 'needsReview', 'rejected')");
                
                // Explicit column mapping to prevent EF Core confusion
                entity.Property(q => q.ChapterId)
                    .HasColumnName("ChapterId")
                    .HasColumnType("integer");
            });

            // AnswerChoice constraints
            modelBuilder.Entity<AnswerChoice>(entity =>
            {
                entity.HasCheckConstraint("CK_AnswerChoice_DisplayOrder", "displayorder > 0");
            });

            // Exam constraints
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasCheckConstraint("CK_Exam_DurationMinutes", "durationminutes > 0");
                entity.HasCheckConstraint("CK_Exam_ExamType", "examtype IN ('15p', '1tiet', 'cuoiky')");
            });

            // ExamMatrix constraints
            modelBuilder.Entity<ExamMatrix>(entity =>
            {
                entity.HasCheckConstraint("CK_ExamMatrix_NumEasy", "numeasy >= 0");
                entity.HasCheckConstraint("CK_ExamMatrix_NumMedium", "nummedium >= 0");
                entity.HasCheckConstraint("CK_ExamMatrix_NumHard", "numhard >= 0");
            });

            // ExamQuestion constraints
            modelBuilder.Entity<ExamQuestion>(entity =>
            {
                entity.HasCheckConstraint("CK_ExamQuestion_QuestionOrder", "questionorder > 0");
                entity.HasCheckConstraint("CK_ExamQuestion_PointsWeight", "pointsweight >= 0");
            });

            // LearningProgress constraints
            modelBuilder.Entity<LearningProgress>(entity =>
            {
                entity.HasCheckConstraint("CK_LearningProgress_Attempts", "attempts >= 0");
                entity.HasCheckConstraint("CK_LearningProgress_AvgScore", "avgscore >= 0 AND avgscore <= 10");
            });

            // StudentAttempt constraints
            modelBuilder.Entity<StudentAttempt>(entity =>
            {
                entity.HasCheckConstraint("CK_StudentAttempt_TotalScore", "totalscore >= 0 AND totalscore <= 10");
                entity.HasCheckConstraint("CK_StudentAttempt_MaxScore", "maxscore > 0 AND maxscore <= 10");
                entity.HasCheckConstraint("CK_StudentAttempt_Status", "status IN ('in_progress', 'completed', 'abandoned')");
            });

            // StudentAnswer constraints
            modelBuilder.Entity<StudentAnswer>(entity =>
            {
                entity.HasCheckConstraint("CK_StudentAnswer_PointsEarned", "pointsearned >= 0");
            });

            // AI-related constraints
            modelBuilder.Entity<QuestionQualityFeedback>(entity =>
            {
                entity.HasCheckConstraint("CK_QuestionQualityFeedback_Rating", "rating >= 1 AND rating <= 5");
                entity.HasCheckConstraint("CK_QuestionQualityFeedback_FeedbackType", "feedbacktype IN ('quality', 'difficulty', 'clarity', 'accuracy')");
            });

            modelBuilder.Entity<AiUsageStats>(entity =>
            {
                entity.HasIndex(e => new { e.Date, e.Provider }).IsUnique();
            });

            modelBuilder.Entity<AdaptiveLearningData>(entity =>
            {
                entity.HasIndex(e => new { e.UserId, e.ChapterId }).IsUnique();
            });

            modelBuilder.Entity<AiModelConfig>(entity =>
            {
                entity.HasIndex(e => new { e.Provider, e.ModelName }).IsUnique();
            });

            // Cấu hình relationships với cascade delete
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Topic)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Question>()
                .HasOne(q => q.Creator)
                .WithMany(u => u.CreatedQuestions)
                .HasForeignKey(q => q.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnswerChoice>()
                .HasOne(ac => ac.Question)
                .WithMany(q => q.AnswerChoices)
                .HasForeignKey(ac => ac.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Creator)
                .WithMany(u => u.CreatedExams)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Exam)
                .WithMany(e => e.ExamQuestions)
                .HasForeignKey(eq => eq.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ExamQuestion>()
                .HasOne(eq => eq.Question)
                .WithMany(q => q.ExamQuestions)
                .HasForeignKey(eq => eq.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Explanation>()
                .HasOne(ex => ex.Question)
                .WithMany(q => q.Explanations)
                .HasForeignKey(ex => ex.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Explanation>()
                .HasOne(ex => ex.Creator)
                .WithMany(u => u.CreatedExplanations)
                .HasForeignKey(ex => ex.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LearningProgress>()
                .HasOne(lp => lp.User)
                .WithMany(u => u.LearningProgresses)
                .HasForeignKey(lp => lp.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LearningProgress>()
                .HasOne(lp => lp.Topic)
                .WithMany(t => t.LearningProgresses)
                .HasForeignKey(lp => lp.TopicId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.User)
                .WithMany(u => u.StudentAttempts)
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAttempt>()
                .HasOne(sa => sa.Exam)
                .WithMany(e => e.StudentAttempts)
                .HasForeignKey(sa => sa.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Attempt)
                .WithMany(at => at.StudentAnswers)
                .HasForeignKey(sa => sa.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.SelectedChoice)
                .WithMany(ac => ac.StudentAnswers)
                .HasForeignKey(sa => sa.SelectedChoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            // AI-related relationships
            modelBuilder.Entity<AiGenerationHistory>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionQualityFeedback>()
                .HasOne(f => f.Question)
                .WithMany()
                .HasForeignKey(f => f.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<QuestionQualityFeedback>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdaptiveLearningData>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AdaptiveLearningData>()
                .HasOne(a => a.Chapter)
                .WithMany()
                .HasForeignKey(a => a.ChapterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SmartExamTemplate>()
                .HasOne(t => t.Creator)
                .WithMany()
                .HasForeignKey(t => t.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 