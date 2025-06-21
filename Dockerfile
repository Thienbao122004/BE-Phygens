# Sử dụng .NET 8.0 SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY *.csproj ./
RUN dotnet restore "BE_Phygens.csproj"

# Copy toàn bộ source code
COPY . .

# Build và publish ứng dụng
RUN dotnet publish "BE_Phygens.csproj" -c Release -o /app/publish --no-restore

# Sử dụng .NET 8.0 runtime để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS runtime
WORKDIR /app

# Copy ứng dụng đã build
COPY --from=build /app/publish .

# Tạo non-root user cho security
RUN addgroup --system --gid 1001 dotnetgroup
RUN adduser --system --uid 1001 --gid 1001 dotnetuser
USER dotnetuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "BE_Phygens.dll"] 