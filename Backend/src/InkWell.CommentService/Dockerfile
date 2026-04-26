# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Backend/src/InkWell.Shared/InkWell.Shared.csproj", "Backend/src/InkWell.Shared/"]
COPY ["Backend/src/InkWell.CommentService/InkWell.CommentService.csproj", "Backend/src/InkWell.CommentService/"]
RUN dotnet restore "Backend/src/InkWell.CommentService/InkWell.CommentService.csproj"
COPY . .
WORKDIR "/src/Backend/src/InkWell.CommentService"
RUN dotnet build "InkWell.CommentService.csproj" -c Release -o /app/build
RUN dotnet publish "InkWell.CommentService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InkWell.CommentService.dll"]
