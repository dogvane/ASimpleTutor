Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Repo root = parent of this scripts/ directory
$repoRoot = Split-Path -Parent $PSScriptRoot

$targets = @(
  'tests/ASimpleTutor.Tests/bin',
  'tests/ASimpleTutor.Tests/obj',
  'tests/ASimpleTutor.IntegrationTests/bin',
  'tests/ASimpleTutor.IntegrationTests/obj',
  'src/ASimpleTutor.Core/bin',
  'src/ASimpleTutor.Core/obj',
  'src/ASimpleTutor.Api/bin',
  'src/ASimpleTutor.Api/obj'
)

Write-Host "Cleaning build outputs under: $repoRoot"

foreach ($rel in $targets) {
  $path = Join-Path $repoRoot $rel
  if (Test-Path -LiteralPath $path) {
    Write-Host "- Removing $rel"
    Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
  } else {
    Write-Host "- Skipping (not found) $rel"
  }
}

Write-Host 'Clean complete.'
