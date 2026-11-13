# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: the restore layer stays cached until dependencies change.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Karajob.Domain/Karajob.Domain.csproj src/Karajob.Domain/
COPY src/Karajob.Application/Karajob.Application.csproj src/Karajob.Application/
COPY src/Karajob.Infrastructure/Karajob.Infrastructure.csproj src/Karajob.Infrastructure/
COPY src/Karajob.Migrations.Sqlite/Karajob.Migrations.Sqlite.csproj src/Karajob.Migrations.Sqlite/
COPY src/Karajob.Web/Karajob.Web.csproj src/Karajob.Web/
RUN dotnet restore src/Karajob.Web/Karajob.Web.csproj

COPY src/ src/
RUN dotnet publish src/Karajob.Web/Karajob.Web.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Owned by the non-root app user so the container also works without a mounted volume.
RUN mkdir /data && chown app:app /data

COPY --from=build /app .

ENV DataProtection__KeysDirectory=/data/keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Karajob.Web.dll"]
