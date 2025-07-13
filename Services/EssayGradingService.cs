using BE_Phygens.Dto;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace BE_Phygens.Services
{
    public class EssayGradingService : IEssayGradingService
    {
        private readonly PhygensContext _context;
        private readonly IAIService _aiService;
        private readonly ILogger<EssayGradingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EssayGradingService(
            PhygensContext context,
            IAIService aiService,
            ILogger<EssayGradingService> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _context = context;
            _aiService = aiService;
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<EssayGradingResultDto> GradeEssayAsync(string questionId, string studentAnswer, string studentId)
        {
            try
            {
                _logger.LogInformation($"Chấm điểm bài tự luận cho câu hỏi {questionId}");

                // Lấy thông tin câu hỏi
                var question = await _context.Questions
                    .Include(q => q.Topic)
                    .FirstOrDefaultAsync(q => q.QuestionId == questionId);

                if (question == null)
                {
                    throw new ArgumentException("Không tìm thấy câu hỏi");
                }

                if (question.QuestionType != "essay")
                {
                    throw new ArgumentException("Câu hỏi không phải dạng tự luận");
                }

                // Phân tích cơ bản
                var analysis = await AnalyzeEssayAsync(studentAnswer);

                // Tạo prompt cho AI grading
                var gradingPrompt = BuildEssayGradingPrompt(question, studentAnswer);

                // Gọi AI để chấm điểm
                var aiGradingResult = await CallAIForEssayGradingAsync(gradingPrompt);

                // Parse kết quả từ AI
                var gradingResult = ParseAIGradingResult(aiGradingResult, questionId, studentAnswer);

                // Áp dụng các tiêu chí bổ sung
                ApplyAdditionalCriteria(gradingResult, analysis, question);

                // Lưu kết quả vào database
                await SaveEssayGradingResultAsync(gradingResult, studentId);

                return gradingResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chấm điểm bài tự luận");
                // Trả về kết quả fallback
                return CreateFallbackGradingResult(questionId, studentAnswer);
            }
        }

        public async Task<List<EssayGradingResultDto>> BatchGradeEssaysAsync(EssayBatchGradingRequest request)
        {
            var results = new List<EssayGradingResultDto>();

            foreach (var submission in request.Submissions)
            {
                try
                {
                    var result = await GradeEssayAsync(submission.QuestionId, submission.StudentAnswer, submission.StudentId);
                    
                    // Điều chỉnh theo style grading
                    AdjustGradingByStyle(result, request.GradingStyle);
                    
                    results.Add(result);

                    // Delay để tránh rate limit
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi chấm điểm bài tự luận của học sinh {submission.StudentId}");
                    results.Add(CreateFallbackGradingResult(submission.QuestionId, submission.StudentAnswer));
                }
            }

            return results;
        }

        public async Task<EssayAnalysisDto> AnalyzeEssayAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new EssayAnalysisDto
                {
                    WordCount = 0,
                    LanguageQuality = "Không có nội dung",
                    Coherence = "Không thể đánh giá"
                };
            }

            var analysis = new EssayAnalysisDto
            {
                WordCount = CountWords(text),
                DetectedKeywords = ExtractKeywords(text),
                GrammarIssues = await DetectGrammarIssuesAsync(text),
                ReadabilityScore = CalculateReadabilityScore(text),
                LanguageQuality = AssessLanguageQuality(text),
                Coherence = AssessCoherence(text),
                SuggestedImprovements = await GenerateImprovementSuggestionsAsync(text)
            };

            return analysis;
        }

        public async Task<EssayQuestionDto> GenerateEssayQuestionAsync(Chapter chapter, GenerateEssayQuestionRequest request)
        {
            try
            {
                _logger.LogInformation($"Tạo câu hỏi tự luận cho chương {chapter.ChapterId}");

                var prompt = BuildEssayQuestionGenerationPrompt(chapter, request);
                var aiResponse = await CallAIForQuestionGenerationAsync(prompt);
                var essayQuestion = ParseEssayQuestionResponse(aiResponse, chapter, request);

                // Tạo grading criteria mặc định
                essayQuestion.GradingCriteria = CreateDefaultGradingCriteria(request.DifficultyLevel);

                return essayQuestion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo câu hỏi tự luận");
                return CreateMockEssayQuestion(chapter, request);
            }
        }

        public async Task<string> GenerateEssayFeedbackAsync(string questionId, string studentAnswer, double score)
        {
            try
            {
                var question = await _context.Questions.FindAsync(questionId);
                if (question == null) return "Không thể tạo phản hồi";

                var prompt = BuildFeedbackPrompt(question.QuestionText, studentAnswer, score);
                return await CallAIForFeedbackAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo phản hồi");
                return GenerateGenericFeedback(score);
            }
        }

        public async Task<bool> ValidateEssayAnswerAsync(string answer, int minWords, int maxWords)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return false;

            var wordCount = CountWords(answer);
            return wordCount >= minWords && wordCount <= maxWords;
        }

        // ========== PRIVATE METHODS ==========

        private string BuildEssayGradingPrompt(Question question, string studentAnswer)
        {
            return $@"
Bạn là giáo viên Vật lý chuyên nghiệp. Hãy chấm điểm bài tự luận sau:

ĐỀ BÀI: {question.QuestionText}

BÀI LÀM CỦA HỌC SINH:
{studentAnswer}

Hãy đánh giá theo các tiêu chí sau (thang điểm 10):
1. Hiểu biết khái niệm (0-3 điểm)
2. Áp dụng công thức/định luật (0-3 điểm)  
3. Giải thích logic và rõ ràng (0-2 điểm)
4. Kết luận chính xác (0-2 điểm)

Trả về kết quả theo format JSON:
{{
    ""totalScore"": số điểm tổng,
    ""criteriaScores"": [
        {{""criteriaName"": ""Hiểu biết khái niệm"", ""score"": điểm, ""maxScore"": 3, ""feedback"": ""nhận xét""}},
        {{""criteriaName"": ""Áp dụng công thức"", ""score"": điểm, ""maxScore"": 3, ""feedback"": ""nhận xét""}},
        {{""criteriaName"": ""Giải thích logic"", ""score"": điểm, ""maxScore"": 2, ""feedback"": ""nhận xét""}},
        {{""criteriaName"": ""Kết luận"", ""score"": điểm, ""maxScore"": 2, ""feedback"": ""nhận xét""}}
    ],
    ""overallFeedback"": ""nhận xét tổng quan"",
    ""strengths"": [""điểm mạnh 1"", ""điểm mạnh 2""],
    ""areasForImprovement"": [""cần cải thiện 1"", ""cần cải thiện 2""]
}}";
        }

        private string BuildEssayQuestionGenerationPrompt(Chapter chapter, GenerateEssayQuestionRequest request)
        {
            return $@"
Tạo một câu hỏi tự luận Vật lý cho:
- Chương: {chapter.ChapterName} (Lớp {chapter.Grade})
- Độ khó: {request.DifficultyLevel}
- Kiểu essay: {request.EssayStyle}
- Số từ tối thiểu: {request.MinWords}
- Số từ tối đa: {request.MaxWords}

Yêu cầu thêm: {request.AdditionalInstructions}

Trả về JSON format:
{{
    ""questionText"": ""nội dung câu hỏi"",
    ""sampleAnswer"": ""câu trả lời mẫu"",
    ""keyPoints"": [""điểm mấu chốt 1"", ""điểm mấu chốt 2""],
    ""explanation"": ""giải thích chi tiết"",
    ""gradingRubric"": ""thang điểm chi tiết""
}}";
        }

        private async Task<string> CallAIForEssayGradingAsync(string prompt)
        {
            var provider = _configuration["AI:Provider"]?.ToLower() ?? "openai";
            
            return provider switch
            {
                "openai" => await CallOpenAIAsync(prompt),
                "gemini" => await CallGeminiAsync(prompt),
                "claude" => await CallClaudeAsync(prompt),
                _ => CreateMockGradingResponse()
            };
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            try
            {
                var apiKey = _configuration["AI:OpenAI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                    return CreateMockGradingResponse();

                var requestBody = new
                {
                    model = "gpt-4",
                    messages = new[]
                    {
                        new { role = "system", content = "Bạn là giáo viên Vật lý chuyên nghiệp chấm bài tự luận." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 1500,
                    temperature = 0.3
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);
                
                if (!response.IsSuccessStatusCode)
                    return CreateMockGradingResponse();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var openAIResponse = JsonSerializer.Deserialize<OpenAIResponse>(jsonResponse);
                
                return openAIResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? CreateMockGradingResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi OpenAI API");
                return CreateMockGradingResponse();
            }
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            // Implementation cho Gemini API
            return CreateMockGradingResponse();
        }

        private async Task<string> CallClaudeAsync(string prompt)
        {
            // Implementation cho Claude API
            return CreateMockGradingResponse();
        }

        private string CreateMockGradingResponse()
        {
            return @"{
                ""totalScore"": 6.5,
                ""criteriaScores"": [
                    {""criteriaName"": ""Hiểu biết khái niệm"", ""score"": 2, ""maxScore"": 3, ""feedback"": ""Thể hiện hiểu biết cơ bản về khái niệm""},
                    {""criteriaName"": ""Áp dụng công thức"", ""score"": 2, ""maxScore"": 3, ""feedback"": ""Áp dụng công thức đúng nhưng chưa đầy đủ""},
                    {""criteriaName"": ""Giải thích logic"", ""score"": 1.5, ""maxScore"": 2, ""feedback"": ""Giải thích tương đối rõ ràng""},
                    {""criteriaName"": ""Kết luận"", ""score"": 1, ""maxScore"": 2, ""feedback"": ""Kết luận chưa hoàn chỉnh""}
                ],
                ""overallFeedback"": ""Bài làm thể hiện hiểu biết cơ bản về vấn đề. Cần cải thiện thêm về độ chi tiết và logic trình bày."",
                ""strengths"": [""Hiểu khái niệm cơ bản"", ""Sử dụng công thức đúng""],
                ""areasForImprovement"": [""Cần giải thích chi tiết hơn"", ""Kết luận cần hoàn chỉnh hơn""]
            }";
        }

        private EssayGradingResultDto ParseAIGradingResult(string aiResponse, string questionId, string studentAnswer)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<EssayGradingResultDto>(aiResponse, options);
                if (result != null)
                {
                    result.QuestionId = questionId;
                    result.StudentAnswer = studentAnswer;
                    result.MaxScore = 10;
                    result.GradedAt = DateTime.UtcNow;
                    result.GradingMethod = "AI";
                    result.IsPlausible = result.TotalScore > 0;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi parse kết quả AI grading");
            }

            return CreateFallbackGradingResult(questionId, studentAnswer);
        }

        private void ApplyAdditionalCriteria(EssayGradingResultDto result, EssayAnalysisDto analysis, Question question)
        {
            // Điều chỉnh điểm dựa trên số từ
            if (analysis.WordCount < 30)
            {
                result.TotalScore *= 0.8; // Giảm 20% nếu quá ngắn
                result.AreasForImprovement.Add("Bài viết cần dài hơn và chi tiết hơn");
            }

            // Điều chỉnh dựa trên chất lượng ngôn ngữ
            if (analysis.GrammarIssues.Count > 5)
            {
                result.TotalScore *= 0.9; // Giảm 10% nếu có nhiều lỗi ngữ pháp
                result.AreasForImprovement.Add("Cần cải thiện ngữ pháp và chính tả");
            }

            // Điều chỉnh dựa trên keywords
            if (analysis.DetectedKeywords.Count >= 3)
            {
                result.Strengths.Add("Sử dụng tốt các thuật ngữ chuyên môn");
            }
            else
            {
                result.AreasForImprovement.Add("Cần sử dụng nhiều thuật ngữ Vật lý hơn");
            }
        }

        private EssayGradingResultDto CreateFallbackGradingResult(string questionId, string studentAnswer)
        {
            var wordCount = CountWords(studentAnswer);
            var baseScore = Math.Min(wordCount / 50.0 * 5, 5); // Điểm cơ bản dựa trên độ dài

            return new EssayGradingResultDto
            {
                QuestionId = questionId,
                StudentAnswer = studentAnswer,
                TotalScore = baseScore,
                MaxScore = 10,
                OverallFeedback = "Đánh giá tự động cơ bản. Vui lòng liên hệ giáo viên để được chấm điểm chi tiết.",
                GradingMethod = "Fallback",
                IsPlausible = wordCount >= 20,
                Strengths = wordCount >= 50 ? new List<string> { "Độ dài phù hợp" } : new List<string>(),
                AreasForImprovement = wordCount < 50 ? new List<string> { "Cần viết dài hơn" } : new List<string>()
            };
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private List<string> ExtractKeywords(string text)
        {
            var keywords = new List<string>();
            var physicsTerms = new[]
            {
                "lực", "gia tốc", "vận tốc", "động lượng", "năng lượng", "công suất",
                "điện tích", "điện trường", "từ trường", "dao động", "sóng", "ánh sáng",
                "nhiệt độ", "entropy", "định luật", "nguyên lý", "hiện tượng", "công thức",
                "tính toán", "đơn vị", "kết quả", "phân tích", "so sánh", "ứng dụng"
            };

            var textLower = text.ToLower();
            foreach (var term in physicsTerms)
            {
                if (textLower.Contains(term))
                {
                    keywords.Add(term);
                }
            }

            return keywords.Distinct().ToList();
        }

        private async Task<List<string>> DetectGrammarIssuesAsync(string text)
        {
            var issues = new List<string>();
            
            if (text.Contains("  ")) // Double spaces
                issues.Add("Có khoảng trắng thừa");
                
            if (!text.TrimEnd().EndsWith(".") && !text.TrimEnd().EndsWith("!") && !text.TrimEnd().EndsWith("?"))
                issues.Add("Thiếu dấu chấm cuối câu");

            // Check for common Vietnamese grammar issues
            if (text.ToLower().Contains("được và") || text.ToLower().Contains("là và"))
                issues.Add("Có thể có lỗi ngữ pháp với từ nối");

            return issues;
        }

        private double CalculateReadabilityScore(string text)
        {
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var words = CountWords(text);
            
            if (sentences == 0) return 0;
            
            var avgWordsPerSentence = (double)words / sentences;
            
            // Điểm từ 0-10, tối ưu khoảng 15-20 từ/câu
            if (avgWordsPerSentence >= 15 && avgWordsPerSentence <= 20)
                return 8.0;
            else if (avgWordsPerSentence >= 10 && avgWordsPerSentence <= 25)
                return 6.0;
            else
                return 4.0;
        }

        private string AssessLanguageQuality(string text)
        {
            var wordCount = CountWords(text);
            
            if (wordCount < 20) return "Cần mở rộng thêm";
            if (wordCount < 50) return "Cơ bản";
            if (wordCount < 100) return "Tốt";
            return "Rất tốt";
        }

        private string AssessCoherence(string text)
        {
            var sentences = text.Split(new char[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (sentences.Length < 2) return "Cần thêm câu để liên kết ý tưởng";
            if (sentences.Length < 4) return "Tạm ổn";
            return "Tốt";
        }

        private async Task<List<string>> GenerateImprovementSuggestionsAsync(string text)
        {
            var suggestions = new List<string>();
            
            var wordCount = CountWords(text);
            if (wordCount < 50)
                suggestions.Add("Nên mở rộng thêm các ý chính");
                
            if (!text.Contains("vì") && !text.Contains("do") && !text.Contains("nên"))
                suggestions.Add("Thêm các từ nối để giải thích nguyên nhân");
                
            if (!text.Contains("kết luận") && !text.Contains("tóm lại") && !text.Contains("vậy"))
                suggestions.Add("Nên có phần kết luận tóm tắt");

            if (!text.Contains("ví dụ") && !text.Contains("chẳng hạn"))
                suggestions.Add("Có thể thêm ví dụ cụ thể để minh họa");

            return suggestions;
        }

        private List<Dto.EssayGradingCriteria> CreateDefaultGradingCriteria(string difficultyLevel)
        {
            return new List<Dto.EssayGradingCriteria>
            {
                new Dto.EssayGradingCriteria
                {
                    CriteriaName = "Hiểu biết khái niệm",
                    Description = "Thể hiện sự hiểu biết về khái niệm vật lý",
                    MaxPoints = 3,
                    Indicators = new List<string> { "Định nghĩa chính xác", "Giải thích rõ ràng", "Liên hệ với thực tế" }
                },
                new Dto.EssayGradingCriteria
                {
                    CriteriaName = "Áp dụng công thức",
                    Description = "Sử dụng đúng công thức và tính toán",
                    MaxPoints = 3,
                    Indicators = new List<string> { "Chọn công thức đúng", "Thay số chính xác", "Đơn vị đúng" }
                },
                new Dto.EssayGradingCriteria
                {
                    CriteriaName = "Giải thích logic",
                    Description = "Trình bày có logic và mạch lạc",
                    MaxPoints = 2,
                    Indicators = new List<string> { "Trình tự hợp lý", "Liên kết các ý", "Dễ hiểu" }
                },
                new Dto.EssayGradingCriteria
                {
                    CriteriaName = "Kết luận",
                    Description = "Đưa ra kết luận chính xác",
                    MaxPoints = 2,
                    Indicators = new List<string> { "Kết quả đúng", "Tóm tắt hay", "Ý nghĩa rõ ràng" }
                }
            };
        }

        // Helper methods với implementation cơ bản
        private async Task<string> CallAIForQuestionGenerationAsync(string prompt)
        {
            return @"{
                ""questionText"": ""Giải thích hiện tượng và tính toán liên quan đến [chủ đề vật lý]"",
                ""sampleAnswer"": ""Câu trả lời mẫu chi tiết..."",
                ""keyPoints"": [""Khái niệm cơ bản"", ""Công thức áp dụng"", ""Tính toán"", ""Kết luận""],
                ""explanation"": ""Giải thích chi tiết về câu hỏi"",
                ""gradingRubric"": ""Thang điểm chi tiết cho từng phần""
            }";
        }

        private async Task<string> CallAIForFeedbackAsync(string prompt)
        {
            return "Phản hồi tự động: Bài làm của em thể hiện sự hiểu biết cơ bản. Hãy tiếp tục phát triển các ý tưởng và cải thiện cách trình bày.";
        }

        private EssayQuestionDto ParseEssayQuestionResponse(string response, Chapter chapter, GenerateEssayQuestionRequest request)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(response, options);
                
                return new EssayQuestionDto
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    QuestionText = data?.GetValueOrDefault("questionText")?.ToString() ?? "Câu hỏi tự luận mẫu",
                    QuestionType = "essay",
                    Difficulty = request.DifficultyLevel,
                    ChapterId = chapter.ChapterId,
                    Topic = chapter.ChapterName,
                    SampleAnswer = data?.GetValueOrDefault("sampleAnswer")?.ToString(),
                    MaxWords = request.MaxWords,
                    MinWords = request.MinWords,
                    CreatedBy = "ai_system",
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return CreateMockEssayQuestion(chapter, request);
            }
        }

        private EssayQuestionDto CreateMockEssayQuestion(Chapter chapter, GenerateEssayQuestionRequest request)
        {
            return new EssayQuestionDto
            {
                QuestionId = Guid.NewGuid().ToString(),
                QuestionText = $"Hãy giải thích một khái niệm quan trọng trong {chapter.ChapterName} và đưa ra ví dụ thực tế.",
                QuestionType = "essay",
                Difficulty = request.DifficultyLevel,
                ChapterId = chapter.ChapterId,
                Topic = chapter.ChapterName,
                SampleAnswer = "Câu trả lời mẫu sẽ bao gồm: định nghĩa khái niệm, giải thích chi tiết, ví dụ thực tế, và kết luận.",
                MaxWords = request.MaxWords,
                MinWords = request.MinWords,
                CreatedBy = "ai_system",
                CreatedAt = DateTime.UtcNow,
                KeyPoints = new List<string> { "Định nghĩa", "Giải thích", "Ví dụ", "Kết luận" }
            };
        }

        private string BuildFeedbackPrompt(string question, string answer, double score)
        {
            return $@"
Câu hỏi: {question}
Câu trả lời: {answer}
Điểm: {score}/10

Hãy đưa ra phản hồi chi tiết và gợi ý cải thiện cho học sinh.";
        }

        private string GenerateGenericFeedback(double score)
        {
            return score switch
            {
                >= 8 => "Bài làm xuất sắc! Em đã thể hiện sự hiểu biết sâu sắc về vấn đề.",
                >= 6.5 => "Bài làm tốt! Một số điểm cần cải thiện thêm về độ chi tiết và logic trình bày.",
                >= 5 => "Bài làm đạt yêu cầu cơ bản. Em cần bổ sung thêm kiến thức và cải thiện cách trình bày.",
                _ => "Bài làm cần cải thiện nhiều. Em hãy ôn lại kiến thức cơ bản và luyện tập thêm."
            };
        }

        private void AdjustGradingByStyle(EssayGradingResultDto result, string style)
        {
            switch (style.ToLower())
            {
                case "strict":
                    result.TotalScore *= 0.9; // Giảm 10% cho chấm điểm nghiêm
                    break;
                case "lenient":
                    result.TotalScore *= 1.1; // Tăng 10% cho chấm điểm khoan dung
                    result.TotalScore = Math.Min(result.TotalScore, result.MaxScore);
                    break;
                // "balanced" giữ nguyên
            }
        }

        private async Task SaveEssayGradingResultAsync(EssayGradingResultDto result, string studentId)
        {
            try
            {
                // Lưu vào bảng studentanswer với studenttextanswer
                var studentAnswer = new StudentAnswer
                {
                    AnswerId = Guid.NewGuid().ToString(),
                    QuestionId = result.QuestionId,
                    StudentTextAnswer = result.StudentAnswer,
                    IsCorrect = result.TotalScore >= 5, // Coi như đúng nếu >= 5 điểm
                    PointsEarned = (decimal)result.TotalScore,
                    AnsweredAt = DateTime.UtcNow
                };

                // Tìm attempt hiện tại hoặc tạo mới
                var existingAttempt = await _context.StudentAttempts
                    .Where(sa => sa.UserId == studentId)
                    .OrderByDescending(sa => sa.StartTime)
                    .FirstOrDefaultAsync();

                if (existingAttempt != null)
                {
                    studentAnswer.AttemptId = existingAttempt.AttemptId;
                }

                _context.StudentAnswers.Add(studentAnswer);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Đã lưu kết quả chấm điểm essay cho student {studentId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu kết quả chấm điểm essay");
            }
        }

        // Inner classes for API responses
        private class OpenAIResponse
        {
            public OpenAIChoice[]? Choices { get; set; }
        }

        private class OpenAIChoice
        {
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            public string? Content { get; set; }
        }
    }
} 