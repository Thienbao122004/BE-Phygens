# Hướng dẫn tích hợp AI và thiết lập dữ liệu

## 1. Thiết lập Database sau khi xóa dữ liệu

### Bước 1: Chạy Migration
```bash
dotnet ef database update
```

### Bước 2: Seed dữ liệu cơ bản
Chạy script SQL `setup_database_data.sql` để tạo dữ liệu mẫu:

```bash
# Kết nối PostgreSQL và chạy script
psql -h localhost -U your_username -d your_database_name -f setup_database_data.sql
```

### Bước 3: Cấu hình AI trong appsettings.json
```json
{
  "AI": {
    "Provider": "gemini",
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY",
      "BaseUrl": "https://generativelanguage.googleapis.com",
      "Model": "gemini-1.5-flash"
    },
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "BaseUrl": "https://api.openai.com",
      "Model": "gpt-4o-mini"
    },
    "FallbackToMock": true,
    "CacheDurationMinutes": 60,
    "BatchDelayMs": 1000
  }
}
```

## 2. Các API có sẵn để tạo đề thi AI

### 2.1 Kiểm tra kết nối AI
```http
POST /ai-question/test-connection
Content-Type: application/json
```

### 2.2 Lấy danh sách chương
```http
GET /ai-question/chapters
```

### 2.3 Tạo câu hỏi AI đơn lẻ
```http
POST /ai-question/generate
Content-Type: application/json

{
  "chapterId": 1,
  "difficultyLevel": "medium",
  "questionType": "multiple_choice",
  "specificTopic": "Chuyển động thẳng đều",
  "saveToDatabase": true,
  "includeExplanation": true
}
```

### 2.4 Tạo nhiều câu hỏi cùng lúc
```http
POST /ai-question/generate-batch
Content-Type: application/json

{
  "questionSpecs": [
    {
      "chapterId": 1,
      "difficultyLevel": "easy",
      "questionType": "multiple_choice",
      "count": 5
    },
    {
      "chapterId": 2,
      "difficultyLevel": "medium",
      "questionType": "multiple_choice",
      "count": 3
    }
  ],
  "saveToDatabase": true
}
```

### 2.5 Tạo ma trận đề thi
```http
POST /smart-exam/create-matrix
Content-Type: application/json

{
  "examName": "Kiểm tra 15 phút - Chương 1",
  "examType": "15p",
  "grade": 10,
  "duration": 15,
  "totalPoints": 10,
  "chapterDetails": [
    {
      "chapterId": 1,
      "questionCount": 5,
      "difficultyLevel": "easy"
    },
    {
      "chapterId": 1,
      "questionCount": 3,
      "difficultyLevel": "medium"
    }
  ]
}
```

### 2.6 Tạo đề thi từ ma trận
```http
POST /smart-exam/generate-exam/{matrixId}
```

## 3. Quy trình tạo đề thi hoàn chỉnh

### Bước 1: Chuẩn bị dữ liệu
1. Chạy script seed database
2. Kiểm tra kết nối AI
3. Xem danh sách chapters có sẵn

### Bước 2: Tạo câu hỏi
1. Sử dụng API generate để tạo câu hỏi cho từng chương
2. Hoặc sử dụng generate-batch để tạo nhiều câu hỏi cùng lúc

### Bước 3: Tạo đề thi
1. Tạo ma trận đề thi với phân bố câu hỏi mong muốn
2. Generate đề thi từ ma trận

## 4. Dữ liệu mẫu được tạo

### Users
- `ai_system_admin`: Admin cho hệ thống AI
- `default_teacher`: Giáo viên mặc định

### Physics Topics
- Cơ học cơ bản (Lớp 10)
- Nhiệt học (Lớp 10)
- Điện học (Lớp 11)
- Từ học (Lớp 11)
- Quang học (Lớp 11)
- Vật lý hạt nhân (Lớp 12)
- Vật lý lượng tử (Lớp 12)

### Chapters
14 chương học từ lớp 10-12 bao gồm:
- Chuyển động thẳng
- Lực và chuyển động
- Công và năng lượng
- Nhiệt học cơ bản
- Điện tích và điện trường
- v.v...

### Smart Exam Templates
6 templates mặc định cho các loại kiểm tra:
- Kiểm tra 15 phút (Lớp 10, 11, 12)
- Kiểm tra 1 tiết (Lớp 10, 11, 12)

## 5. Troubleshooting

### Vấn đề: Không tạo được câu hỏi
- Kiểm tra API key AI
- Kiểm tra kết nối internet
- Enable FallbackToMock để test

### Vấn đề: Không có chapter/topic
- Chạy lại script setup_database_data.sql
- Kiểm tra migration đã chạy đúng

### Vấn đề: Authentication lỗi
- API có thể chạy mà không cần auth (đã remove [Authorize])
- Hệ thống tự tạo user mặc định khi cần

## 6. Cấu hình AI Provider

### Sử dụng Gemini (Miễn phí)
1. Truy cập Google AI Studio
2. Tạo API key
3. Cấu hình trong appsettings.json

### Sử dụng OpenAI
1. Đăng ký OpenAI account
2. Tạo API key
3. Cấu hình trong appsettings.json

### Sử dụng Mock (Testing)
Set `"Provider": "mock"` trong config để test mà không cần API key thật.

## 7. Monitoring và Statistics

Hệ thống tự động track:
- Số lượng câu hỏi được tạo
- Chi phí sử dụng AI
- Tỷ lệ thành công
- Thống kê theo độ khó và chương

Xem thống kê qua API:
```http
GET /ai-question/config
``` 