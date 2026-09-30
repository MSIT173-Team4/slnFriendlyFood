# ============ 第一階段：建置 ============
# 用包含完整 SDK 的映像檔來編譯
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 先只複製 .csproj 再 restore：
# Docker 會快取每一層，只要 .csproj 沒變，下次建置就直接跳過下載套件
COPY prjFriendlyFoodWebAPI/prjFriendlyFoodWebAPI.csproj prjFriendlyFoodWebAPI/
RUN dotnet restore prjFriendlyFoodWebAPI/prjFriendlyFoodWebAPI.csproj

# 再複製全部原始碼並發行
COPY prjFriendlyFoodWebAPI/ prjFriendlyFoodWebAPI/
RUN dotnet publish prjFriendlyFoodWebAPI/prjFriendlyFoodWebAPI.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ============ 第二階段：執行 ============
# 只包含執行環境（沒有編譯器），映像檔小很多、攻擊面也小
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# 確保上傳資料夾存在（雲端會被 Cloud Storage 掛載蓋過去）
RUN mkdir -p wwwroot/ProductImageUploads wwwroot/image/Member

# Cloud Run 預設把流量送到 8080
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "prjFriendlyFoodWebAPI.dll"]