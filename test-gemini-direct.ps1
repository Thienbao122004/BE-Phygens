 # Test Gemini API directly
Write-Host "Testing Gemini API directly..." -ForegroundColor Yellow

$apiKey = "AIzaSyBUVcfSYFtmZGPV7t3pp9z53qGdGYYqOmE"
$model = "gemini-1.5-flash"
$url = "https://generativelanguage.googleapis.com/v1beta/models/$model`:generateContent?key=$apiKey"

Write-Host "URL: $url" -ForegroundColor Cyan

$requestBody = @{
    contents = @(
        @{
            parts = @(
                @{
                    text = "Test connection. Reply with 'OK'."
                }
            )
        }
    )
} | ConvertTo-Json -Depth 5

Write-Host "Request Body:" -ForegroundColor Cyan
Write-Host $requestBody

$headers = @{
    "Content-Type" = "application/json"
}

try {
    Write-Host "Calling Gemini API..." -ForegroundColor Yellow
    $response = Invoke-RestMethod -Uri $url -Method POST -Body $requestBody -Headers $headers
    
    Write-Host "Success! Response:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 5)
    
    $text = $response.candidates[0].content.parts[0].text
    Write-Host "Extracted text: '$text'" -ForegroundColor Green
}
catch {
    Write-Host "Error:" -ForegroundColor Red
    Write-Host $_.Exception.Message
    
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
        try {
            $errorStream = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorStream)
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error Response: $errorContent" -ForegroundColor Red
        } catch {
            Write-Host "Could not read error response" -ForegroundColor Red
        }
    }
}