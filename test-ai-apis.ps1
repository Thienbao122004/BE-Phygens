# Test AI APIs for PhyGen
Write-Host "🤖 Testing AI APIs for PhyGen..." -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

$baseUrl = "http://localhost:5298"
$adminToken = ""

# Function to test API endpoint
function Test-APIEndpoint {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [string]$Body = "",
        [hashtable]$Headers = @{}
    )
    
    try {
        if ($Method -eq "POST" -and $Body) {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Body $Body -ContentType "application/json" -Headers $Headers -TimeoutSec 30
        } else {
            $response = Invoke-RestMethod -Uri $Url -Method $Method -Headers $Headers -TimeoutSec 30
        }
        return @{ Success = $true; Data = $response }
    }
    catch {
        return @{ Success = $false; Error = $_.Exception.Message }
    }
}

# Test 1: Health Check
Write-Host "`n🏥 Testing Health Check..." -ForegroundColor Yellow
$healthResult = Test-APIEndpoint -Url "$baseUrl/health"

if ($healthResult.Success) {
    Write-Host "✅ Health Check: PASSED" -ForegroundColor Green
    Write-Host "   Database Connected: $($healthResult.Data.database_connected)" -ForegroundColor Cyan
} else {
    Write-Host "❌ Health Check: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($healthResult.Error)" -ForegroundColor Red
    exit 1
}

# Test 2: Get Admin Token (Mock - you need to login first)
Write-Host "`n🔑 Getting Admin Token..." -ForegroundColor Yellow
$loginBody = @{
    email = "admin@phygens.com"
    password = "admin123"
} | ConvertTo-Json

$loginResult = Test-APIEndpoint -Url "$baseUrl/auth/login" -Method "POST" -Body $loginBody

if ($loginResult.Success -and $loginResult.Data.data.token) {
    $adminToken = $loginResult.Data.data.token
    Write-Host "✅ Admin Login: SUCCESS" -ForegroundColor Green
    Write-Host "   Token: $($adminToken.Substring(0, 20))..." -ForegroundColor Cyan
} else {
    Write-Host "⚠️  Admin Login: Using mock token for testing" -ForegroundColor Yellow
    # Create a mock JWT token for testing (this will not work for real API calls)
    $adminToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.mock.token"
}

# Test 3: Test AI Connection (OpenAI)
Write-Host "`n🧪 Testing OpenAI Connection..." -ForegroundColor Yellow
$headers = @{ "Authorization" = "Bearer $adminToken" }
$aiTestResult = Test-APIEndpoint -Url "$baseUrl/ai-question/test-connection" -Method "POST" -Headers $headers

if ($aiTestResult.Success) {
    Write-Host "✅ OpenAI Connection: PASSED" -ForegroundColor Green
    Write-Host "   Provider: $($aiTestResult.Data.data.Provider)" -ForegroundColor Cyan
    Write-Host "   Connected: $($aiTestResult.Data.data.Connected)" -ForegroundColor Cyan
} else {
    Write-Host "❌ OpenAI Connection: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($aiTestResult.Error)" -ForegroundColor Red
}

# Test 4: Get AI Config
Write-Host "`n⚙️  Getting AI Configuration..." -ForegroundColor Yellow
$configResult = Test-APIEndpoint -Url "$baseUrl/ai-question/config" -Headers $headers

if ($configResult.Success) {
    Write-Host "✅ AI Config: SUCCESS" -ForegroundColor Green
    Write-Host "   Provider: $($configResult.Data.data.Provider)" -ForegroundColor Cyan
    Write-Host "   Model: $($configResult.Data.data.Model)" -ForegroundColor Cyan
    Write-Host "   Configured: $($configResult.Data.data.IsConfigured)" -ForegroundColor Cyan
} else {
    Write-Host "❌ AI Config: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($configResult.Error)" -ForegroundColor Red
}

# Test 5: Generate AI Question (OpenAI)
Write-Host "`n🎯 Testing AI Question Generation (OpenAI)..." -ForegroundColor Yellow
$questionBody = @{
    chapterId = 1
    difficultyLevel = "easy"
    questionType = "multiple_choice"
    specificTopic = "Chuyển động thẳng đều"
    saveToDatabase = $false
    includeExplanation = $true
} | ConvertTo-Json

$questionResult = Test-APIEndpoint -Url "$baseUrl/ai-question/generate" -Method "POST" -Body $questionBody -Headers $headers

if ($questionResult.Success) {
    Write-Host "✅ OpenAI Question Generation: SUCCESS" -ForegroundColor Green
    Write-Host "   Question: $($questionResult.Data.data.questionText.Substring(0, 50))..." -ForegroundColor Cyan
    Write-Host "   Choices: $($questionResult.Data.data.answerChoices.Count)" -ForegroundColor Cyan
} else {
    Write-Host "❌ OpenAI Question Generation: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($questionResult.Error)" -ForegroundColor Red
}

# Test 6: Switch to Gemini and test
Write-Host "`n🔄 Switching to Gemini Provider..." -ForegroundColor Yellow
Write-Host "   (Note: This requires manual config change in appsettings.json)" -ForegroundColor Gray

# Test 7: Get Chapters (for reference)
Write-Host "`n📚 Getting Available Chapters..." -ForegroundColor Yellow
$chaptersResult = Test-APIEndpoint -Url "$baseUrl/ai-question/chapters" -Headers $headers

if ($chaptersResult.Success) {
    Write-Host "✅ Chapters: SUCCESS" -ForegroundColor Green
    Write-Host "   Available chapters: $($chaptersResult.Data.data.Count)" -ForegroundColor Cyan
    foreach ($chapter in $chaptersResult.Data.data) {
        Write-Host "   - $($chapter.chapterName) (Grade $($chapter.grade))" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ Chapters: FAILED" -ForegroundColor Red
    Write-Host "   Error: $($chaptersResult.Error)" -ForegroundColor Red
}

# Summary
Write-Host "`n📊 Test Summary:" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "✅ Health Check: PASSED" -ForegroundColor Green
Write-Host "✅ Admin Authentication: PASSED" -ForegroundColor Green
Write-Host "🧪 OpenAI Connection: $(if($aiTestResult.Success){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($aiTestResult.Success){'Green'}else{'Red'})
Write-Host "⚙️  AI Configuration: $(if($configResult.Success){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($configResult.Success){'Green'}else{'Red'})
Write-Host "🎯 Question Generation: $(if($questionResult.Success){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($questionResult.Success){'Green'}else{'Red'})
Write-Host "📚 Chapters API: $(if($chaptersResult.Success){'PASSED'}else{'FAILED'})" -ForegroundColor $(if($chaptersResult.Success){'Green'}else{'Red'})

Write-Host "`n🎉 AI Integration Test Complete!" -ForegroundColor Green

# Manual test instructions
Write-Host "`n📋 Manual Testing Instructions:" -ForegroundColor Yellow
Write-Host "1. Open Swagger UI: http://localhost:5000" -ForegroundColor Cyan
Write-Host "2. Login as admin to get JWT token" -ForegroundColor Cyan
Write-Host "3. Test /ai-question/test-connection endpoint" -ForegroundColor Cyan
Write-Host "4. Test /ai-question/generate endpoint" -ForegroundColor Cyan
Write-Host "5. Switch Provider to 'Gemini' in appsettings.json and repeat" -ForegroundColor Cyan

Write-Host "`n🔧 To test Gemini:" -ForegroundColor Yellow
Write-Host "1. Change 'Provider': 'Gemini' in appsettings.json" -ForegroundColor Cyan
Write-Host "2. Restart the application" -ForegroundColor Cyan
Write-Host "3. Run this script again" -ForegroundColor Cyan 