# Alpha Vantage API Key Setup Script
# This script helps you configure your Alpha Vantage API key securely using .NET User Secrets

Write-Host "=== Alpha Vantage API Key Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check if we're in the correct directory
$apiProjectPath = "src\StockSensePro.API"
if (-not (Test-Path $apiProjectPath)) {
    Write-Host "Error: Please run this script from the backend directory" -ForegroundColor Red
    Write-Host "Current directory: $(Get-Location)" -ForegroundColor Yellow
    exit 1
}

# Prompt for API key
Write-Host "Enter your Alpha Vantage API key:" -ForegroundColor Yellow
Write-Host "(Get one free at: https://www.alphavantage.co/support/#api-key)" -ForegroundColor Gray
$apiKey = Read-Host "API Key"

if ([string]::IsNullOrWhiteSpace($apiKey)) {
    Write-Host "Error: API key cannot be empty" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Setting up User Secrets..." -ForegroundColor Cyan

# Navigate to API project
Push-Location $apiProjectPath

try {
    # Set the API key
    Write-Host "Setting Alpha Vantage API key..." -ForegroundColor Gray
    dotnet user-secrets set "AlphaVantage:ApiKey" $apiKey
    
    # Enable Alpha Vantage
    Write-Host "Enabling Alpha Vantage provider..." -ForegroundColor Gray
    dotnet user-secrets set "AlphaVantage:Enabled" "true"
    
    Write-Host ""
    Write-Host "=== Configuration Options ===" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Choose your provider strategy:" -ForegroundColor Yellow
    Write-Host "1. Use Yahoo Finance as primary (Alpha Vantage as fallback) - RECOMMENDED" -ForegroundColor Green
    Write-Host "2. Use Alpha Vantage as primary (Yahoo Finance as fallback)" -ForegroundColor White
    Write-Host "3. Use Alpha Vantage only (no fallback)" -ForegroundColor White
    Write-Host ""
    
    $choice = Read-Host "Enter choice (1-3)"
    
    switch ($choice) {
        "1" {
            Write-Host "Configuring Yahoo Finance as primary with Alpha Vantage fallback..." -ForegroundColor Gray
            dotnet user-secrets set "DataProvider:PrimaryProvider" "YahooFinance"
            dotnet user-secrets set "DataProvider:FallbackProvider" "AlphaVantage"
            dotnet user-secrets set "DataProvider:Strategy" "Fallback"
            dotnet user-secrets set "DataProvider:EnableAutomaticFallback" "true"
        }
        "2" {
            Write-Host "Configuring Alpha Vantage as primary with Yahoo Finance fallback..." -ForegroundColor Gray
            dotnet user-secrets set "DataProvider:PrimaryProvider" "AlphaVantage"
            dotnet user-secrets set "DataProvider:FallbackProvider" "YahooFinance"
            dotnet user-secrets set "DataProvider:Strategy" "Fallback"
            dotnet user-secrets set "DataProvider:EnableAutomaticFallback" "true"
        }
        "3" {
            Write-Host "Configuring Alpha Vantage only (no fallback)..." -ForegroundColor Gray
            dotnet user-secrets set "DataProvider:PrimaryProvider" "AlphaVantage"
            dotnet user-secrets set "DataProvider:Strategy" "Primary"
        }
        default {
            Write-Host "Invalid choice. Using default (Yahoo Finance primary)..." -ForegroundColor Yellow
            dotnet user-secrets set "DataProvider:PrimaryProvider" "YahooFinance"
            dotnet user-secrets set "DataProvider:FallbackProvider" "AlphaVantage"
            dotnet user-secrets set "DataProvider:Strategy" "Fallback"
        }
    }
    
    Write-Host ""
    Write-Host "=== Setup Complete! ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your configuration:" -ForegroundColor Cyan
    dotnet user-secrets list | Select-String "AlphaVantage|DataProvider"
    
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Run the application: dotnet run" -ForegroundColor White
    Write-Host "2. Test the health endpoint: curl http://localhost:5566/api/health" -ForegroundColor White
    Write-Host "3. Check metrics: curl http://localhost:5566/api/health/metrics" -ForegroundColor White
    Write-Host ""
    Write-Host "Note: Free tier limits are 25 requests/day, 5 requests/minute" -ForegroundColor Gray
    Write-Host "Upgrade at: https://www.alphavantage.co/premium/" -ForegroundColor Gray
    
} catch {
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}
