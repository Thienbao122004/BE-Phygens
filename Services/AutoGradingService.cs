using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Services
{
    public class AutoGradingService : IAutoGradingService
    {
        private readonly PhygensContext _context;
        private readonly ILogger<AutoGradingService> _logger;

        public AutoGradingService(PhygensContext context, ILogger<AutoGradingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<QuestionGradingResult> GradeSingleQuestionAsync(string questionId, string studentChoiceId, string? studentUserId)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    throw new ArgumentException("Không tìm thấy câu hỏi");
                }

                var examQuestion = await _context.ExamQuestions
                    .Include(eq => eq.Exam)
                    .FirstOrDefaultAsync(eq => eq.QuestionId == questionId);

                double pointsPerQuestion = 1.0; // Mặc định 1 điểm cho câu đơn lẻ
                if (examQuestion?.Exam != null)
                {
                    var totalQuestionsInExam = await _context.ExamQuestions
                        .CountAsync(eq => eq.ExamId == examQuestion.ExamId);
                    
                    if (totalQuestionsInExam > 0)
                    {
                        pointsPerQuestion = 10.0 / totalQuestionsInExam; 
                    }
                }

                // Kiểm tra loại câu hỏi
                if (question.QuestionType?.ToLower() == "essay")
                {
                    var essayAnswer = studentChoiceId; 

                    var result = new QuestionGradingResult
                    {
                        QuestionId = questionId,
                        CorrectChoiceId = "",
                        CorrectChoiceLabel = "",
                        CorrectChoiceText = "Câu hỏi tự luận - cần chấm thủ công",
                        StudentChoiceId = "",
                        StudentChoiceLabel = "",
                        StudentChoiceText = essayAnswer ?? "",
                        IsCorrect = false, 
                        PointsEarned = 0,
                        MaxPoints = pointsPerQuestion, 
                        Feedback = "Câu hỏi tự luận cần được chấm thủ công",
                        Explanation = "Đây là câu hỏi tự luận, cần giáo viên chấm điểm",
                        DifficultyLevel = question.DifficultyLevel ?? "",
                        QuestionType = question.QuestionType ?? "",
                        GradedAt = DateTime.UtcNow
                    };

                    // Lưu câu trả lời tự luận nếu có studentUserId
                    if (!string.IsNullOrEmpty(studentUserId))
                    {
                        if (examQuestion != null)
                        {
                            var attempt = new StudentAttempt
                            {
                                AttemptId = Guid.NewGuid().ToString(),
                                UserId = studentUserId,
                                ExamId = examQuestion.ExamId,
                                StartTime = DateTime.UtcNow,
                                EndTime = DateTime.UtcNow,
                                TotalScore = 0, 
                                MaxScore = 10.0m, 
                                Status = "completed"
                            };
                            _context.StudentAttempts.Add(attempt);

                            var studentAnswer = new StudentAnswer
                            {
                                AnswerId = Guid.NewGuid().ToString(),
                                AttemptId = attempt.AttemptId,
                                QuestionId = questionId,
                                SelectedChoiceId = null,
                                StudentTextAnswer = essayAnswer, 
                                IsCorrect = false, 
                                PointsEarned = 0, 
                                AnsweredAt = DateTime.UtcNow
                            };

                            _context.StudentAnswers.Add(studentAnswer);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return result;
                }

                // Query AnswerChoices bằng projection để tránh NULL displayOrder issues
                var answerChoicesData = await _context.AnswerChoices
                    .Where(ac => ac.QuestionId == questionId)
                    .Select(ac => new
                    {
                        ac.ChoiceId,
                        ac.QuestionId,
                        ac.ChoiceLabel,
                        ac.ChoiceText,
                        ac.IsCorrect,
                        DisplayOrder = (int?)ac.DisplayOrder ?? 1 // Cast tới nullable để xử lý NULL từ database
                    })
                    .OrderBy(ac => ac.DisplayOrder)
                    .ThenBy(ac => ac.ChoiceLabel)
                    .ToListAsync();

                if (!answerChoicesData.Any())
                {
                    throw new ArgumentException("Câu hỏi không có lựa chọn đáp án");
                }

                var studentChoiceData = answerChoicesData
                    .FirstOrDefault(c => c.ChoiceId == studentChoiceId);

                if (studentChoiceData == null)
                {
                    throw new ArgumentException("Không tìm thấy lựa chọn của học sinh");
                }

                var correctChoiceData = answerChoicesData.FirstOrDefault(c => c.IsCorrect);
                if (correctChoiceData == null)
                {
                    throw new InvalidOperationException("Câu hỏi không có đáp án đúng");
                }

                bool isCorrect = studentChoiceData.IsCorrect;
                double pointsEarned = isCorrect ? pointsPerQuestion : 0.0;

                var explanation = await _context.Explanations
                    .FirstOrDefaultAsync(e => e.QuestionId == questionId);

                _logger.LogInformation("Debug AutoGrading - Question {QuestionId}: Student={StudentLabel}.{StudentText}, Correct={CorrectLabel}.{CorrectText}, Points: {PointsEarned}/{MaxPoints}", 
                    questionId, 
                    studentChoiceData.ChoiceLabel, studentChoiceData.ChoiceText,
                    correctChoiceData.ChoiceLabel, correctChoiceData.ChoiceText,
                    pointsEarned, pointsPerQuestion);

                var multipleChoiceResult = new QuestionGradingResult
                {
                    QuestionId = questionId,
                    CorrectChoiceId = correctChoiceData.ChoiceId ?? "",
                    CorrectChoiceLabel = correctChoiceData.ChoiceLabel ?? "",
                    CorrectChoiceText = correctChoiceData.ChoiceText ?? "",
                    StudentChoiceId = studentChoiceData.ChoiceId ?? "",
                    StudentChoiceLabel = studentChoiceData.ChoiceLabel ?? "",
                    StudentChoiceText = studentChoiceData.ChoiceText ?? "",
                    IsCorrect = isCorrect,
                    PointsEarned = pointsEarned, 
                    MaxPoints = pointsPerQuestion, 
                    Feedback = isCorrect ? "Chính xác!" : "Chưa chính xác",
                    Explanation = explanation?.ExplanationText ?? "Chưa có giải thích chi tiết",
                    DifficultyLevel = question.DifficultyLevel ?? "",
                    QuestionType = question.QuestionType ?? "",
                    GradedAt = DateTime.UtcNow
                };

                // Debug: Log kết quả được tạo
                _logger.LogInformation("Debug AutoGrading - Result created: StudentChoice={StudentLabel}.{StudentText}, CorrectChoice={CorrectLabel}.{CorrectText}, Points: {PointsEarned}/{MaxPoints}",
                    multipleChoiceResult.StudentChoiceLabel, multipleChoiceResult.StudentChoiceText,
                    multipleChoiceResult.CorrectChoiceLabel, multipleChoiceResult.CorrectChoiceText,
                    multipleChoiceResult.PointsEarned, multipleChoiceResult.MaxPoints);

                // Lưu kết quả vào lịch sử nếu có studentUserId
                if (!string.IsNullOrEmpty(studentUserId))
                {
                    if (examQuestion == null)
                    {
                        throw new ArgumentException("Không tìm thấy câu hỏi trong bài thi");
                    }

                    // Tạo một attempt mới cho lần trả lời này
                    var attempt = new StudentAttempt
                    {
                        AttemptId = Guid.NewGuid().ToString(),
                        UserId = studentUserId,
                        ExamId = examQuestion.ExamId,
                        StartTime = DateTime.UtcNow,
                        EndTime = DateTime.UtcNow,
                        TotalScore = (decimal)pointsEarned, 
                        MaxScore = 10.0m, // Thang điểm 10
                        Status = "completed"
                    };
                    _context.StudentAttempts.Add(attempt);

                    var studentAnswer = new StudentAnswer
                    {
                        AnswerId = Guid.NewGuid().ToString(),
                        AttemptId = attempt.AttemptId,
                        QuestionId = questionId,
                        SelectedChoiceId = studentChoiceId,
                        IsCorrect = isCorrect,
                        PointsEarned = (decimal)pointsEarned, 
                        AnsweredAt = DateTime.UtcNow
                    };

                    _context.StudentAnswers.Add(studentAnswer);
                    await _context.SaveChangesAsync();
                }

                return multipleChoiceResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm câu hỏi {QuestionId}", questionId);
                throw;
            }
        }

        public async Task<DetailedFeedback> GetDetailedFeedbackAsync(string questionId, string studentChoiceId)
        {
            // Lấy thông tin câu hỏi
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                throw new ArgumentException("Không tìm thấy câu hỏi");
            }

            // Lấy choices với projection để tránh NULL displayOrder
            var choicesData = await _context.AnswerChoices
                .Where(ac => ac.QuestionId == questionId)
                .Select(ac => new
                {
                    ac.ChoiceId,
                    ac.ChoiceLabel,
                    ac.ChoiceText,
                    ac.IsCorrect,
                    DisplayOrder = (int?)ac.DisplayOrder ?? 1 // Cast tới nullable để xử lý NULL từ database
                })
                .ToListAsync();

            var studentChoiceData = choicesData
                .FirstOrDefault(c => c.ChoiceId == studentChoiceId);

            if (studentChoiceData == null)
            {
                throw new ArgumentException("Không tìm thấy lựa chọn của học sinh");
            }

            // Lấy đáp án đúng
            var correctChoiceData = choicesData.FirstOrDefault(c => c.IsCorrect);
            if (correctChoiceData == null)
            {
                throw new InvalidOperationException("Câu hỏi không có đáp án đúng");
            }

            // Lấy giải thích chi tiết
            var explanation = await _context.Explanations
                .FirstOrDefaultAsync(e => e.QuestionId == questionId);

            // Lấy thông tin về chủ đề liên quan
            var topic = await _context.PhysicsTopics
                .FirstOrDefaultAsync(t => t.Questions.Any(q => q.QuestionId == questionId));

            // Tạo phản hồi chi tiết
            var feedback = new DetailedFeedback
            {
                QuestionId = questionId,
                QuestionText = question.QuestionText,
                CorrectAnswer = $"{correctChoiceData.ChoiceLabel}. {correctChoiceData.ChoiceText}",
                StudentAnswer = $"{studentChoiceData.ChoiceLabel}. {studentChoiceData.ChoiceText}",
                IsCorrect = studentChoiceData.IsCorrect,
                Explanation = explanation?.ExplanationText ?? "Chưa có giải thích chi tiết",
                CommonMistakeWarning = studentChoiceData.IsCorrect ? string.Empty : "Đây là một lỗi phổ biến. Hãy chú ý đọc kỹ đề bài và xem lại phần lý thuyết liên quan.",
                StudyTip = GetStudyTip(question, studentChoiceData.IsCorrect),
                RelatedTopics = topic?.TopicName ?? "Chưa phân loại"
            };

            return feedback;
        }

        private string GetCommonMistakeWarning(Question question, AnswerChoice studentChoice)
        {
            // Phân tích lỗi phổ biến dựa trên lựa chọn của học sinh
            if (!studentChoice.IsCorrect)
            {
                return "Đây là một lỗi phổ biến. Hãy chú ý đọc kỹ đề bài và xem lại phần lý thuyết liên quan.";
            }
            return string.Empty;
        }

        private string GetStudyTip(Question question, bool isCorrect)
        {
            if (!isCorrect)
            {
                return "Gợi ý: Hãy ôn lại các công thức và định luật liên quan. Thử giải lại bài tập với cách tiếp cận khác.";
            }
            return "Tốt lắm! Hãy tiếp tục luyện tập các dạng bài tập tương tự để nâng cao kỹ năng.";
        }

        public async Task<ExamGradingResult> GradeExamAsync(string examId, List<StudentAnswerSubmission> studentAnswers, string studentUserId)
        {
            // Lấy thông tin bài thi (không cần Include AnswerChoices vì sẽ query riêng)
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
            {
                throw new ArgumentException("Không tìm thấy bài thi");
            }

            // Tạo attempt mới
            var attempt = new StudentAttempt
            {
                AttemptId = Guid.NewGuid().ToString(),
                ExamId = examId,
                UserId = studentUserId,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow,
                Status = "completed"
            };
            _context.StudentAttempts.Add(attempt);

            int totalQuestionsInExam = exam.ExamQuestions.Count;
            double pointsPerQuestion = totalQuestionsInExam > 0 ? 10.0 / totalQuestionsInExam : 1.0;

            // Chấm từng câu hỏi
            var questionResults = new List<QuestionGradingResult>();
            double totalPointsEarned = 0; 
            int correctAnswers = 0;
            var topicAccuracy = new Dictionary<string, (int correct, int total)>();
            var difficultyBreakdown = new Dictionary<string, int>();

            foreach (var answer in studentAnswers)
            {
                // Xử lý khác nhau cho câu hỏi trắc nghiệm và tự luận
                var question = exam.ExamQuestions.FirstOrDefault(eq => eq.QuestionId == answer.QuestionId)?.Question;
                QuestionGradingResult result;

                if (question?.QuestionType?.ToLower() == "essay")
                {
                    // Với câu hỏi tự luận, xử lý riêng biệt
                    var essayContent = answer.StudentTextAnswer ?? "";
                    result = await GradeEssayQuestionAsync(answer.QuestionId, essayContent, studentUserId);
                }
                else
                {
                    // Với câu hỏi trắc nghiệm, truyền selectedChoiceId
                    result = await GradeSingleQuestionAsync(answer.QuestionId, answer.SelectedChoiceId, studentUserId);
                }

                questionResults.Add(result);

                if (result.IsCorrect)
                {
                    correctAnswers++;
                }

                totalPointsEarned += result.PointsEarned;

                // Cập nhật thống kê theo chủ đề
                var examQuestion = exam.ExamQuestions.FirstOrDefault(eq => eq.QuestionId == answer.QuestionId);
                if (examQuestion?.Question != null)
                {
                    var topic = await _context.PhysicsTopics
                        .FirstOrDefaultAsync(t => t.Questions.Any(q => q.QuestionId == examQuestion.QuestionId));
                    
                    if (topic != null)
                    {
                        if (!topicAccuracy.ContainsKey(topic.TopicName))
                        {
                            topicAccuracy[topic.TopicName] = (0, 0);
                        }
                        var (correct, total) = topicAccuracy[topic.TopicName];
                        topicAccuracy[topic.TopicName] = (correct + (result.IsCorrect ? 1 : 0), total + 1);
                    }

                    // Cập nhật thống kê theo độ khó
                    var difficulty = result.DifficultyLevel;
                    if (!difficultyBreakdown.ContainsKey(difficulty))
                    {
                        difficultyBreakdown[difficulty] = 0;
                    }
                    difficultyBreakdown[difficulty]++;
                }
            }

            var percentageScore = totalQuestionsInExam > 0 ? (totalPointsEarned / 10.0 * 100) : 0;
            var finalScore = Math.Max(0, totalPointsEarned); // Điểm thực tế, tối thiểu 0
            
            // Debug: Log chi tiết về tính điểm
            _logger.LogInformation(" AutoGrading Debug - Exam: {ExamId}, Student: {StudentId}", examId, studentUserId);
            _logger.LogInformation(" Points: {TotalPointsEarned}/10 = {PercentageScore}% | Questions: {CorrectAnswers}/{TotalQuestions}", 
                totalPointsEarned, percentageScore, correctAnswers, totalQuestionsInExam);
            _logger.LogInformation(" PointsPerQuestion: {PointsPerQuestion}, FinalScore: {FinalScore}", pointsPerQuestion, finalScore);
            
            attempt.TotalScore = Math.Round((decimal)finalScore, 2); 
            attempt.MaxScore = 10;
            await _context.SaveChangesAsync();

            var examResult = new ExamGradingResult
            {
                ExamId = examId,
                StudentId = studentUserId,
                ExamName = exam.ExamName,
                ExamType = exam.ExamType,
                TotalQuestions = totalQuestionsInExam,
                CorrectAnswers = correctAnswers,
                IncorrectAnswers = totalQuestionsInExam - correctAnswers,
                TotalPointsEarned = Math.Round(totalPointsEarned, 2), 
                MaxPossiblePoints = 10,
                PercentageScore = Math.Round(percentageScore, 2), 
                Grade = GetGrade(percentageScore),
                CompletedAt = DateTime.UtcNow,
                DifficultyBreakdown = difficultyBreakdown,
                TopicAccuracy = topicAccuracy.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.correct * 100.0 / kvp.Value.total
                ),
                QuestionResults = questionResults.Cast<Models.QuestionGradingResult>(),
                Analysis = GenerateExamAnalysis(percentageScore, topicAccuracy, difficultyBreakdown)
            };

            return examResult;
        }

        public async Task<ExamGradingResult> GradeExamAttemptAsync(string attemptId)
        {
            var attempt = await _context.StudentAttempts
                .Include(a => a.Exam)
                .Include(a => a.StudentAnswers)
                .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

            if (attempt == null)
            {
                throw new ArgumentException("Không tìm thấy lần thi");
            }

            var studentAnswers = attempt.StudentAnswers.Select(a => new StudentAnswerSubmission
            {
                QuestionId = a.QuestionId,
                SelectedChoiceId = a.SelectedChoiceId ?? string.Empty,
                SubmittedAt = a.AnsweredAt
            }).ToList();

            return await GradeExamAsync(attempt.ExamId, studentAnswers, attempt.UserId);
        }

        public async Task<List<QuestionGradingResult>> BatchGradeQuestionsAsync(List<StudentAnswerSubmission> submissions, string? studentUserId)
        {
            var results = new List<QuestionGradingResult>();
            foreach (var submission in submissions)
            {
                var result = await GradeSingleQuestionAsync(
                    submission.QuestionId,
                    submission.SelectedChoiceId,
                    studentUserId
                );
                results.Add(result);
            }
            return results;
        }

        private string GetGrade(double percentageScore)
        {
            if (percentageScore >= 90) return "A";
            if (percentageScore >= 80) return "B";
            if (percentageScore >= 70) return "C";
            if (percentageScore >= 60) return "D";
            return "F";
        }

        private ExamAnalysis GenerateExamAnalysis(
            double percentageScore,
            Dictionary<string, (int correct, int total)> topicAccuracy,
            Dictionary<string, int> difficultyBreakdown)
        {
            var analysis = new ExamAnalysis();

            // Xác định mức độ thành tích
            if (percentageScore >= 90)
                analysis.PerformanceLevel = "Xuất sắc";
            else if (percentageScore >= 80)
                analysis.PerformanceLevel = "Giỏi";
            else if (percentageScore >= 70)
                analysis.PerformanceLevel = "Khá";
            else if (percentageScore >= 60)
                analysis.PerformanceLevel = "Trung bình";
            else
                analysis.PerformanceLevel = "Cần cải thiện";

            // Tạo các khuyến nghị
            analysis.Recommendations = new List<string>();
            analysis.StudyPlan = new List<string>();

            // Phân tích điểm yếu theo chủ đề
            var weakTopics = topicAccuracy
                .Where(t => t.Value.correct * 100.0 / t.Value.total < 70)
                .Select(t => t.Key)
                .ToList();

            if (weakTopics.Any())
            {
                analysis.Recommendations.Add($"Cần tập trung ôn tập các chủ đề: {string.Join(", ", weakTopics)}");
                analysis.StudyPlan.Add("Xem lại lý thuyết và làm thêm bài tập về các chủ đề trên");
            }

            // Phân tích theo độ khó
            if (difficultyBreakdown.ContainsKey("hard") && difficultyBreakdown["hard"] > 0)
            {
                analysis.Recommendations.Add("Cần luyện tập thêm các bài tập khó");
                analysis.StudyPlan.Add("Tăng dần độ khó của bài tập khi luyện tập");
            }

            // Tạo phân tích chi tiết theo chủ đề
            analysis.TopicBreakdown = topicAccuracy.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.correct * 100.0 / kvp.Value.total
            );

            return analysis;
        }

        public async Task<List<QuestionAnalytics>> GetQuestionAnalyticsAsync(string questionId)
        {
            var questions = new List<Question>();
            
            if (string.IsNullOrEmpty(questionId))
            {
                // Nếu không có questionId, lấy phân tích cho tất cả câu hỏi (không Include AnswerChoices)
                questions = await _context.Questions
                    .Include(q => q.Topic)
                    .ToListAsync();
            }
            else
            {
                // Nếu có questionId, chỉ lấy phân tích cho câu hỏi đó (không Include AnswerChoices)
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    throw new ArgumentException("Không tìm thấy câu hỏi");
                }

                questions.Add(question);
            }

            var result = new List<QuestionAnalytics>();

            foreach (var question in questions)
            {
                // Lấy tất cả các lần trả lời cho câu hỏi này
                var answers = await _context.StudentAnswers
                    .Where(a => a.QuestionId == question.QuestionId)
                    .ToListAsync();

                var totalAttempts = answers.Count;
                var correctAttempts = answers.Count(a => a.IsCorrect);

                // Phân tích lựa chọn phổ biến
                var choiceDistribution = answers
                    .GroupBy(a => a.SelectedChoiceId)
                    .ToDictionary(
                        g => g.Key ?? "no_answer",
                        g => g.Count()
                    );

                // Tìm các lựa chọn sai phổ biến
                var commonWrongChoices = answers
                    .Where(a => !a.IsCorrect && a.SelectedChoiceId != null)
                    .GroupBy(a => a.SelectedChoiceId)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key ?? "no_answer")
                    .ToList();

                // Tính thời gian trung bình
                var averageTimeSpent = TimeSpan.FromMinutes(2); // Mặc định 2 phút

                // Xác định mức độ hiệu suất
                string performanceLevel;
                var successRate = totalAttempts > 0 ? (double)correctAttempts / totalAttempts * 100 : 0;
                if (successRate >= 80) performanceLevel = "Xuất sắc";
                else if (successRate >= 60) performanceLevel = "Khá";
                else if (successRate >= 40) performanceLevel = "Trung bình";
                else performanceLevel = "Cần cải thiện";

                // Đề xuất cải thiện
                var suggestions = new List<string>();
                if (successRate < 60)
                {
                    suggestions.Add("Ôn tập lại lý thuyết cơ bản của chủ đề này");
                    suggestions.Add("Làm thêm các bài tập tương tự để rèn luyện kỹ năng");
                    suggestions.Add("Tham khảo giải thích chi tiết cho các lần làm sai");
                }

                result.Add(new QuestionAnalytics
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    DifficultyLevel = question.DifficultyLevel ?? "medium",
                    TopicName = question.Topic?.TopicName ?? "Chưa phân loại",
                    TotalAttempts = totalAttempts,
                    CorrectAttempts = correctAttempts,
                    SuccessRate = successRate,
                    ChoiceDistribution = choiceDistribution,
                    CommonWrongChoices = commonWrongChoices,
                    AverageTimeSpent = averageTimeSpent,
                    PerformanceLevel = performanceLevel,
                    ImprovementSuggestions = suggestions
                });
            }

            return result;
        }

        public async Task<StudentPerformance> GetStudentPerformanceAsync(string studentId, string? examId)
        {
            // Lấy thông tin học sinh
            var student = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == studentId);

            if (student == null)
            {
                throw new ArgumentException("Không tìm thấy học sinh");
            }

            // Lấy tất cả các lần làm bài của học sinh
            var query = _context.StudentAnswers
                .Include(a => a.Attempt)
                .Include(a => a.Question)
                .ThenInclude(q => q.Topic)
                .Where(a => a.Attempt.UserId == studentId);

            // Nếu có examId, chỉ lấy kết quả của bài thi cụ thể
            if (!string.IsNullOrEmpty(examId))
            {
                query = query.Where(a => a.Attempt.ExamId == examId);
            }

            var answers = await query.ToListAsync();

            // Tính toán các chỉ số cơ bản
            var totalQuestions = answers.Count;
            var correctAnswers = answers.Count(a => a.IsCorrect);
            var overallAccuracy = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;

            // Phân tích theo độ khó
            var accuracyByDifficulty = answers
                .GroupBy(a => a.Question.DifficultyLevel ?? "medium")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 ? (double)g.Count(a => a.IsCorrect) / g.Count() * 100 : 0
                );

            // Phân tích theo chủ đề
            var accuracyByTopic = answers
                .GroupBy(a => a.Question.Topic.TopicName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 ? (double)g.Count(a => a.IsCorrect) / g.Count() * 100 : 0
                );

            // Phân tích theo loại câu hỏi
            var accuracyByQuestionType = answers
                .GroupBy(a => a.Question.QuestionType ?? "multiple_choice")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 ? (double)g.Count(a => a.IsCorrect) / g.Count() * 100 : 0
                );

            // Xác định điểm yếu
            var weakAreas = accuracyByTopic
                .Where(t => t.Value < 60)
                .Select(t => new WeakArea
                {
                    TopicName = t.Key,
                    AccuracyRate = t.Value,
                    QuestionsAttempted = answers.Count(a => a.Question.Topic.TopicName == t.Key),
                    CommonMistakes = new List<string> { "Cần ôn tập lại lý thuyết cơ bản" },
                    Recommendations = new List<string> { "Làm thêm bài tập cơ bản", "Xem lại video bài giảng" },
                    Priority = t.Value < 40 ? 1 : 2
                })
                .ToList();

            // Xác định điểm mạnh
            var strengths = accuracyByTopic
                .Where(t => t.Value >= 80)
                .Select(t => new Strength
                {
                    TopicName = t.Key,
                    AccuracyRate = t.Value,
                    QuestionsAttempted = answers.Count(a => a.Question.Topic.TopicName == t.Key),
                    ConsistencyLevel = t.Value >= 90 ? "Rất tốt" : "Tốt"
                })
                .ToList();

            // Lấy các bài thi gần đây
            var recentExams = await _context.StudentAttempts
                .Include(a => a.Exam)
                .Where(a => a.UserId == studentId)
                .OrderByDescending(a => a.EndTime)
                .Take(5)
                .Select(a => new RecentExam
                {
                    ExamId = a.ExamId,
                    ExamName = a.Exam.ExamName,
                    ExamType = a.Exam.ExamType,
                    Score = (double)a.TotalScore,
                    MaxScore = (double)(a.MaxScore ?? 100m),
                    Percentage = a.MaxScore > 0 ? (double)a.TotalScore / (double)a.MaxScore * 100 : 0,
                    CompletedAt = a.EndTime ?? DateTime.UtcNow,
                    TimeTaken = a.EndTime.HasValue ? a.EndTime.Value - a.StartTime : TimeSpan.Zero
                })
                .ToListAsync();

            // Phân tích xu hướng học tập
            var learningTrend = new LearningTrend
            {
                Trend = overallAccuracy >= 70 ? "Tích cực" : "Cần cải thiện",
                ProgressRate = 0, // Cần thêm dữ liệu theo thời gian để tính
                DataPoints = await _context.StudentAttempts
                    .Include(a => a.StudentAnswers)
                    .Where(a => a.UserId == studentId)
                    .OrderBy(a => a.EndTime)
                    .Select(a => new LearningDataPoint
                    {
                        Date = a.EndTime ?? DateTime.UtcNow,
                        AccuracyRate = a.MaxScore > 0 ? (double)a.TotalScore / (double)a.MaxScore * 100 : 0,
                        QuestionsAnswered = a.StudentAnswers.Count
                    })
                    .ToListAsync()
            };

            return new StudentPerformance
            {
                StudentId = studentId,
                StudentName = student.FullName,
                Email = student.Email,
                TotalQuestionsAttempted = totalQuestions,
                TotalCorrectAnswers = correctAnswers,
                OverallAccuracy = overallAccuracy,
                AccuracyByDifficulty = accuracyByDifficulty,
                AccuracyByTopic = accuracyByTopic,
                AccuracyByQuestionType = accuracyByQuestionType,
                WeakAreas = weakAreas,
                Strengths = strengths,
                RecentExams = recentExams,
                LearningTrend = learningTrend,
                LastActivity = answers.Any() 
                    ? answers.Max(a => a.Attempt.EndTime ?? DateTime.UtcNow)
                    : DateTime.UtcNow
            };
        }

        public async Task<List<CommonMistake>> GetCommonMistakesAsync(string questionId)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);

            if (question == null)
            {
                throw new ArgumentException("Không tìm thấy câu hỏi");
            }

            // Lấy AnswerChoices riêng biệt bằng projection để tránh NULL displayOrder
            var answerChoices = await _context.AnswerChoices
                .Where(ac => ac.QuestionId == questionId)
                .Select(ac => new
                {
                    ac.ChoiceId,
                    ac.ChoiceText,
                    ac.IsCorrect,
                    DisplayOrder = (int?)ac.DisplayOrder ?? 1 // Cast tới nullable để xử lý NULL từ database
                })
                .ToListAsync();

            // Lấy tất cả các lần trả lời sai cho câu hỏi này
            var incorrectAnswers = await _context.StudentAnswers
                .Where(a => a.QuestionId == questionId && !a.IsCorrect)
                .GroupBy(a => a.SelectedChoiceId)
                .Select(g => new
                {
                    ChoiceId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var totalAttempts = await _context.StudentAnswers
                .Where(a => a.QuestionId == questionId)
                .CountAsync();

            var commonMistakes = new List<CommonMistake>();

            foreach (var answer in incorrectAnswers)
            {
                var choice = answerChoices.FirstOrDefault(c => c.ChoiceId == answer.ChoiceId);
                if (choice != null)
                {
                    commonMistakes.Add(new CommonMistake
                    {
                        QuestionId = questionId,
                        WrongChoiceId = choice.ChoiceId,
                        WrongChoiceText = choice.ChoiceText,
                        TimesSelected = answer.Count,
                        SelectionRate = totalAttempts > 0 ? (double)answer.Count / totalAttempts * 100 : 0,
                        ReasonForMistake = "Học sinh có thể đã nhầm lẫn giữa các công thức hoặc hiểu sai yêu cầu của bài toán",
                        Correction = "Cần đọc kỹ đề bài và xem lại lý thuyết liên quan",
                        StudyTips = new List<string>
                        {
                            "Ôn tập lại lý thuyết cơ bản",
                            "Làm thêm bài tập tương tự",
                            "Xem lại video bài giảng"
                        }
                    });
                }
            }

            return commonMistakes.OrderByDescending(m => m.TimesSelected).ToList();
        }

        public async Task<ExamStatistics> GetExamStatisticsAsync(string examId)
        {
            var exam = await _context.Exams
                .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                .FirstOrDefaultAsync(e => e.ExamId == examId);

            if (exam == null)
            {
                throw new ArgumentException("Không tìm thấy bài thi");
            }

            var attempts = await _context.StudentAttempts
                .Where(a => a.ExamId == examId)
                .Include(a => a.StudentAnswers)
                .ToListAsync();

            if (!attempts.Any())
            {
                return new ExamStatistics
                {
                    ExamId = examId,
                    ExamName = exam.ExamName,
                    TotalAttempts = 0,
                    AverageScore = 0,
                    HighestScore = 0,
                    LowestScore = 0,
                    AverageCompletionTime = TimeSpan.Zero,
                    GradeDistribution = new Dictionary<string, int>(),
                    DifficultyAnalysis = new Dictionary<string, double>(),
                    QuestionPerformances = new List<QuestionPerformance>()
                };
            }

            // Tính toán thống kê cơ bản
            var scores = attempts.Select(a => (double)a.TotalScore / (double)(a.MaxScore ?? 100m) * 100).ToList();
            var averageScore = scores.Average();
            var highestScore = scores.Max();
            var lowestScore = scores.Min();

            // Tính thời gian hoàn thành trung bình
            var completionTimes = attempts
                .Where(a => a.EndTime.HasValue)
                .Select(a => a.EndTime.Value - a.StartTime)
                .ToList();
            var averageCompletionTime = completionTimes.Any() 
                ? TimeSpan.FromTicks((long)completionTimes.Average(t => t.Ticks))
                : TimeSpan.Zero;

            // Phân phối điểm số
            var gradeDistribution = new Dictionary<string, int>
            {
                { "A (90-100)", scores.Count(s => s >= 90) },
                { "B (80-89)", scores.Count(s => s >= 80 && s < 90) },
                { "C (70-79)", scores.Count(s => s >= 70 && s < 80) },
                { "D (60-69)", scores.Count(s => s >= 60 && s < 70) },
                { "F (0-59)", scores.Count(s => s < 60) }
            };

            // Phân tích độ khó
            var difficultyAnalysis = exam.ExamQuestions
                .GroupBy(eq => eq.Question.DifficultyLevel ?? "medium")
                .ToDictionary(
                    g => g.Key,
                    g => g.Count() > 0 
                        ? (double)attempts.SelectMany(a => a.StudentAnswers)
                            .Count(sa => sa.QuestionId == g.First().QuestionId && sa.IsCorrect) 
                            / (g.Count() * attempts.Count) * 100 
                        : 0
                );

            // Phân tích từng câu hỏi
            var questionPerformances = new List<QuestionPerformance>();
            foreach (var examQuestion in exam.ExamQuestions)
            {
                var answers = attempts
                    .SelectMany(a => a.StudentAnswers)
                    .Where(a => a.QuestionId == examQuestion.QuestionId);

                var totalAnswers = answers.Count();
                var correctAnswers = answers.Count(a => a.IsCorrect);
                var successRate = totalAnswers > 0 ? (double)correctAnswers / totalAnswers * 100 : 0;

                var performanceRating = successRate switch
                {
                    >= 80 => "Rất tốt",
                    >= 60 => "Tốt",
                    >= 40 => "Trung bình",
                    _ => "Cần cải thiện"
                };

                questionPerformances.Add(new QuestionPerformance
                {
                    QuestionId = examQuestion.QuestionId,
                    QuestionText = examQuestion.Question?.QuestionText ?? "",
                    SuccessRate = successRate,
                    DifficultyLevel = examQuestion.Question?.DifficultyLevel ?? "medium",
                    PerformanceRating = performanceRating
                });
            }

            return new ExamStatistics
            {
                ExamId = examId,
                ExamName = exam.ExamName,
                TotalAttempts = attempts.Count,
                AverageScore = averageScore,
                HighestScore = highestScore,
                LowestScore = lowestScore,
                AverageCompletionTime = averageCompletionTime,
                GradeDistribution = gradeDistribution,
                DifficultyAnalysis = difficultyAnalysis,
                QuestionPerformances = questionPerformances
            };
        }

        private string AnalyzeMistakeReason(Question question, AnswerChoice incorrectChoice)
        {
            // Logic phân tích lỗi dựa trên câu hỏi và lựa chọn sai
            return "Học sinh có thể đã nhầm lẫn giữa các công thức hoặc hiểu sai yêu cầu của bài toán.";
        }

        private string GetRecommendedMaterial(Question question)
        {
            // Đề xuất tài liệu học tập phù hợp
            return "Xem lại chương 3 - Động lực học chất điểm và các bài tập tương tự.";
        }

        public async Task<QuestionGradingResult> GradeEssayQuestionAsync(string questionId, string studentTextAnswer, string? studentUserId)
        {
            try
            {
                // Lấy thông tin câu hỏi essay
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    throw new ArgumentException("Không tìm thấy câu hỏi");
                }

                if (question.QuestionType?.ToLower() != "essay")
                {
                    throw new ArgumentException("Câu hỏi không phải dạng tự luận");
                }

                // 🎯 Tính điểm mỗi câu dựa trên tổng số câu trong đề
                var essayExamQuestion = await _context.ExamQuestions
                    .Include(eq => eq.Exam)
                    .FirstOrDefaultAsync(eq => eq.QuestionId == questionId);

                double maxPoints = 1.0; // Mặc định 1 điểm cho câu đơn lẻ
                if (essayExamQuestion?.Exam != null)
                {
                    var totalQuestionsInExam = await _context.ExamQuestions
                        .CountAsync(eq => eq.ExamId == essayExamQuestion.ExamId);
                    
                    if (totalQuestionsInExam > 0)
                    {
                        maxPoints = 10.0 / totalQuestionsInExam; // Chia 10 điểm cho tổng số câu
                    }
                }

                // Lấy thông tin từ AiGenerationMetadata nếu có
                var essayProperties = !string.IsNullOrEmpty(question.AiGenerationMetadata)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(question.AiGenerationMetadata)
                    : new Dictionary<string, object>();

                var sampleAnswer = question.Explanation ?? "";

                // Cố gắng lấy sample answer từ metadata (không override maxPoints vì đã tính theo số câu)
                if (essayProperties.ContainsKey("sampleAnswer"))
                {
                    sampleAnswer = essayProperties["sampleAnswer"].ToString();
                }

                // Phân tích cơ bản nội dung essay
                var wordCount = CountWords(studentTextAnswer);
                var minWords = essayProperties.ContainsKey("minWords") && int.TryParse(essayProperties["minWords"].ToString(), out int min) ? min : 50;
                var maxWords = essayProperties.ContainsKey("maxWords") && int.TryParse(essayProperties["maxWords"].ToString(), out int max) ? max : 500;

                // Tính điểm cơ bản dựa trên độ dài và từ khóa
                double pointsEarned = 0;
                var feedback = "";

                if (string.IsNullOrWhiteSpace(studentTextAnswer))
                {
                    feedback = "Chưa có câu trả lời";
                }
                else if (wordCount < minWords)
                {
                    pointsEarned = maxPoints * 0.3; // 30% điểm nếu quá ngắn
                    feedback = $"Câu trả lời quá ngắn ({wordCount} từ, cần tối thiểu {minWords} từ)";
                }
                else if (wordCount > maxWords)
                {
                    pointsEarned = maxPoints * 0.7; // 70% điểm nếu quá dài
                    feedback = $"Câu trả lời quá dài ({wordCount} từ, tối đa {maxWords} từ)";
                }
                else
                {
                    // Đánh giá cơ bản dựa trên từ khóa vật lý
                    var physicsKeywords = new[] { "lực", "gia tốc", "vận tốc", "động lượng", "năng lượng", "công suất", "điện", "từ trường", "sóng", "nhiệt" };
                    var foundKeywords = physicsKeywords.Count(keyword => studentTextAnswer.ToLower().Contains(keyword));

                    pointsEarned = Math.Min(maxPoints, maxPoints * (0.5 + (foundKeywords * 0.1))); // Base 50% + 10% per keyword
                    feedback = $"Câu trả lời có độ dài phù hợp ({wordCount} từ). Tìm thấy {foundKeywords} thuật ngữ vật lý quan trọng.";
                }

                // Lấy explanation từ database nếu có
                var explanationFromDb = await _context.Explanations
                    .FirstOrDefaultAsync(e => e.QuestionId == questionId);

                var explanationText = explanationFromDb?.ExplanationText 
                    ?? question.Explanation 
                    ?? (!string.IsNullOrEmpty(sampleAnswer) ? $"Gợi ý: {sampleAnswer}" : "Câu hỏi tự luận cần đánh giá chi tiết");

                var correctAnswerText = !string.IsNullOrEmpty(sampleAnswer) 
                    ? sampleAnswer 
                    : (explanationFromDb?.ExplanationText ?? question.Explanation ?? "Câu hỏi tự luận - xem hướng dẫn chi tiết");

                var result = new QuestionGradingResult
                {
                    QuestionId = questionId,
                    CorrectChoiceId = "", // Essay không có choice ID
                    CorrectChoiceLabel = "", // Essay không có choice label  
                    CorrectChoiceText = correctAnswerText,
                    StudentChoiceId = "", // Essay không có choice ID
                    StudentChoiceLabel = "", // Essay không có choice label
                    StudentChoiceText = studentTextAnswer ?? "", // Đảm bảo không null
                    IsCorrect = pointsEarned >= (maxPoints * 0.6), // Đạt 60% trở lên coi là đúng
                    PointsEarned = pointsEarned,
                    MaxPoints = maxPoints,
                    Feedback = feedback ?? "",
                    Explanation = explanationText,
                    DifficultyLevel = question.DifficultyLevel ?? "medium",
                    QuestionType = "essay",
                    GradedAt = DateTime.UtcNow
                };

                // Debug: Log kết quả essay grading
                _logger.LogInformation("Debug Essay Grading - Question {QuestionId}: StudentAnswer={StudentAnswer}, CorrectAnswer={CorrectAnswer}, Points={Points}/{MaxPoints}", 
                    questionId, 
                    studentTextAnswer?.Substring(0, Math.Min(50, studentTextAnswer?.Length ?? 0)) + "...",
                    correctAnswerText?.Substring(0, Math.Min(50, correctAnswerText.Length)) + "...",
                    pointsEarned, maxPoints);

                // Lưu câu trả lời essay vào database
                if (!string.IsNullOrEmpty(studentUserId))
                {
                    if (essayExamQuestion != null)
                    {
                        var attempt = new StudentAttempt
                        {
                            AttemptId = Guid.NewGuid().ToString(),
                            UserId = studentUserId,
                            ExamId = essayExamQuestion.ExamId,
                            StartTime = DateTime.UtcNow,
                            EndTime = DateTime.UtcNow,
                            TotalScore = Convert.ToDecimal(pointsEarned),
                            MaxScore = 10.0m, // 🎯 Sử dụng thang điểm 10
                            Status = "completed"
                        };
                        _context.StudentAttempts.Add(attempt);

                        var studentAnswer = new StudentAnswer
                        {
                            AnswerId = Guid.NewGuid().ToString(),
                            AttemptId = attempt.AttemptId,
                            QuestionId = questionId,
                            SelectedChoiceId = null,
                            StudentTextAnswer = studentTextAnswer,
                            IsCorrect = result.IsCorrect,
                            PointsEarned = Convert.ToDecimal(pointsEarned),
                            AnsweredAt = DateTime.UtcNow
                        };

                        _context.StudentAnswers.Add(studentAnswer);
                        await _context.SaveChangesAsync();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm điểm câu hỏi tự luận {QuestionId}", questionId);

                // Return fallback result
                return new QuestionGradingResult
                {
                    QuestionId = questionId,
                    CorrectChoiceText = "Lỗi hệ thống",
                    StudentChoiceText = studentTextAnswer,

                    IsCorrect = false,
                    PointsEarned = 0,
                    MaxPoints = 1.0,
                    Feedback = "Có lỗi xảy ra khi chấm điểm",
                    QuestionType = "essay",
                    GradedAt = DateTime.UtcNow
                };
            }
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Trim().Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
} 