# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["LeetCodeDaily.Web/LeetCodeDaily.Web.csproj", "LeetCodeDaily.Web/"]
RUN dotnet restore "LeetCodeDaily.Web/LeetCodeDaily.Web.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
WORKDIR "/src/LeetCodeDaily.Web"
RUN dotnet build "LeetCodeDaily.Web.csproj" -c Release -o /app/build
RUN dotnet publish "LeetCodeDaily.Web.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directory for solutions
RUN mkdir -p /app/Solutions

# Copy published files
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "LeetCodeDaily.Web.dll"] 