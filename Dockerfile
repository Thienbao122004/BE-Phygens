# Sử dụng SDK để build ứng dụng
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies
COPY BE_Phygens.csproj .
RUN dotnet restore

# Copy toàn bộ source code và build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Sử dụng runtime image để chạy ứng dụng
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy ứng dụng đã build
COPY --from=build /app/publish .

# Expose port (Railway sẽ tự động set PORT)
EXPOSE 8080

# Set environment variable để ứng dụng listen trên đúng port
ENV ASPNETCORE_URLS=http://+:$PORT

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "BE_Phygens.dll"] 