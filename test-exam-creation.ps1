$headers = @{
    'Authorization' = 'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsInJvbGUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AcGh5Z2Vucy5jb20iLCJuYmYiOjE3Mzc3MTM2NjIsImV4cCI6MTczNzcxNzI2MiwiaWF0IjoxNzM3NzEzNjYyfQ.-HJw9r4MQj9H48nZ6x8WZGmI77rCE8-sWXEYqMhk8qQ'
    'Content-Type' = 'application/json'
}

Write-Host "TESTING AI + EXAM CREATION FLOW" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Yellow

# Step 1: Generate AI questions
Write-Host "Step 1: Generating AI questions..." -ForegroundColor Cyan

$batchRequest = @{
    questionSpecs = @(
        @{
            chapterId = 1
            difficultyLevel = "easy"
            questionType = "multiple_choice"
            count = 3
        }
    )
    saveToDatabase = $false
} | ConvertTo-Json -Depth 10

try {
    $aiResponse = Invoke-WebRequest -Uri "http://localhost:5268/ai-question/generate-batch" -Method POST -Headers $headers -Body $batchRequest
    $aiData = $aiResponse.Content | ConvertFrom-Json
    
    if ($aiData.success) {
        Write-Host "SUCCESS: Generated $($aiData.data.Count) AI questions" -ForegroundColor Green
        $questionIds = $aiData.data | ForEach-Object { $_.questionId }
        
        # Show first question as sample
        $firstQuestion = $aiData.data[0]
        Write-Host "Sample question: $($firstQuestion.questionText.Substring(0, [Math]::Min(60, $firstQuestion.questionText.Length)))..."
        Write-Host "QuestionIds: $($questionIds -join ', ')" -ForegroundColor Gray
    } else {
        throw "AI generation failed: $($aiData.message)"
    }
}
catch {
    Write-Host "ERROR: AI generation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Step 2: Create exam with AI questions
Write-Host "Step 2: Creating exam with AI questions..." -ForegroundColor Cyan

$examRequest = @{
    examName = "AI Generated Physics Test $(Get-Date -Format 'HH:mm:ss')"
    description = "Test exam with AI-generated questions"
    durationMinutes = 30
    examType = "practice"
    createdBy = "admin_user_123"
    isPublished = $false
    questions = @()
} 

# Add AI questions to exam
for ($i = 0; $i -lt $questionIds.Count; $i++) {
    $examRequest.questions += @{
        questionId = $questionIds[$i]
        questionOrder = $i + 1
        pointsWeight = 1
    }
}

$examBody = $examRequest | ConvertTo-Json -Depth 10

try {
    $examResponse = Invoke-WebRequest -Uri "http://localhost:5268/exams" -Method POST -Headers $headers -Body $examBody
    $examData = $examResponse.Content | ConvertFrom-Json
    
    Write-Host "SUCCESS: Exam created successfully!" -ForegroundColor Green
    Write-Host "Exam ID: $($examData.examId)" -ForegroundColor Gray
    Write-Host "Title: $($examData.examName)" -ForegroundColor Gray
    
    Write-Host "FULL TEST COMPLETED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "- AI questions generated"
    Write-Host "- Exam created with AI questions" 
    Write-Host "- Database foreign key constraint resolved"
    
} 
catch {
    $errorResponse = ""
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorResponse = $reader.ReadToEnd()
    }
    
    Write-Host "ERROR: Exam creation failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($errorResponse) {
        Write-Host "Error details: $errorResponse" -ForegroundColor Red
    }
    exit 1
} 