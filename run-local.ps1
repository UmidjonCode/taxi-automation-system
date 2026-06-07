$ErrorActionPreference = "Stop"

Write-Host "Starting HotelOS services... (Press Ctrl+C to stop all)" -ForegroundColor Cyan

# Start all microservices in the background, ensuring the correct working directory
$jobs = @()
$jobs += Start-Process -FilePath "dotnet" -ArgumentList "bin/Debug/net10.0/HotelOS.Reception.dll --urls http://localhost:5001" -WorkingDirectory "src/Services/HotelOS.Reception" -PassThru
$jobs += Start-Process -FilePath "dotnet" -ArgumentList "bin/Debug/net10.0/HotelOS.Housekeeping.dll --urls http://localhost:5002" -WorkingDirectory "src/Services/HotelOS.Housekeeping" -PassThru
$jobs += Start-Process -FilePath "dotnet" -ArgumentList "bin/Debug/net10.0/HotelOS.RoomService.dll --urls http://localhost:5003" -WorkingDirectory "src/Services/HotelOS.RoomService" -PassThru
$jobs += Start-Process -FilePath "dotnet" -ArgumentList "bin/Debug/net10.0/HotelOS.Maintenance.dll --urls http://localhost:5004" -WorkingDirectory "src/Services/HotelOS.Maintenance" -PassThru
$jobs += Start-Process -FilePath "dotnet" -ArgumentList "bin/Debug/net10.0/HotelOS.Dashboard.dll --urls http://localhost:5000" -WorkingDirectory "src/Gateway/HotelOS.Dashboard" -PassThru

try {
    Write-Host "Services are starting up! Check the new console windows." -ForegroundColor Green
    Write-Host "Press Ctrl+C in this window to stop all services." -ForegroundColor Yellow
    # Wait indefinitely until user presses Ctrl+C
    while ($true) {
        Start-Sleep -Seconds 1
    }
}
finally {
    Write-Host "Stopping all services..." -ForegroundColor Cyan
    foreach ($job in $jobs) {
        if (-not $job.HasExited) {
            Stop-Process -Id $job.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "All services stopped." -ForegroundColor Green
}
