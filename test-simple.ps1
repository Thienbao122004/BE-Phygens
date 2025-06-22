Write-Host "Testing AI connection..." -ForegroundColor Yellow

$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJ1MDAxIiwiZW1haWwiOiJhZG1pbkBleGFtcGxlLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmd1eeG7hW4gVsSDbiBBZG1pbiIsInJvbGUiOiJhZG1pbiIsInVzZXJuYW1lIjoiYWRtaW4iLCJmdWxsX25hbWUiOiJOZ3V54buFbiBWxINuIEFkbWluIiwiaXNfYWN0aXZlIjoiVHJ1ZSIsImp0aSI6IjNmMDI1OTg4LWJiOTgtNDU3Ni04Njg0LWU5MjhkNGIwYzQ3ZCIsImlhdCI6MTc1MDU3NTk4NCwibmJmIjoxNzUwNTc1OTg0LCJleHAiOjE3NTExODA3ODR9._mcDHXH8si4kWhBYFihVQdlsGL8SQVaV7_a_ycEvcfM"

$headers = @{
    "Authorization" = "Bearer $token"
}

try {
    $response = Invoke-RestMethod -Uri "http://localhost:5298/ai-question/test-connection" -Method POST -Headers $headers
    Write-Host "Response received:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 3)
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message
}