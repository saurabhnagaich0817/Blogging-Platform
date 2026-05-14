param (
    [string]$BackendToken,
    [string]$FrontendToken,
    [string]$HostUrl = "http://localhost:9000"
)

Write-Host "Starting Quality Analysis..." -ForegroundColor Cyan

# 1. Backend
Write-Host "Analyzing Backend..." -ForegroundColor Blue
dotnet sonarscanner begin /k:"InkWell-Backend" /d:sonar.host.url="$HostUrl" /d:sonar.login="$BackendToken"
dotnet build Backend/InkWell.sln --no-incremental
dotnet sonarscanner end /d:sonar.login="$BackendToken"

# 2. Frontend
Write-Host "Analyzing Frontend..." -ForegroundColor Yellow
Set-Location -Path "Frontend/inkwell-frontend"

# Clear broken JAVA_HOME
$env:JAVA_HOME = $null

# Using npx with quoted arguments for PowerShell stability
npx sonar-scanner "-Dsonar.login=$FrontendToken" "-Dsonar.host.url=$HostUrl" "-Dsonar.projectKey=InkWell-Frontend"
Set-Location -Path "../../"

Write-Host "Analysis Complete! Check http://localhost:9000" -ForegroundColor Green
