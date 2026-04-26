# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/src/InkWell.NotificationService/InkWell.NotificationService.csproj", "Backend/src/InkWell.NotificationService/"]
RUN dotnet restore "Backend/src/InkWell.NotificationService/InkWell.NotificationService.csproj"
COPY . .
WORKDIR "/src/Backend/src/InkWell.NotificationService"
RUN dotnet build "InkWell.NotificationService.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.NotificationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InkWell.NotificationService.dll"]
