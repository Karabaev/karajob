param(
  [Parameter(Mandatory = $true)]
  [string]$Name
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
  dotnet tool restore
  if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }

  # One entry per provider, so migration sets never drift apart.
  $projects = @('src/Karajob.Migrations.Sqlite')
  foreach ($project in $projects) {
    dotnet ef migrations add $Name --project $project --startup-project $project --output-dir Migrations
    if ($LASTEXITCODE -ne 0) { throw "dotnet ef migrations add failed for $project." }
  }
}
finally {
  Pop-Location
}
