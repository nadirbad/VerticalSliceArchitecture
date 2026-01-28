#!/bin/bash
set -e

echo "Restoring .NET packages..."
dotnet restore

echo "Building solution..."
dotnet build --no-restore

echo "Waiting for PostgreSQL to be ready..."
until pg_isready -h postgres -p 5432 -U postgres; do
  sleep 1
done

echo "Applying EF Core migrations..."
dotnet ef database update --project src/Application --startup-project src/Api

echo "Dev container setup complete!"
echo ""
echo "Quick start:"
echo "  dotnet run --project src/Api    # Start the API (Swagger at http://localhost:5206/swagger)"
echo "  dotnet test                     # Run all tests"
echo "  dotnet watch --project src/Api  # Start with hot reload"
