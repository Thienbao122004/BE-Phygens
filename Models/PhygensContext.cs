using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Models;

public partial class PhygensContext : DbContext
{
    public PhygensContext()
    {
    }

    public PhygensContext(DbContextOptions<PhygensContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AnswerChoice> AnswerChoices { get; set; }

    public virtual DbSet<Exam> Exams { get; set; }

    public virtual DbSet<ExamMatrix> ExamMatrices { get; set; }

    public virtual DbSet<ExamQuestion> ExamQuestions { get; set; }

    public virtual DbSet<Explanation> Explanations { get; set; }

    public virtual DbSet<LearningProgress> LearningProgresses { get; set; }

    public virtual DbSet<PhysicsTopic> PhysicsTopics { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

    public virtual DbSet<StudentAttempt> StudentAttempts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  => optionsBuilder.UseSqlServer(GetConnectionString());
    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder().
            SetBasePath(Directory.GetCurrentDirectory()).
            AddJsonFile("appsettings.json", true, true)
            .Build();
        string connectionString = config["ConnectionStrings:ConnectDB"];
        return connectionString;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnswerChoice>(entity =>
        {
            entity.HasKey(e => e.ChoiceId).HasName("PK__ANSWER_C__16CBB3276D49F7A6");

            entity.ToTable("ANSWER_CHOICE");

            entity.Property(e => e.ChoiceId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("choiceId");
            entity.Property(e => e.ChoiceLabel)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("choiceLabel");
            entity.Property(e => e.ChoiceText)
                .HasColumnType("text")
                .HasColumnName("choiceText");
            entity.Property(e => e.DisplayOrder).HasColumnName("displayOrder");
            entity.Property(e => e.IsCorrect)
                .HasDefaultValue(false)
                .HasColumnName("isCorrect");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("questionId");

            entity.HasOne(d => d.Question).WithMany(p => p.AnswerChoices)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ANSWER_CH__quest__4BAC3F29");
        });

        modelBuilder.Entity<Exam>(entity =>
        {
            entity.HasKey(e => e.ExamId).HasName("PK__EXAM__A56D125F52271BD1");

            entity.ToTable("EXAM");

            entity.Property(e => e.ExamId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("examId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("durationMinutes");
            entity.Property(e => e.ExamName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("examName");
            entity.Property(e => e.ExamType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("examType");
            entity.Property(e => e.IsPublished)
                .HasDefaultValue(false)
                .HasColumnName("isPublished");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Exams)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXAM__createdBy__52593CB8");
        });

        modelBuilder.Entity<ExamMatrix>(entity =>
        {
            entity.HasKey(e => e.MatrixId).HasName("PK__EXAM_MAT__5828B4766DC449F4");

            entity.ToTable("EXAM_MATRIX");

            entity.Property(e => e.MatrixId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("matrixId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.NumEasy)
                .HasDefaultValue(0)
                .HasColumnName("numEasy");
            entity.Property(e => e.NumHard)
                .HasDefaultValue(0)
                .HasColumnName("numHard");
            entity.Property(e => e.NumMedium)
                .HasDefaultValue(0)
                .HasColumnName("numMedium");
            entity.Property(e => e.Subject)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("subject");
            entity.Property(e => e.Topic)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("topic");
            entity.Property(e => e.TotalQuestions)
                .HasComputedColumnSql("(([numEasy]+[numMedium])+[numHard])", false)
                .HasColumnName("totalQuestions");
        });

        modelBuilder.Entity<ExamQuestion>(entity =>
        {
            entity.HasKey(e => e.ExamQuestionId).HasName("PK__EXAM_QUE__3D6427C1262EEB7A");

            entity.ToTable("EXAM_QUESTION");

            entity.HasIndex(e => new { e.ExamId, e.QuestionId }, "UQ_exam_question").IsUnique();

            entity.HasIndex(e => new { e.ExamId, e.QuestionOrder }, "UQ_exam_question_order").IsUnique();

            entity.Property(e => e.ExamQuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("examQuestionId");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("addedAt");
            entity.Property(e => e.ExamId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("examId");
            entity.Property(e => e.PointsWeight)
                .HasDefaultValue(1.0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("pointsWeight");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("questionId");
            entity.Property(e => e.QuestionOrder).HasColumnName("questionOrder");

            entity.HasOne(d => d.Exam).WithMany(p => p.ExamQuestions)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXAM_QUES__examI__59063A47");

            entity.HasOne(d => d.Question).WithMany(p => p.ExamQuestions)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXAM_QUES__quest__59FA5E80");
        });

        modelBuilder.Entity<Explanation>(entity =>
        {
            entity.HasKey(e => e.ExplanationId).HasName("PK__EXPLANAT__6FED3B8988F42024");

            entity.ToTable("EXPLANATIONS");

            entity.Property(e => e.ExplanationId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("explanationId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.ExplanationText)
                .HasColumnType("text")
                .HasColumnName("explanationText");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("questionId");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Explanations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXPLANATI__creat__74AE54BC");

            entity.HasOne(d => d.Question).WithMany(p => p.Explanations)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__EXPLANATI__quest__73BA3083");
        });

        modelBuilder.Entity<LearningProgress>(entity =>
        {
            entity.HasKey(e => e.ProgressId).HasName("PK__LEARNING__0F2BDC7D8A8D8EB3");

            entity.ToTable("LEARNING_PROGRESS");

            entity.HasIndex(e => new { e.UserId, e.TopicId }, "UQ_user_topic").IsUnique();

            entity.Property(e => e.ProgressId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("progressId");
            entity.Property(e => e.Attempts)
                .HasDefaultValue(0)
                .HasColumnName("attempts");
            entity.Property(e => e.AvgScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("avgScore");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("lastUpdated");
            entity.Property(e => e.TopicId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("topicId");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userId");

            entity.HasOne(d => d.Topic).WithMany(p => p.LearningProgresses)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LEARNING___topic__02FC7413");

            entity.HasOne(d => d.User).WithMany(p => p.LearningProgresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__LEARNING___userI__02084FDA");
        });

        modelBuilder.Entity<PhysicsTopic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PK__PHYSICS___72C15B413E585F18");

            entity.ToTable("PHYSICS_TOPIC");

            entity.HasIndex(e => e.TopicName, "UQ__PHYSICS___65A860FEF0C0C75E").IsUnique();

            entity.Property(e => e.TopicId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("topicId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.DisplayOrder).HasColumnName("displayOrder");
            entity.Property(e => e.GradeLevel)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("gradeLevel");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");
            entity.Property(e => e.TopicName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("topicName");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PK__QUESTION__6238D4B2C6C3F22B");

            entity.ToTable("QUESTION");

            entity.Property(e => e.QuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("questionId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("createdBy");
            entity.Property(e => e.DifficultyLevel)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("difficultyLevel");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("imageUrl");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");
            entity.Property(e => e.QuestionText)
                .HasColumnType("text")
                .HasColumnName("questionText");
            entity.Property(e => e.QuestionType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("questionType");
            entity.Property(e => e.TopicId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("topicId");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Questions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__QUESTION__create__46E78A0C");

            entity.HasOne(d => d.Topic).WithMany(p => p.Questions)
                .HasForeignKey(d => d.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__QUESTION__topicI__440B1D61");
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("PK__STUDENT___6836B974F48E04EB");

            entity.ToTable("STUDENT_ANSWER");

            entity.HasIndex(e => new { e.AttemptId, e.QuestionId }, "UQ_attempt_question").IsUnique();

            entity.Property(e => e.AnswerId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("answerId");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("answeredAt");
            entity.Property(e => e.AttemptId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("attemptId");
            entity.Property(e => e.IsCorrect)
                .HasDefaultValue(false)
                .HasColumnName("isCorrect");
            entity.Property(e => e.PointsEarned)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("pointsEarned");
            entity.Property(e => e.QuestionId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("questionId");
            entity.Property(e => e.SelectedChoiceId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("selectedChoiceId");
            entity.Property(e => e.StudentTextAnswer)
                .HasColumnType("text")
                .HasColumnName("studentTextAnswer");

            entity.HasOne(d => d.Attempt).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.AttemptId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__STUDENT_A__attem__6B24EA82");

            entity.HasOne(d => d.Question).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__STUDENT_A__quest__6C190EBB");

            entity.HasOne(d => d.SelectedChoice).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.SelectedChoiceId)
                .HasConstraintName("FK__STUDENT_A__selec__6D0D32F4");
        });

        modelBuilder.Entity<StudentAttempt>(entity =>
        {
            entity.HasKey(e => e.AttemptId).HasName("PK__STUDENT___9304800654E48BB9");

            entity.ToTable("STUDENT_ATTEMPT");

            entity.Property(e => e.AttemptId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("attemptId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("endTime");
            entity.Property(e => e.ExamId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("examId");
            entity.Property(e => e.MaxScore)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("maxScore");
            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("startTime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("in_progress")
                .HasColumnName("status");
            entity.Property(e => e.TotalScore)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("totalScore");
            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userId");

            entity.HasOne(d => d.Exam).WithMany(p => p.StudentAttempts)
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__STUDENT_A__examI__619B8048");

            entity.HasOne(d => d.User).WithMany(p => p.StudentAttempts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__STUDENT_A__userI__60A75C0F");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USER__CB9A1CFFB71FFD2F");

            entity.ToTable("USER");

            entity.HasIndex(e => e.Email, "UQ__USER__AB6E616472382E9D").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__USER__F3DBC5722C9DF1B4").IsUnique();

            entity.Property(e => e.UserId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userId");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("createdAt");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("fullName");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("isActive");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("passwordHash");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("username");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
