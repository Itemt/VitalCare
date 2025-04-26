# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the solution and project files
COPY *.sln .
COPY *.csproj .
EXPOSE 8000
ENTRYPOINT ["dotnet", "RUN"] 