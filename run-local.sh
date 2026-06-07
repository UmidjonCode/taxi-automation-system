#!/usr/bin/env bash
# Runs all four microservices + the dashboard locally.
# Prereqs: .NET 8 SDK installed, and RabbitMQ running (see infrastructure/docker-compose.yml).
set -euo pipefail
cd "$(dirname "$0")"

echo "Starting HotelOS services… (Ctrl+C to stop all)"

dotnet run --project src/Services/HotelOS.Reception   --urls http://localhost:5001 &
dotnet run --project src/Services/HotelOS.Housekeeping --urls http://localhost:5002 &
dotnet run --project src/Services/HotelOS.RoomService  --urls http://localhost:5003 &
dotnet run --project src/Services/HotelOS.Maintenance  --urls http://localhost:5004 &
dotnet run --project src/Gateway/HotelOS.Dashboard     --urls http://localhost:5000 &

# Stop every background service when this script is interrupted.
trap 'echo; echo "Stopping…"; kill 0' INT TERM
wait
