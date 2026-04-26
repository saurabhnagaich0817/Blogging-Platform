# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/src/InkWell.PostService/InkWell.PostService.csproj", "Backend/src/InkWell.PostService/"]
RUN dotnet restore "Backend/src/InkWell.PostService/InkWell.PostService.csproj"
COPY . .
WORKDIR "/src/Backend/src/InkWell.PostService"
RUN dotnet build "InkWell.PostService.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.PostService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InkWell.PostService.dll"]
