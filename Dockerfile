# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/gateway/InkWell.API.Gateway/InkWell.API.Gateway.csproj", "Backend/gateway/InkWell.API.Gateway/"]
RUN dotnet restore "Backend/gateway/InkWell.API.Gateway/InkWell.API.Gateway.csproj"
COPY . .
WORKDIR "/src/Backend/gateway/InkWell.API.Gateway"
RUN dotnet build "InkWell.API.Gateway.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.API.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Hugging Face uses port 7860
EXPOSE 7860
ENV ASPNETCORE_URLS=http://*:7860

ENTRYPOINT ["dotnet", "InkWell.AuthService.dll"]
