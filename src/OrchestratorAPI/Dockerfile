# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["MicrosoftCopilotDemo.slnx", "./"]
COPY ["src/OrchestratorAPI/OrchestratorAPI.csproj", "src/OrchestratorAPI/"]

# Restore dependencies
RUN dotnet restore "src/OrchestratorAPI/OrchestratorAPI.csproj"

# Copy source code
COPY ["src/OrchestratorAPI/", "src/OrchestratorAPI/"]

# Build
WORKDIR "/src/src/OrchestratorAPI"
RUN dotnet build "OrchestratorAPI.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "OrchestratorAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published app
COPY --from=publish /app/publish .

# Set entry point
ENTRYPOINT ["dotnet", "OrchestratorAPI.dll"]
