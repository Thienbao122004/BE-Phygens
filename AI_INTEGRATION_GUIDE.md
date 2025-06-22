# 🤖 Hướng Dẫn Tích Hợp AI vào PhyGen

## 📋 **Tổng Quan**

PhyGen hiện đã được tích hợp sẵn AI Service hỗ trợ:
- ✅ OpenAI GPT-3.5/GPT-4
- ✅ Google Gemini Pro
- ✅ Anthropic Claude
- ✅ Mock AI (fallback)

## 🚀 **Bước 1: Cấu Hình API Keys**

### 1.1 Cập nhật `appsettings.json`

```json
{
  "AI": {
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-your-openai-api-key-here",
      "Model": "gpt-3.5-turbo",
      "MaxTokens": 2048,
      "Temperature": 0.7
    },
    "Gemini": {
      "ApiKey": "your-gemini-api-key-here",
      "Model": "gemini-pro"
    },
    "Claude": {
      "ApiKey": "your-claude-api-key-here",
      "Model": "claude-3-sonnet-20240229"
    },
    "RateLimit": 60,
    "DailyQuota": 1000,
    "EnableCaching": true,
    "FallbackToMock": true
  }
}
```

### 1.2 Lấy API Keys

#### **OpenAI:**
1. Truy cập: https://platform.openai.com/api-keys
2. Tạo API key mới
3. Copy và paste vào `appsettings.json`

#### **Google Gemini:**
1. Truy cập: https://aistudio.google.com/app/apikey
2. Tạo API key
3. Copy và paste vào `appsettings.json`

#### **Anthropic Claude:**
1. Truy cập: https://console.anthropic.com/
2. Tạo API key
3. Copy và paste vào `appsettings.json`

## 🔧 **Bước 2: Chạy Ứng Dụng**

### 2.1 Build Project
```bash
# Dừng ứng dụng hiện tại (nếu đang chạy)
# Ctrl + C trong terminal

# Clean và build lại
dotnet clean
dotnet build
```

### 2.2 Chạy Ứng Dụng
```bash
dotnet run
```

## 🧪 **Bước 3: Test AI Integration**

### 3.1 Test API Connection
```bash
# Test kết nối AI (cần admin token)
POST /ai-question/test-connection
Authorization: Bearer your-admin-jwt-token
```

### 3.2 Generate AI Question
```bash
# Tạo câu hỏi AI
POST /ai-question/generate
Authorization: Bearer your-jwt-token
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

### 3.3 Check AI Status
```bash
# Kiểm tra trạng thái AI (admin only)
GET /ai-question/config
Authorization: Bearer your-admin-jwt-token
```

## 📊 **Bước 4: Sử Dụng Tính Năng AI**

### 4.1 **Tạo Câu Hỏi Đơn Lẻ**
- Endpoint: `POST /ai-question/generate`
- Tự động tạo câu hỏi Vật lý theo chương và độ khó
- Hỗ trợ nhiều loại câu hỏi: trắc nghiệm, đúng/sai, tính toán

### 4.2 **Tạo Câu Hỏi Hàng Loạt**
- Endpoint: `POST /ai-question/generate-batch`
- Tạo nhiều câu hỏi cùng lúc
- Tự động rate limiting để tránh spam API

### 4.3 **Cải Thiện Câu Hỏi**
- Endpoint: `POST /ai-question/improve/{questionId}`
- AI phân tích và cải thiện câu hỏi hiện có
- Tăng chất lượng và độ chính xác

### 4.4 **Kiểm Tra Chất Lượng**
- Endpoint: `POST /ai-question/validate/{questionId}`
- AI đánh giá chất lượng câu hỏi
- Phát hiện lỗi khoa học, ngữ pháp

### 4.5 **Gợi Ý Chủ Đề**
- Endpoint: `POST /ai-question/suggest-topics`
- AI đề xuất chủ đề phù hợp cho từng chương
- Giúp đa dạng hóa ngân hàng câu hỏi

## ⚙️ **Bước 5: Cấu Hình Nâng Cao**

### 5.1 **Chuyển Đổi AI Provider**

Trong `appsettings.json`, thay đổi:
```json
{
  "AI": {
    "Provider": "Gemini"  // hoặc "Claude", "OpenAI"
  }
}
```

### 5.2 **Điều Chỉnh Tham Số AI**

```json
{
  "AI": {
    "OpenAI": {
      "Model": "gpt-4",           // Model mạnh hơn
      "Temperature": 0.5,         // Giảm tính ngẫu nhiên
      "MaxTokens": 4096          // Tăng độ dài response
    }
  }
}
```

### 5.3 **Caching & Rate Limiting**

```json
{
  "AI": {
    "EnableCaching": true,
    "CacheDurationMinutes": 120,
    "RateLimit": 30,              // requests/minute
    "DailyQuota": 500,           // requests/day
    "FallbackToMock": true       // Dùng mock khi lỗi
  }
}
```

## 🎯 **Bước 6: Tích Hợp Frontend**

### 6.1 **Cập Nhật Admin Service**

File: `FE_Physics-Test-System-Highschool/src/services/adminService.jsx`

```javascript
// Test AI connection
export const testAIConnection = async () => {
  try {
    const response = await api.post('/ai-question/test-connection');
    return response.data;
  } catch (error) {
    console.error('AI connection test failed:', error);
    throw error;
  }
};

// Generate AI question
export const generateAIQuestion = async (questionData) => {
  try {
    const response = await api.post('/ai-question/generate', questionData);
    return response.data;
  } catch (error) {
    console.error('AI question generation failed:', error);
    throw error;
  }
};

// Get AI status
export const getAIStatus = async () => {
  try {
    const response = await api.get('/ai-question/config');
    return response.data;
  } catch (error) {
    console.error('Failed to get AI status:', error);
    throw error;
  }
};
```

### 6.2 **Tạo AI Question Component**

```jsx
// src/components/AIQuestionGenerator.jsx
import React, { useState } from 'react';
import { generateAIQuestion } from '../services/adminService';

const AIQuestionGenerator = () => {
  const [loading, setLoading] = useState(false);
  const [question, setQuestion] = useState(null);
  
  const handleGenerate = async (formData) => {
    setLoading(true);
    try {
      const result = await generateAIQuestion(formData);
      setQuestion(result.data);
    } catch (error) {
      console.error('Generation failed:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="ai-question-generator">
      {/* Form tạo câu hỏi */}
      {/* Hiển thị kết quả */}
    </div>
  );
};
```

## 🔍 **Bước 7: Monitoring & Debug**

### 7.1 **Logs**

Kiểm tra logs trong console:
```bash
dotnet run
# Xem logs AI operations
```

### 7.2 **Health Check**

```bash
GET /health
# Kiểm tra trạng thái hệ thống và database
```

### 7.3 **Swagger UI**

Truy cập: `http://localhost:5000`
- Test tất cả AI endpoints
- Xem API documentation
- Test với JWT token

## 🚨 **Troubleshooting**

### Lỗi Thường Gặp:

#### 1. **"AI API Key not configured"**
- ✅ Kiểm tra API key trong `appsettings.json`
- ✅ Đảm bảo key đúng format và còn hạn

#### 2. **"Rate limit exceeded"**
- ✅ Giảm `RateLimit` trong config
- ✅ Tăng delay giữa các requests

#### 3. **"AI connection failed"**
- ✅ Kiểm tra internet connection
- ✅ Verify API key còn credit
- ✅ Check firewall settings

#### 4. **"Build failed - file locked"**
```bash
# Dừng tất cả processes
taskkill /f /im BE_Phygens.exe
# Hoặc restart IDE/terminal
```

## 🎉 **Kết Quả**

Sau khi hoàn thành, bạn sẽ có:

✅ **AI Question Generation**: Tạo câu hỏi Vật lý chất lượng cao  
✅ **Multi-Provider Support**: OpenAI, Gemini, Claude  
✅ **Smart Caching**: Giảm chi phí API calls  
✅ **Quality Validation**: AI kiểm tra chất lượng câu hỏi  
✅ **Batch Processing**: Tạo nhiều câu hỏi cùng lúc  
✅ **Adaptive Learning**: Câu hỏi thích ứng theo học sinh  
✅ **Admin Dashboard**: Quản lý và monitor AI  

## 📞 **Hỗ Trợ**

Nếu gặp vấn đề:
1. Kiểm tra logs trong console
2. Test API endpoints qua Swagger
3. Verify configuration trong `appsettings.json`
4. Check network connectivity

**Chúc bạn tích hợp AI thành công! 🚀** 