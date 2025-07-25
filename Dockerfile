# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# 🎯 Create required directories for file uploads
RUN mkdir -p /app/wwwroot/uploads/uploads && \
    mkdir -p /app/wwwroot/uploads/essay-images && \
    mkdir -p /app/wwwroot/chat-images && \
    mkdir -p /app/wwwroot/images && \
    chmod -R 755 /app/wwwroot

# Railway uses PORT environment variable
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "BE_Phygens.dll"] 