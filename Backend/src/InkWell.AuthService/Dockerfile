# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/src/InkWell.AuthService/InkWell.AuthService.csproj", "Backend/src/InkWell.AuthService/"]
RUN dotnet restore "Backend/src/InkWell.AuthService/InkWell.AuthService.csproj"
COPY . .
WORKDIR "/src/Backend/src/InkWell.AuthService"
RUN dotnet build "InkWell.AuthService.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InkWell.AuthService.dll"]
