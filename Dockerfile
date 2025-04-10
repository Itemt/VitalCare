# Use the official ASP.NET Core runtime image as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Copy the project file and restore dependencies
# Ensure the source path matches your project structure if CitasEPS.csproj is not in the root of the context
COPY ["CitasEPS.csproj", "."]
RUN dotnet restore "./CitasEPS.csproj"
# Copy the rest of the application code from the build context (defined in docker-compose.yml)
COPY . .
WORKDIR "/src/."
# Build the application
RUN dotnet build "CitasEPS.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CitasEPS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final image from the base runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Define the entry point for the container
ENTRYPOINT ["dotnet", "CitasEPS.dll"] 