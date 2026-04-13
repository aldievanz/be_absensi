# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["SmartAttendanceApi.csproj", "./"]
RUN dotnet restore "SmartAttendanceApi.csproj"
COPY . .
RUN dotnet publish "SmartAttendanceApi.csproj" -c Release -o /app/publish

# Serve Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (Render sets PORT environment variable dynamically)
ENV ASPNETCORE_URLS=http://+:${PORT}
ENTRYPOINT ["dotnet", "SmartAttendanceApp.dll"]
