# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Copy the project file(s) and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the rest of the source code and build the app
COPY . ./
RUN dotnet publish -c Release -o out

# Use the official ASP.NET runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/out ./

# Expose the port your app runs on (default 80 for ASP.NET Core)
EXPOSE 80

# Set the entry point to run the app
ENTRYPOINT ["dotnet", "CitasEPS.dll"]
