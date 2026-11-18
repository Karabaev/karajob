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
# .git is not in the build context, so the commit hash shown in the UI comes from this argument.
ARG SOURCE_REVISION=
RUN dotnet publish src/Karajob.Web/Karajob.Web.csproj -c Release --no-restore -o /app -p:SourceRevisionId=$SOURCE_REVISION

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The aspnet image has no /usr/share/zoneinfo, so TimeZoneInfo would only know UTC.
USER root
RUN apt-get update \
  && apt-get install -y --no-install-recommends tzdata \
  && rm -rf /var/lib/apt/lists/*

# Owned by the non-root app user so the container also works without a mounted volume.
RUN mkdir /data && chown app:app /data

COPY --from=build /app .

ENV DataProtection__KeysDirectory=/data/keys
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Karajob.Web.dll"]
