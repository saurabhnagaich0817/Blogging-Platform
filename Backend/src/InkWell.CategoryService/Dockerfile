# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/src/InkWell.CategoryService/InkWell.CategoryService.csproj", "Backend/src/InkWell.CategoryService/"]
RUN dotnet restore "Backend/src/InkWell.CategoryService/InkWell.CategoryService.csproj"
COPY . .
WORKDIR "/src/Backend/src/InkWell.CategoryService"
RUN dotnet build "InkWell.CategoryService.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.CategoryService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InkWell.CategoryService.dll"]
