# Simple AI API Test for PhyGen
Write-Host "🤖 Testing AI APIs for PhyGen..." -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

$baseUrl = "http://localhost:5298"

# Test Health Check
Write-Host "`n🏥 Testing Health Check..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod -Uri "$baseUrl/health" -TimeoutSec 10
    Write-Host "✅ Health Check: PASSED" -ForegroundColor Green
    Write-Host "   Database Connected: $($health.database_connected)" -ForegroundColor Cyan
    Write-Host "   Status: $($health.status)" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ Health Check: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure the application is running on port 5000" -ForegroundColor Yellow
    exit 1
}

# Test Swagger UI
Write-Host "`n📖 Testing Swagger UI..." -ForegroundColor Yellow
try {
    $swagger = Invoke-WebRequest -Uri "$baseUrl" -TimeoutSec 10
    Write-Host "✅ Swagger UI: ACCESSIBLE" -ForegroundColor Green
    Write-Host "   URL: http://localhost:5000" -ForegroundColor Cyan
}
catch {
    Write-Host "❌ Swagger UI: NOT ACCESSIBLE" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Instructions for manual testing
Write-Host "`n📋 Manual Testing Instructions:" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow
Write-Host "1. Open browser and go to: http://localhost:5000" -ForegroundColor Cyan
Write-Host "2. You should see Swagger UI with all API endpoints" -ForegroundColor Cyan
Write-Host "3. First, login to get JWT token:" -ForegroundColor Cyan
Write-Host "   - Click on 'auth' section" -ForegroundColor Gray
Write-Host "   - Use POST /auth/login endpoint" -ForegroundColor Gray
Write-Host "   - Try these credentials:" -ForegroundColor Gray
Write-Host "     Email: admin@phygens.com" -ForegroundColor Gray
Write-Host "     Password: admin123" -ForegroundColor Gray
Write-Host "4. Copy the JWT token from response" -ForegroundColor Cyan
Write-Host "5. Click 'Authorize' button at top of Swagger UI" -ForegroundColor Cyan
Write-Host "6. Enter: Bearer [your-jwt-token]" -ForegroundColor Cyan

Write-Host "`n🧪 Testing AI Endpoints:" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow
Write-Host "1. Test AI Connection:" -ForegroundColor Cyan
Write-Host "   - POST /ai-question/test-connection" -ForegroundColor Gray
Write-Host "   - Should return OpenAI connection status" -ForegroundColor Gray

Write-Host "`n2. Get AI Configuration:" -ForegroundColor Cyan
Write-Host "   - GET /ai-question/config" -ForegroundColor Gray
Write-Host "   - Should show current AI provider settings" -ForegroundColor Gray

Write-Host "`n3. Generate AI Question:" -ForegroundColor Cyan
Write-Host "   - POST /ai-question/generate" -ForegroundColor Gray
Write-Host "   - Use this sample request body:" -ForegroundColor Gray
Write-Host "   {" -ForegroundColor DarkGray
Write-Host "     `"chapterId`": 1," -ForegroundColor DarkGray
Write-Host "     `"difficultyLevel`": `"easy`"," -ForegroundColor DarkGray
Write-Host "     `"questionType`": `"multiple_choice`"," -ForegroundColor DarkGray
Write-Host "     `"specificTopic`": `"Chuyển động thẳng đều`"," -ForegroundColor DarkGray
Write-Host "     `"saveToDatabase`": false," -ForegroundColor DarkGray
Write-Host "     `"includeExplanation`": true" -ForegroundColor DarkGray
Write-Host "   }" -ForegroundColor DarkGray

Write-Host "`n🔄 Testing Gemini:" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow
Write-Host "1. Stop the application (Ctrl+C)" -ForegroundColor Cyan
Write-Host "2. Edit appsettings.json:" -ForegroundColor Cyan
Write-Host "   Change: `"Provider`": `"Gemini`"" -ForegroundColor Gray
Write-Host "3. Restart: dotnet run" -ForegroundColor Cyan
Write-Host "4. Repeat the AI tests above" -ForegroundColor Cyan

Write-Host "`n🎯 Expected Results:" -ForegroundColor Yellow
Write-Host "=================================" -ForegroundColor Yellow
Write-Host "✅ OpenAI should generate physics questions in Vietnamese" -ForegroundColor Green
Write-Host "✅ Gemini should also work with your API key" -ForegroundColor Green
Write-Host "✅ Questions should have 4 multiple choice answers" -ForegroundColor Green
Write-Host "✅ AI should include explanations" -ForegroundColor Green

Write-Host "`n🚨 Troubleshooting:" -ForegroundColor Red
Write-Host "=================================" -ForegroundColor Red
Write-Host "- If 401 Unauthorized: Check JWT token" -ForegroundColor Yellow
Write-Host "- If 500 Internal Error: Check API keys in appsettings.json" -ForegroundColor Yellow
Write-Host "- If connection timeout: Check internet connection" -ForegroundColor Yellow
Write-Host "- If rate limit: Wait a few minutes and try again" -ForegroundColor Yellow

Write-Host "`n🎉 Ready to test AI integration!" -ForegroundColor Green
Write-Host "Open: http://localhost:5000" -ForegroundColor Cyan 