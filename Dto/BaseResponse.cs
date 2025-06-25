using System.Text.Json.Serialization;

namespace BE_Phygens.Dto
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public T? Data { get; set; }

        [JsonPropertyName("errors")]
        public List<string>? Errors { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Success response
        public static ApiResponse<T> SuccessResult(T data, string message = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        // Error response
        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    public class PaginatedResponse<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; } = new();

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; }

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("has_previous")]
        public bool HasPrevious { get; set; }
    }

    public class PaginationRequest
    {
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 10;

        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("sort_by")]
        public string? SortBy { get; set; }

        [JsonPropertyName("sort_direction")]
        public string SortDirection { get; set; } = "asc"; // asc or desc
    }

    // New DTOs for Analytics
    public class DashboardDataDto
    {
        [JsonPropertyName("total_exams")]
        public int TotalExams { get; set; }

        [JsonPropertyName("total_users")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("total_questions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("total_chapters")]
        public int TotalChapters { get; set; }

        [JsonPropertyName("recent_activities")]
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
    }

    public class RecentActivityDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    public class ExamListDto
    {
        [JsonPropertyName("exam_id")]
        public string ExamId { get; set; } = string.Empty;

        [JsonPropertyName("exam_name")]
        public string ExamName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("exam_type")]
        public string ExamType { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("is_published")]
        public bool IsPublished { get; set; }
    }

    public class ExamStatsDto
    {
        [JsonPropertyName("total_exams")]
        public int TotalExams { get; set; }

        [JsonPropertyName("published_exams")]
        public int PublishedExams { get; set; }

        [JsonPropertyName("draft_exams")]
        public int DraftExams { get; set; }

        [JsonPropertyName("total_questions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("ai_generated_questions")]
        public int AiGeneratedQuestions { get; set; }

        [JsonPropertyName("exams_by_type")]
        public Dictionary<string, int> ExamsByType { get; set; } = new();
    }

    public class SampleExamDto
    {
        [JsonPropertyName("examId")]
        public string ExamId { get; set; } = string.Empty;

        [JsonPropertyName("examName")]
        public string ExamName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("examType")]
        public string ExamType { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("questionCount")]
        public int QuestionCount { get; set; }

        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = string.Empty;

        [JsonPropertyName("grade")]
        public int? Grade { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("isPopular")]
        public bool IsPopular { get; set; }
    }
} 