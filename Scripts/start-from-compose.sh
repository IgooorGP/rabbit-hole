#!/bin/bash
PROJECT_NAME=Rabbitcs
CSPROJ_PATH=./$PROJECT_NAME

echo "Running the project..."
dotnet run --project $CSPROJ_PATH --urls $ASPNETCORE_URLS