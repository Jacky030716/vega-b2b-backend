# Use SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution file and restore all projects
COPY CAproject.sln ./
COPY src/Core/CleanArc.Domain/CleanArc.Domain.csproj src/Core/CleanArc.Domain/
COPY src/Core/CleanArc.Application/CleanArc.Application.csproj src/Core/CleanArc.Application/
COPY src/Infrastructure/CleanArc.Infrastructure.Persistence/CleanArc.Infrastructure.Persistence.csproj src/Infrastructure/CleanArc.Infrastructure.Persistence/
COPY src/Infrastructure/CleanArc.Infrastructure.Identity/CleanArc.Infrastructure.Identity.csproj src/Infrastructure/CleanArc.Infrastructure.Identity/
COPY src/Infrastructure/CleanArc.Infrastructure.CrossCutting/CleanArc.Infrastructure.CrossCutting.csproj src/Infrastructure/CleanArc.Infrastructure.CrossCutting/
COPY src/Shared/CleanArc.SharedKernel/CleanArc.SharedKernel.csproj src/Shared/CleanArc.SharedKernel/
COPY src/API/CleanArc.WebFramework/CleanArc.WebFramework.csproj src/API/CleanArc.WebFramework/
COPY src/API/CleanArc.Web.Api/CleanArc.Web.Api.csproj src/API/CleanArc.Web.Api/
COPY src/API/Plugins/CleanArc.Web.Plugins.Grpc/CleanArc.Web.Plugins.Grpc.csproj src/API/Plugins/CleanArc.Web.Plugins.Grpc/
COPY src/Tests/CleanArc.Tests.Setup/CleanArc.Tests.Setup.csproj src/Tests/CleanArc.Tests.Setup/
COPY src/Tests/CleanArc.Test.Infrastructure.Identity/CleanArc.Test.Infrastructure.Identity/CleanArc.Test.Infrastructure.Identity.csproj src/Tests/CleanArc.Test.Infrastructure.Identity/CleanArc.Test.Infrastructure.Identity/

RUN dotnet restore

# Copy all source files
COPY src/ src/

# Build and publish API in Release configuration
WORKDIR /app/src/API/CleanArc.Web.Api
RUN dotnet publish -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port (Render and Fly.io configure routing automatically using the PORT variable)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CleanArc.Web.Api.dll"]
