FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only the .csproj files first so the restore layer is cached
COPY iDoctor.Api/iDoctor.Api.csproj iDoctor.Api/
COPY iDoctor.Domain/iDoctor.Domain.csproj iDoctor.Domain/
COPY iDoctor.Infrastructure/iDoctor.Infrastructure.csproj iDoctor.Infrastructure/
RUN dotnet restore iDoctor.Api/iDoctor.Api.csproj

COPY . .
RUN dotnet publish iDoctor.Api/iDoctor.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "iDoctor.Api.dll"]