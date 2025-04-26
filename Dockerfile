# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the project file and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy the remaining source code and build the application
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 for the web application
EXPOSE 8080

# Set the entry point for the application
ENTRYPOINT ["dotnet", "CitasEPS.dll"] 