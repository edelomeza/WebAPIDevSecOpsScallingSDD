#Requires -Version 5.1
<#
.SYNOPSIS
  Verifica que docs/endpoints.md liste toda action de Controllers/V1 + sondas probe.
  Falla (exit 1) si alguna ruta del codigo no tiene fila "| VERBO | ruta |".
  Solo no-prod: las sondas probe viven en Program.cs tras EnableProviderStates.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$controllersDir = Join-Path $repoRoot 'WebAPIDevSecOpsScallingSDD/Controllers/V1'
$docPath = Join-Path $repoRoot 'docs/endpoints.md'
$programPath = Join-Path $repoRoot 'WebAPIDevSecOpsScallingSDD/Program.cs'

if (-not (Test-Path -LiteralPath $docPath)) {
  Write-Output "endpoints: FAIL (falta docs/endpoints.md)"
  exit 1
}
$doc = Get-Content -LiteralPath $docPath -Raw

function Resolve-Route([string]$template, [string]$base, [string]$token) {
  $t = $template
  if ($t.StartsWith('~')) { $t = $t.Substring(1) }
  elseif ($t -eq '') { $t = $base }
  else { $t = $base + '/' + $t }
  $t = $t.Replace('v{version:apiVersion}', 'v1')
  $t = $t.Replace('[controller]', $token)
  if (-not $t.StartsWith('/')) { $t = '/' + $t }
  # Minúsculas fuera de placeholders {..} (respeta {idVenta:int} del código).
  $parts = [regex]::Split($t, '(\{[^}]*\})')
  for ($i = 0; $i -lt $parts.Count; $i++) {
    if (-not $parts[$i].StartsWith('{')) { $parts[$i] = $parts[$i].ToLowerInvariant() }
  }
  return (-join $parts)
}

$expected = @()
$files = Get-ChildItem -LiteralPath $controllersDir -Filter '*.cs'
foreach ($f in $files) {
  $src = Get-Content -LiteralPath $f.FullName -Raw
  $rm = [regex]::Match($src, '\[Route\("([^"]+)"\)\]')
  if (-not $rm.Success) { continue }
  $base = $rm.Groups[1].Value
  $token = $f.BaseName -replace 'Controller$', ''
  $token = $token.ToLowerInvariant()
  $ams = [regex]::Matches($src, '\[Http(Get|Post|Put|Delete)(?:\("([^"]*)"\))?\]')
  foreach ($am in $ams) {
    $verb = $am.Groups[1].Value.ToUpperInvariant()
    $tpl = $am.Groups[2].Value
    $route = Resolve-Route $tpl $base $token
    $expected += ('| ' + $verb + ' | ' + $route + ' |')
  }
}

$prog = Get-Content -LiteralPath $programPath -Raw
$pms = [regex]::Matches($prog, 'app\.MapGet\("([^"]+)"')
foreach ($pm in $pms) {
  $route = $pm.Groups[1].Value.ToLowerInvariant()
  $expected += ('| GET | ' + $route + ' |')
}

$missing = @()
foreach ($e in $expected) {
  if (-not $doc.Contains($e)) { $missing += $e }
}

if ($missing.Count -gt 0) {
  Write-Output ('endpoints: FAIL (' + $missing.Count + ' rutas sin fila)')
  foreach ($m in $missing) { Write-Output ('  falta: ' + $m) }
  exit 1
}
Write-Output ('endpoints: OK (' + $expected.Count + ' rutas con fila)')
exit 0
