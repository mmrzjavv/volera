# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/WebAPI/WebAPI.csproj", "src/WebAPI/"]
COPY ["src/Core.Application/Core.Application.csproj", "src/Core.Application/"]
COPY ["src/Core.Domain/Core.Domain.csproj", "src/Core.Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]

RUN dotnet restore "src/WebAPI/WebAPI.csproj"
COPY . .
RUN dotnet build "src/WebAPI/WebAPI.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "src/WebAPI/WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebAPI.dll"]
