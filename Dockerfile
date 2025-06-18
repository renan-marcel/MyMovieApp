ARG LAUNCHING_FROM_VS
ARG FINAL_BASE_IMAGE=${LAUNCHING_FROM_VS:+aotdebug}

# This stage is used when running in VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install clang/zlib1g-dev dependencies for native publishing
RUN apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MyMovieApp.API/MyMovieApp.API.csproj", "src/MyMovieApp.API/"]
COPY ["src/MyMovieApp.Application/MyMovieApp.Application.csproj", "src/MyMovieApp.Application/"]
COPY ["src/MyMovieApp.Domain/MyMovieApp.Domain.csproj", "src/MyMovieApp.Domain/"]
COPY ["src/MyMovieApp.Infrastructure/MyMovieApp.Infrastructure.csproj", "src/MyMovieApp.Infrastructure/"]
RUN dotnet restore "./src/MyMovieApp.API/MyMovieApp.API.csproj"
COPY . .
WORKDIR "/src/src/MyMovieApp.API"
RUN dotnet build "./MyMovieApp.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MyMovieApp.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used as a base for the final stage when launching in VS to support debugging in normal mode (Default when not using Debug configuration)
FROM base AS aotdebug
USER root
# Install GDB to support native debugging
RUN apt-get update && apt-get install -y --no-install-recommends  gdb curl

USER app

# This stage is used in production or when running in VS in normal mode (default when not using Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyMovieApp.API.dll"]