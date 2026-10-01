FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SmartBot.Api/SmartBot.Api.csproj SmartBot.Api/
RUN dotnet restore SmartBot.Api/SmartBot.Api.csproj
COPY SmartBot.Api/ SmartBot.Api/
RUN dotnet publish SmartBot.Api/SmartBot.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartBot.Api.dll"]