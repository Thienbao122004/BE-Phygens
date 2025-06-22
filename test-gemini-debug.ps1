# Test Gemini API Connection
Write-Host "Testing Gemini API Connection..." -ForegroundColor Yellow

$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImp0aSI6IjE4MjE1ZGI0LWVhZTMtNGNlZi04YzBjLTVhNjE5ZjJjODY5OSIsInJvbGUiOiJBZG1pbiIsImV4cCI6MTczNjI2ODQ1MSwiaXNzIjoiUGh5Z2VucyIsImF1ZCI6IlBoeWdlbnMifQ.eyhbGcl0i3UzlNMisInR5SInVuXF17V9uXl1JolTmd1ecc1nVbdJHZG1pbK1pIsInfV2ZJuWl1JoiTmd1ecc"

$headers = @{
    "Authorization" = "Bearer $token"
    "Accept" = "application/json"
}

try {
    Write-Host "Calling API..." -ForegroundColor Cyan
    $response = Invoke-RestMethod -Uri "http://localhost:5298/ai-question/test-connection" -Method POST -Headers $headers
    
    Write-Host "Response:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3)
    
    if ($response.success) {
        Write-Host "SUCCESS!" -ForegroundColor Green
    } else {
        Write-Host "FAILED" -ForegroundColor Red
    }
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message
} 