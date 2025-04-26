# Use the .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy the solution and project files
COPY *.sln .
COPY *.csproj .
RUN dotnet restore CitasEPS.sln

# Copy the remaining source code and build the application
COPY . .
RUN dotnet publish CitasEPS.csproj -c Release -o /app/publish --no-restore

# Use the ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 for the web application
EXPOSE 8080

# Set the entry point for the application
ENTRYPOINT ["dotnet", "CitasEPS.dll"] 