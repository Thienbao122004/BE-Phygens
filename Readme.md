# Hướng dẫn Deploy dự án Phygens lên Railway
taskkill /f /im BE_Phygens.exe
## Bước 1: Chuẩn bị dự án

Dự án đã được cấu hình sẵn các file cần thiết:
- ✅ `Dockerfile` - Container configuration
- ✅ `.dockerignore` - Loại bỏ file không cần thiết
- ✅ `appsettings.Production.json` - Cấu hình production
- ✅ `Program.cs` đã được cập nhật để hỗ trợ Railway

## Bước 2: Push code lên GitHub

```bash
git add .
git commit -m "Configure for Railway deployment"
git push origin main
```

## Bước 3: Deploy trên Railway

### Cách 1: Deploy từ GitHub (Khuyến nghị)

1. Truy cập [railway.app](https://railway.app)
2. Đăng nhập bằng GitHub
3. Chọn "New Project" → "Deploy from GitHub repo"
4. Chọn repository `AdmissionInfoSystem`
5. Railway sẽ tự động detect Dockerfile và bắt đầu build

### Cách 2: Deploy bằng Railway CLI

```bash
# Cài đặt Railway CLI
npm install -g @railway/cli

# Đăng nhập
railway login

# Khởi tạo project
railway init

# Deploy
railway up
```

## Bước 4: Cấu hình biến môi trường

Trong Railway Dashboard, vào phần **Variables** và thêm:

### Database Configuration (Supabase)
```
SUPABASE_CONNECTION_STRING=User Id=postgres.rgjnylthyxydbcghbllq;Password=IloveYou3000!123;Server=aws-0-ap-southeast-1.pooler.supabase.com;Port=5432;Database=postgres
```

### JWT Configuration
```
JWT_KEY=ThisIsMySecretKeyForAdmissionInfoSystem12345
JWT_ISSUER=AdmissionInfoSystem
JWT_AUDIENCE=AdmissionInfoSystemClient
```

### Environment Configuration
```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
```

## Bước 5: Kiểm tra deployment

1. **Kiểm tra logs**: Trong Railway dashboard, xem tab "Deployments" để theo dõi quá trình build
2. **Test API**: 
   - Truy cập `https://your-app.railway.app/swagger`
   - Test các endpoint API
3. **Kiểm tra database connection**: Test các API liên quan đến database

## Bước 6: Cấu hình Domain (Tùy chọn)

- Railway tự động cung cấp subdomain: `your-app.railway.app`
- Có thể cấu hình custom domain trong phần "Settings" → "Domains"

## Troubleshooting

### Lỗi thường gặp:

1. **Build failed**: Kiểm tra Dockerfile và dependencies trong `.csproj`
2. **Database connection**: Đảm bảo Supabase connection string đúng
3. **Port binding**: Ứng dụng đã được cấu hình để sử dụng PORT từ environment

### Kiểm tra logs:
```bash
railway logs
```

### Restart service:
```bash
railway restart
```

## Cấu trúc URL sau khi deploy

- **API Base**: `https://your-app.railway.app`
- **Swagger UI**: `https://your-app.railway.app/swagger`
- **Health Check**: `https://your-app.railway.app` (redirect to swagger)

## Lưu ý quan trọng

1. **Database**: Dự án sử dụng Supabase PostgreSQL, không cần setup database trên Railway
2. **HTTPS**: Railway tự động cung cấp SSL certificate
3. **Auto-deploy**: Mỗi khi push code lên GitHub, Railway sẽ tự động deploy lại
4. **Scaling**: Railway hỗ trợ auto-scaling based on traffic

## Environment Variables Summary

| Variable | Value | Description |
|----------|--------|-------------|
| `SUPABASE_CONNECTION_STRING` | Supabase connection string | Database connection |
| `JWT_KEY` | JWT secret key | Authentication |
| `JWT_ISSUER` | AdmissionInfoSystem | JWT issuer |
| `JWT_AUDIENCE` | AdmissionInfoSystemClient | JWT audience |
| `ASPNETCORE_ENVIRONMENT` | Production | Environment mode | 