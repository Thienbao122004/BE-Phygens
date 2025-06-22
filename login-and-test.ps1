# Login and Test AI Connection
Write-Host "Step 1: Login to get JWT token..." -ForegroundColor Yellow

$loginBody = @{
    username = "admin"
    password = "123"
} | ConvertTo-Json

$loginHeaders = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

try {
    # Login to get token
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5298/auth/login" -Method POST -Body $loginBody -Headers $loginHeaders
    
    Write-Host "Login response:" -ForegroundColor Cyan
    Write-Host ($loginResponse | ConvertTo-Json -Depth 3)
    
    if ($loginResponse.success) {
        Write-Host "Login successful!" -ForegroundColor Green
        $token = $loginResponse.data.access_token
        Write-Host "Token: $($token.Substring(0, 50))..." -ForegroundColor Cyan
        
        # Test AI connection with new token
        Write-Host "`nStep 2: Testing AI connection..." -ForegroundColor Yellow
        
        $aiHeaders = @{
            "Authorization" = "Bearer $token"
            "Accept" = "application/json"
        }
        
        $aiResponse = Invoke-RestMethod -Uri "http://localhost:5298/ai-question/test-connection" -Method POST -Headers $aiHeaders
        
        Write-Host "AI Test Response:" -ForegroundColor Green
        Write-Host ($aiResponse | ConvertTo-Json -Depth 3)
        
        if ($aiResponse.success) {
            Write-Host "AI Connection: SUCCESS!" -ForegroundColor Green
        } else {
            Write-Host "AI Connection: FAILED" -ForegroundColor Red
            Write-Host "Message: $($aiResponse.message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Login failed!" -ForegroundColor Red
        Write-Host ($loginResponse | ConvertTo-Json)
    }
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message
} 