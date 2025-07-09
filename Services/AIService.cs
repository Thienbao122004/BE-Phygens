using BE_Phygens.Dto;
using BE_Phygens.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Services
{
    public class AIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<AIService> _logger;
        private readonly IMemoryCache _cache;
        private readonly PhygensContext _context;

        public AIService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<AIService> logger,
            IMemoryCache cache,
            PhygensContext context)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;
            _context = context;
        }

        public async Task<QuestionDto> GenerateQuestionAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";

                // Check cache first
                var cacheKey = $"ai_question_{chapter.ChapterId}_{request.DifficultyLevel}_{request.QuestionType}";
                if (_cache.TryGetValue(cacheKey, out QuestionDto? cachedQuestion) && cachedQuestion != null)
                {
                    _logger.LogInformation("Returning cached AI question");
                    return cachedQuestion;
                }

                QuestionDto question = provider switch
                {
                    "openai" => await GenerateWithOpenAIAsync(chapter, request),
                    "gemini" => await GenerateWithGeminiAsync(chapter, request),
                    "claude" => await GenerateWithClaudeAsync(chapter, request),
                    "groq" => await GenerateWithGroqAsync(chapter, request),
                    "huggingface" => await GenerateWithHuggingFaceAsync(chapter, request),
                    "togetherai" => await GenerateWithTogetherAIAsync(chapter, request),
                    "openrouter" => await GenerateWithOpenRouterAsync(chapter, request),
                    _ => CreateMockQuestion(chapter, request)
                };

                // Cache the result
                var cacheExpiry = TimeSpan.FromMinutes(_configuration.GetValue<int>("AI:CacheDurationMinutes", 60));
                _cache.Set(cacheKey, question, cacheExpiry);

                return question;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI question");

                // Fallback to mock if enabled
                if (_configuration.GetValue<bool>("AI:FallbackToMock", true))
                {
                    return CreateMockQuestion(chapter, request);
                }

                throw;
            }
        }

        public async Task<List<QuestionDto>> GenerateBatchQuestionsAsync(List<QuestionSpecification> specs)
        {
            var questions = new List<QuestionDto>();
            var delay = _configuration.GetValue<int>("AI:BatchDelayMs", 1000);

            foreach (var spec in specs)
            {
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == spec.ChapterId && c.IsActive);

                if (chapter == null) continue;

                for (int i = 0; i < spec.Count; i++)
                {
                    var request = new GenerateQuestionRequest
                    {
                        ChapterId = spec.ChapterId,
                        DifficultyLevel = spec.DifficultyLevel,
                        QuestionType = spec.QuestionType,
                        SpecificTopic = spec.SpecificTopic
                    };

                    var question = await GenerateQuestionAsync(chapter, request);
                    questions.Add(question);

                    // Rate limiting
                    if (i < spec.Count - 1)
                    {
                        await Task.Delay(delay);
                    }
                }
            }

            return questions;
        }

        public async Task<QuestionDto> ImproveQuestionAsync(Question existingQuestion, ImproveQuestionRequest request)
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";
                var prompt = BuildImprovementPrompt(existingQuestion, request);

                var improvedContent = provider switch
                {
                    "openai" => await CallOpenAIAsync(prompt),
                    "gemini" => await CallGeminiAsync(prompt),
                    "claude" => await CallClaudeAsync(prompt),
                    _ => "Improved question content (mock)"
                };

                return ParseImprovedQuestion(improvedContent, existingQuestion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error improving question with AI");
                throw;
            }
        }

        public async Task<QuestionValidationDto> ValidateQuestionAsync(Question question)
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";
                var prompt = BuildValidationPrompt(question);

                var validationResult = provider switch
                {
                    "openai" => await CallOpenAIAsync(prompt),
                    "gemini" => await CallGeminiAsync(prompt),
                    "claude" => await CallClaudeAsync(prompt),
                    _ => CreateMockValidation()
                };

                return ParseValidationResult(validationResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating question with AI");
                throw;
            }
        }

        public async Task<List<TopicSuggestionDto>> GetTopicSuggestionsAsync(Chapter chapter, TopicSuggestionRequest request)
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";
                var prompt = BuildTopicSuggestionPrompt(chapter, request);

                var suggestionsText = provider switch
                {
                    "openai" => await CallOpenAIAsync(prompt),
                    "gemini" => await CallGeminiAsync(prompt),
                    "claude" => await CallClaudeAsync(prompt),
                    _ => ""
                };

                var suggestions = string.IsNullOrEmpty(suggestionsText)
                    ? CreateMockTopicSuggestions(chapter)
                    : ParseTopicSuggestions(suggestionsText);

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting topic suggestions from AI");
                return CreateMockTopicSuggestions(chapter);
            }
        }

        public async Task<string> GenerateExplanationAsync(Question question, string correctAnswer)
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";
                var prompt = BuildExplanationPrompt(question, correctAnswer);

                return provider switch
                {
                    "openai" => await CallOpenAIAsync(prompt),
                    "gemini" => await CallGeminiAsync(prompt),
                    "claude" => await CallClaudeAsync(prompt),
                    _ => $"Giải thích chi tiết cho câu hỏi: {question.QuestionText}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating explanation with AI");
                return "Không thể tạo giải thích tự động.";
            }
        }

        public async Task<List<QuestionDto>> GetAdaptiveQuestionsAsync(string studentId, int chapterId, int count)
        {
            try
            {
                // Analyze student performance
                var performance = await AnalyzeStudentPerformance(studentId, chapterId);

                // Generate adaptive questions based on performance
                var chapter = await _context.Set<Chapter>()
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null) return new List<QuestionDto>();

                var questions = new List<QuestionDto>();
                for (int i = 0; i < count; i++)
                {
                    var difficulty = DetermineAdaptiveDifficulty(performance, i);
                    var request = new GenerateQuestionRequest
                    {
                        ChapterId = chapterId,
                        DifficultyLevel = difficulty,
                        QuestionType = "multiple_choice",
                        AdditionalInstructions = $"Câu hỏi thích ứng cho học sinh có điểm trung bình {performance.AverageScore:F1}"
                    };

                    var question = await GenerateQuestionAsync(chapter, request);
                    questions.Add(question);
                }

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating adaptive questions");
                return new List<QuestionDto>();
            }
        }

        public async Task<GeneratedExamDto> GenerateSmartExamAsync(SmartExamGenerationRequest request)
        {
            try
            {
                var examId = Guid.NewGuid().ToString();
                var questions = new List<ExamQuestionDto>();
                int questionOrder = 1;

                foreach (var chapterReq in request.ChapterRequirements)
                {
                    var chapter = await _context.Set<Chapter>()
                        .FirstOrDefaultAsync(c => c.ChapterId == chapterReq.ChapterId);

                    if (chapter == null) continue;

                    // Distribute questions by difficulty
                    var distribution = CalculateDifficultyDistribution(chapterReq.QuestionCount, request.DifficultyDistribution);

                    foreach (var (difficulty, count) in distribution)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            var questionRequest = new GenerateQuestionRequest
                            {
                                ChapterId = chapterReq.ChapterId,
                                DifficultyLevel = difficulty,
                                QuestionType = "multiple_choice",
                                AdditionalInstructions = request.UseAIGeneration ? "Sử dụng AI generation" : "Sử dụng câu hỏi có sẵn"
                            };

                            var question = await GenerateQuestionAsync(chapter, questionRequest);

                            questions.Add(new ExamQuestionDto
                            {
                                ExamQuestionId = Guid.NewGuid().ToString(),
                                QuestionId = question.QuestionId,
                                QuestionOrder = questionOrder++,
                                PointsWeight = CalculateQuestionPoints(difficulty),
                                Question = question
                            });
                        }
                    }
                }

                return new GeneratedExamDto
                {
                    ExamId = examId,
                    ExamName = request.ExamName,
                    Description = request.Description,
                    Duration = request.DurationMinutes,
                    ExamType = request.ExamType,
                    TotalQuestions = questions.Count,
                    TotalPoints = questions.Sum(q => q.PointsWeight),
                    Questions = questions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating smart exam");
                throw;
            }
        }

        public async Task<AIConfigDto> GetAIStatusAsync()
        {
            var provider = _configuration["AI:Provider"] ?? "OpenAI";
            var isConfigured = !string.IsNullOrEmpty(_configuration[$"AI:{provider}:ApiKey"]);

            var usage = await GetUsageStatistics();

            return new AIConfigDto
            {
                Provider = provider,
                Model = _configuration[$"AI:{provider}:Model"] ?? "gpt-3.5-turbo",
                MaxTokens = _configuration.GetValue<int>($"AI:{provider}:MaxTokens", 2048),
                Temperature = _configuration.GetValue<double>($"AI:{provider}:Temperature", 0.7),
                IsConfigured = isConfigured,
                RateLimit = _configuration.GetValue<int>("AI:RateLimit", 60),
                DailyQuota = _configuration.GetValue<int>("AI:DailyQuota", 1000),
                UsedToday = usage.QuestionsGeneratedToday,
                LastUsed = DateTime.UtcNow,
                SupportedFeatures = GetSupportedFeatures(provider),
                Usage = usage
            };
        }

        public async Task<bool> TestAIConnectionAsync()
        {
            try
            {
                var provider = _configuration["AI:Provider"]?.ToLower() ?? "mock";
                var testPrompt = "Hi";

                _logger.LogInformation($"Testing AI connection with provider: {provider}");

                string response = provider switch
                {
                    "openai" => await CallOpenAIAsync(testPrompt),
                    "gemini" => await CallGeminiAsync(testPrompt),
                    "claude" => await CallClaudeAsync(testPrompt),
                    "groq" => await CallGroqAsync(testPrompt),
                    "huggingface" => await CallHuggingFaceAsync(testPrompt),
                    "togetherai" => await CallTogetherAIAsync(testPrompt),
                    "openrouter" => await CallOpenRouterAsync(testPrompt),
                    _ => throw new InvalidOperationException($"❌ AI PROVIDER KHÔNG HỖ TRỢ: {provider}")
                };

                _logger.LogInformation($"{provider} response received: '{response}' (Length: {response?.Length ?? 0})");

                var isSuccess = !string.IsNullOrEmpty(response) && response.Trim().Length > 0;
                _logger.LogInformation($"{provider} connection test result: {isSuccess}");

                return isSuccess;
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("429") || httpEx.Message.Contains("Too Many Requests"))
            {
                _logger.LogWarning("AI API rate limited - connection is working but throttled");
                return true; // Rate limit means API is reachable
            }
            catch (Exception ex) when (ex.Message.Contains("rate limited"))
            {
                _logger.LogWarning("AI API rate limited - connection is working but throttled");
                return true; // Rate limit means API is reachable
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI connection test failed");
                return false;
            }
        }

        #region Private AI Provider Methods

        private async Task<QuestionDto> GenerateWithOpenAIAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["AI:OpenAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenAI API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallOpenAIAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithGeminiAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _configuration["AI:Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallGeminiAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithClaudeAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = _configuration["AI:Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Claude API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallClaudeAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithGroqAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? _configuration["AI:Groq:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Groq API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallGroqAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithHuggingFaceAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") ?? _configuration["AI:HuggingFace:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("HuggingFace API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallHuggingFaceAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithTogetherAIAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("TOGETHER_API_KEY") ?? _configuration["AI:TogetherAI:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Together AI API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallTogetherAIAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<QuestionDto> GenerateWithOpenRouterAsync(Chapter chapter, GenerateQuestionRequest request)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? _configuration["AI:OpenRouter:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("OpenRouter API key not configured");
            }

            var prompt = BuildPhysicsQuestionPrompt(chapter, request);
            var response = await CallOpenRouterAsync(prompt);

            return ParseAIQuestionResponse(response, chapter, request);
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? _configuration["AI:OpenAI:ApiKey"];
            var model = _configuration["AI:OpenAI:Model"] ?? "gpt-3.5-turbo";
            var maxTokens = _configuration.GetValue<int>("AI:OpenAI:MaxTokens", 2048);
            var temperature = _configuration.GetValue<double>("AI:OpenAI:Temperature", 0.7);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là một giáo viên Vật lý chuyên nghiệp, tạo câu hỏi chất lượng cao cho học sinh THPT Việt Nam." },
                    new { role = "user", content = prompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent);

            return aiResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? _configuration["AI:Gemini:ApiKey"];
            var model = _configuration["AI:Gemini:Model"] ?? "gemini-1.5-flash";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            _httpClient.DefaultRequestHeaders.Clear();

            _logger.LogInformation($"Calling Gemini API with model: {model}");

            var response = await _httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Gemini raw response: {responseContent}");

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseContent);
            var extractedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";

            _logger.LogInformation($"Gemini extracted text: '{extractedText}' (Length: {extractedText.Length})");

            return extractedText;
        }

        private async Task<string> CallClaudeAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? _configuration["AI:Claude:ApiKey"];
            var model = _configuration["AI:Claude:Model"] ?? "claude-3-sonnet-20240229";
            var maxTokens = _configuration.GetValue<int>("AI:Claude:MaxTokens", 2048);

            var requestBody = new
            {
                model = model,
                max_tokens = maxTokens,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            var response = await _httpClient.PostAsync(
                "https://api.anthropic.com/v1/messages",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseContent);

            return claudeResponse?.Content?.FirstOrDefault()?.Text ?? "";
        }

        private async Task<string> CallGroqAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY") ?? _configuration["AI:Groq:ApiKey"];
            var model = _configuration["AI:Groq:Model"] ?? "llama-3.3-70b-versatile";
            var maxTokens = _configuration.GetValue<int>("AI:Groq:MaxTokens", 2048);
            var temperature = _configuration.GetValue<double>("AI:Groq:Temperature", 0.7);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là một giáo viên Vật lý chuyên nghiệp, tạo câu hỏi chất lượng cao cho học sinh THPT Việt Nam." },
                    new { role = "user", content = prompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.groq.com/openai/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"Groq raw response: {responseContent}");

            var groqResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent); // Same format as OpenAI
            var extractedContent = groqResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";

            _logger.LogInformation($"Groq extracted content: '{extractedContent}' (Length: {extractedContent.Length})");

            return extractedContent;
        }

        private async Task<string> CallHuggingFaceAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("HUGGINGFACE_API_KEY") ?? _configuration["AI:HuggingFace:ApiKey"];
            var model = _configuration["AI:HuggingFace:Model"] ?? "Qwen/Qwen2.5-72B-Instruct";

            var requestBody = new
            {
                inputs = prompt,
                parameters = new
                {
                    max_new_tokens = 2048,
                    temperature = 0.7,
                    return_full_text = false
                }
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var url = $"https://api-inference.huggingface.co/models/{model}";

            var response = await _httpClient.PostAsync(
                url,
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            // HuggingFace returns array format
            var hfResponse = JsonSerializer.Deserialize<HuggingFaceResponse[]>(responseContent);

            return hfResponse?.FirstOrDefault()?.GeneratedText ?? "";
        }

        private async Task<string> CallTogetherAIAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("TOGETHER_API_KEY") ?? _configuration["AI:TogetherAI:ApiKey"];
            var model = _configuration["AI:TogetherAI:Model"] ?? "meta-llama/Llama-3.1-70B-Instruct-Turbo";
            var maxTokens = _configuration.GetValue<int>("AI:TogetherAI:MaxTokens", 2048);
            var temperature = _configuration.GetValue<double>("AI:TogetherAI:Temperature", 0.7);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là một giáo viên Vật lý chuyên nghiệp, tạo câu hỏi chất lượng cao cho học sinh THPT Việt Nam." },
                    new { role = "user", content = prompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _httpClient.PostAsync(
                "https://api.together.xyz/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var togetherResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent); // Same format as OpenAI

            return togetherResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

        private async Task<string> CallOpenRouterAsync(string prompt)
        {
            var apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? _configuration["AI:OpenRouter:ApiKey"];
            var model = _configuration["AI:OpenRouter:Model"] ?? "microsoft/wizardlm-2-8x22b";
            var maxTokens = _configuration.GetValue<int>("AI:OpenRouter:MaxTokens", 2048);
            var temperature = _configuration.GetValue<double>("AI:OpenRouter:Temperature", 0.7);

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Bạn là một giáo viên Vật lý chuyên nghiệp, tạo câu hỏi chất lượng cao cho học sinh THPT Việt Nam." },
                    new { role = "user", content = prompt }
                },
                temperature = temperature,
                max_tokens = maxTokens
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost:5298"); // Required by OpenRouter

            var response = await _httpClient.PostAsync(
                "https://openrouter.ai/api/v1/chat/completions",
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();
            var openRouterResponse = JsonSerializer.Deserialize<OpenAIResponse>(responseContent); // Same format as OpenAI

            return openRouterResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
        }

        #endregion

        #region Helper Methods

        private string BuildPhysicsQuestionPrompt(Chapter chapter, GenerateQuestionRequest request)
        {
            var difficultyDesc = request.DifficultyLevel switch
            {
                "easy" => "dễ, phù hợp với học sinh trung bình",
                "medium" => "trung bình, yêu cầu tư duy phân tích",
                "hard" => "khó, thách thức học sinh giỏi",
                _ => "trung bình"
            };

            var typeDesc = request.QuestionType switch
            {
                "multiple_choice" => "trắc nghiệm 4 lựa chọn",
                "true_false" => "đúng/sai",
                "calculation" => "tính toán có lời giải",
                "essay" => "tự luận",
                _ => "trắc nghiệm 4 lựa chọn"
            };

            // Tạo prompt khác nhau cho essay vs multiple choice
            if (request.QuestionType == "essay")
            {
                return $@"
Tạo một câu hỏi tự luận Vật lý về chương ""{chapter.ChapterName}"" (lớp {chapter.Grade}) với độ khó {difficultyDesc}.

Yêu cầu chất lượng cao:
1. Câu hỏi phải chính xác về mặt khoa học và phù hợp chương trình THPT Việt Nam
2. Yêu cầu học sinh giải thích, phân tích hoặc tính toán chi tiết
3. Có câu trả lời mẫu đầy đủ và logic
4. Sử dụng thuật ngữ và ký hiệu Vật lý chuẩn
5. Khuyến khích tư duy phản biện và ứng dụng thực tế
{(string.IsNullOrEmpty(request.SpecificTopic) ? "" : $"6. Tập trung vào chủ đề cụ thể: {request.SpecificTopic}")}
{(string.IsNullOrEmpty(request.AdditionalInstructions) ? "" : $"7. Yêu cầu bổ sung: {request.AdditionalInstructions}")}

Trả về theo định dạng JSON chính xác:
{{
  ""question"": ""Nội dung câu hỏi tự luận"",
  ""sampleAnswer"": ""Câu trả lời mẫu chi tiết"",
  ""keyPoints"": [""điểm mấu chốt 1"", ""điểm mấu chốt 2"", ""điểm mấu chốt 3""],
  ""explanation"": ""Hướng dẫn giải chi tiết"",
  ""difficulty"": ""{request.DifficultyLevel}"",
  ""topic"": ""{chapter.ChapterName}""
}}";
            }

            return $@"
Tạo một câu hỏi Vật lý {typeDesc} về chương ""{chapter.ChapterName}"" (lớp {chapter.Grade}) với độ khó {difficultyDesc}.

Yêu cầu chất lượng cao:
1. Câu hỏi phải chính xác về mặt khoa học và phù hợp chương trình THPT Việt Nam
2. Có 4 lựa chọn đáp án (A, B, C, D) với 1 đáp án đúng duy nhất
3. Các đáp án sai phải hợp lý, không quá dễ loại trừ
4. Sử dụng thuật ngữ và ký hiệu Vật lý chuẩn
5. Kèm giải thích chi tiết cho đáp án đúng
{(string.IsNullOrEmpty(request.SpecificTopic) ? "" : $"6. Tập trung vào chủ đề cụ thể: {request.SpecificTopic}")}
{(string.IsNullOrEmpty(request.AdditionalInstructions) ? "" : $"7. Yêu cầu bổ sung: {request.AdditionalInstructions}")}

Trả về theo định dạng JSON chính xác:
{{
  ""question"": ""Nội dung câu hỏi"",
  ""choices"": [
    {{""label"": ""A"", ""text"": ""Lựa chọn A"", ""isCorrect"": false}},
    {{""label"": ""B"", ""text"": ""Lựa chọn B"", ""isCorrect"": true}},
    {{""label"": ""C"", ""text"": ""Lựa chọn C"", ""isCorrect"": false}},
    {{""label"": ""D"", ""text"": ""Lựa chọn D"", ""isCorrect"": false}}
  ],
  ""explanation"": ""Giải thích chi tiết tại sao đáp án B đúng và các đáp án khác sai"",
  ""difficulty"": ""{request.DifficultyLevel}"",
  ""topic"": ""{chapter.ChapterName}""
}}";
        }

                private QuestionDto ParseAIQuestionResponse(string aiResponse, Chapter chapter, GenerateQuestionRequest request)
        {
            try
            {
                var parsedResponse = JsonSerializer.Deserialize<AIQuestionResponse>(aiResponse);
                
                var questionDto = new QuestionDto
                {
                    QuestionId = Guid.NewGuid().ToString(),
                    Topic = parsedResponse?.Topic ?? chapter.ChapterName,
                    QuestionText = parsedResponse?.Question ?? "Câu hỏi AI",
                    QuestionType = request.QuestionType,
                    Difficulty = request.DifficultyLevel,
                    ImageUrl = "",
                    CreatedBy = "ai_system",
                    CreatedAt = DateTime.UtcNow,
                    Explanation = parsedResponse?.Explanation ?? ""
                };

                // Xử lý khác nhau cho essay vs multiple choice
                if (request.QuestionType == "essay")
                {
                    // Cho essay questions, không có answer choices
                    questionDto.AnswerChoices = new List<AnswerChoiceDto>();
                    // Sample answer và key points sẽ được lưu trong explanation hoặc metadata
                }
                else
                {
                    // Cho multiple choice questions
                    questionDto.AnswerChoices = parsedResponse?.Choices?.Select(c => new AnswerChoiceDto
                    {
                        ChoiceId = Guid.NewGuid().ToString(),
                        ChoiceLabel = c.Label,
                        ChoiceText = c.Text,
                        IsCorrect = c.IsCorrect,
                        DisplayOrder = c.Label[0] - 'A' + 1
                    }).ToList() ?? new List<AnswerChoiceDto>();
                }

                return questionDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response, using mock question");
                return CreateMockQuestion(chapter, request);
            }
        }

        private QuestionDto CreateMockQuestion(Chapter chapter, GenerateQuestionRequest request)
        {
            _logger.LogWarning($"🤖 Creating fallback question for Chapter: {chapter.ChapterName}");
            
            var questionDto = new QuestionDto
            {
                QuestionId = Guid.NewGuid().ToString(),
                Topic = chapter.ChapterName,
                QuestionText = $"[AI Generated] Câu hỏi {request.QuestionType} về {chapter.ChapterName} - Mức độ: {request.DifficultyLevel}",
                QuestionType = request.QuestionType,
                Difficulty = request.DifficultyLevel,
                DifficultyLevel = request.DifficultyLevel,
                ImageUrl = "",
                CreatedBy = "ai_system",
                CreatedAt = DateTime.UtcNow,
                Explanation = $"Đây là câu hỏi được tạo tự động cho chương {chapter.ChapterName} với mức độ {request.DifficultyLevel}."
            };

            if (request.QuestionType == "essay")
            {
                questionDto.AnswerChoices = new List<AnswerChoiceDto>();
            }
            else
            {
                // Tạo 4 đáp án mẫu cho multiple choice
                questionDto.AnswerChoices = new List<AnswerChoiceDto>
                {
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "A", ChoiceText = "Đáp án A", IsCorrect = true, DisplayOrder = 1 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "B", ChoiceText = "Đáp án B", IsCorrect = false, DisplayOrder = 2 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "C", ChoiceText = "Đáp án C", IsCorrect = false, DisplayOrder = 3 },
                    new() { ChoiceId = Guid.NewGuid().ToString(), ChoiceLabel = "D", ChoiceText = "Đáp án D", IsCorrect = false, DisplayOrder = 4 }
                };
            }

            return questionDto;
        }

        // Additional helper methods would go here...
        private string BuildImprovementPrompt(Question question, ImproveQuestionRequest request) => "";
        private string BuildValidationPrompt(Question question) => "";
        private string BuildTopicSuggestionPrompt(Chapter chapter, TopicSuggestionRequest request) => "";
        private string BuildExplanationPrompt(Question question, string correctAnswer) => "";
        private QuestionDto ParseImprovedQuestion(string content, Question original) => new();
        private QuestionValidationDto ParseValidationResult(string result) => new();
        private List<TopicSuggestionDto> ParseTopicSuggestions(string suggestions) => new();
        private string CreateMockValidation() => throw new InvalidOperationException();
        private List<TopicSuggestionDto> CreateMockTopicSuggestions(Chapter chapter) => throw new InvalidOperationException();
        private async Task<StudentPerformance> AnalyzeStudentPerformance(string studentId, int chapterId) => new();
        private string DetermineAdaptiveDifficulty(StudentPerformance performance, int questionIndex) => "medium";
        private Dictionary<string, int> CalculateDifficultyDistribution(int totalQuestions, DifficultyDistribution distribution)
        {
            var result = new Dictionary<string, int>();

            var easyCount = (int)Math.Round(totalQuestions * distribution.EasyPercentage / 100.0);
            var hardCount = (int)Math.Round(totalQuestions * distribution.HardPercentage / 100.0);
            var mediumCount = totalQuestions - easyCount - hardCount;

            if (easyCount > 0) result["easy"] = easyCount;
            if (mediumCount > 0) result["medium"] = mediumCount;
            if (hardCount > 0) result["hard"] = hardCount;

            return result;
        }
        private decimal CalculateQuestionPoints(string difficulty) => difficulty switch { "easy" => 1, "medium" => 2, "hard" => 3, _ => 2 };
        private async Task<AIUsageStatistics> GetUsageStatistics() => new();
        private List<string> GetSupportedFeatures(string provider) => new() { "question_generation", "validation", "improvement" };

        #endregion

        #region Response Classes

        private class OpenAIResponse
        {
            [JsonPropertyName("choices")]
            public OpenAIChoice[]? Choices { get; set; }
        }

        private class OpenAIChoice
        {
            [JsonPropertyName("message")]
            public OpenAIMessage? Message { get; set; }
        }

        private class OpenAIMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public GeminiCandidate[]? Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }

        private class GeminiContent
        {
            [JsonPropertyName("parts")]
            public GeminiPart[]? Parts { get; set; }
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }

        private class ClaudeResponse
        {
            public ClaudeContent[]? Content { get; set; }
        }

        private class ClaudeContent
        {
            public string? Text { get; set; }
        }

        private class AIQuestionResponse
        {
            public string? Question { get; set; }
            public AIChoiceResponse[]? Choices { get; set; }
            public string? Explanation { get; set; }
            public string? Difficulty { get; set; }
            public string? Topic { get; set; }
            
            // Essay-specific properties
            public string? SampleAnswer { get; set; }
            public string[]? KeyPoints { get; set; }
        }

        private class AIChoiceResponse
        {
            public string Label { get; set; } = "";
            public string Text { get; set; } = "";
            public bool IsCorrect { get; set; }
        }

        private class StudentPerformance
        {
            public double AverageScore { get; set; }
            public List<string> WeakTopics { get; set; } = new();
            public List<string> StrongTopics { get; set; } = new();
        }

        private class HuggingFaceResponse
        {
            [JsonPropertyName("generated_text")]
            public string? GeneratedText { get; set; }
        }

        #endregion
    }
}