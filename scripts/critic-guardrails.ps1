# critic-guardrails.ps1 — checklist ligero local y de CI (Fase 1).
# Bloqueantes (exit 1): marcadores TODO, secretos en DTOs/cache, auth ausente, catch nuevo en Controllers.
# Ambito: todo el analisis es diff-scoped contra origin/main: la deuda pre-03-16 ya mergeada no falla.
# Compatible PowerShell 5.1 (sin ?. / ?? / -Parallel). Exit 0 PASS, 1 FAIL.

$ErrorActionPreference = 'Stop'

$failures = @()
$warnings = @()

# Estilo powershell-quirks: el stderr nativo (p. ej. "From https://..." de
# git fetch) se vuelve terminating con $ErrorActionPreference='Stop' aunque
# haya 2>$null. Todo git pasa por aqui: 2>&1 + filtrado de ErrorRecord.
function Invoke-Git {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        return @(git @args 2>&1 | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] })
    } finally {
        $ErrorActionPreference = $prev
    }
}

function Get-ChangedFiles {
    # Union de ambos ambitos: el diff commiteado vs origin/main (caso CI)
    # mas los cambios locales sin commitear (caso pre-push). Sin la union,
    # un scratch untracked pasaba desapercibido cuando el diff era no vacio.
    $all = @()
    Invoke-Git fetch origin main --depth=100 | Out-Null
    $out = Invoke-Git diff --name-only 'origin/main...HEAD'
    if ($out) { $all += @($out | Where-Object { $_ -ne '' }) }
    $unstaged = Invoke-Git diff --name-only HEAD
    if ($unstaged) { $all += @($unstaged | Where-Object { $_ -ne '' }) }
    $untracked = Invoke-Git ls-files --others --exclude-standard
    if ($untracked) { $all += @($untracked | Where-Object { $_ -ne '' }) }
    return @($all | Select-Object -Unique)
}

function Get-AddedLines($file) {
    $tracked = $true
    $chk = Invoke-Git ls-files --error-unmatch -- $file
    if (-not $chk) { $tracked = $false }
    if (-not $tracked) {
        return @(Get-Content -LiteralPath $file)
    }
    $diff = Invoke-Git diff -U0 'origin/main...HEAD' -- $file
    if (-not $diff) { $diff = Invoke-Git diff -U0 HEAD -- $file }
    return @($diff | Where-Object { $_ -match '^\+' -and $_ -notmatch '^\+\+\+' })
}

$changed = Get-ChangedFiles
if ($changed.Count -eq 0) {
    Write-Host 'critic: no changed files detected — PASS'
    exit 0
}

$codeExt = @('.cs', '.ps1', '.yml', '.yaml', '.json', '.csproj', '.props', '.targets')

foreach ($f in $changed) {
    if (-not (Test-Path -LiteralPath $f)) { continue }
    $isTest = $f -match 'Tests?\.cs$|/test/|/tests/'
    $ext = [System.IO.Path]::GetExtension($f)

    # B1: marcadores TODO reales en codigo/config (prosa con "TODO" no cuenta).
    if ($codeExt -contains $ext) {
        $n = 0
        foreach ($line in (Get-Content -LiteralPath $f)) {
            $n++
            if ($line -match '(?i)(//|#)\s*TODO\b') {
                $failures += "${f}:${n} TODO marker (usar NOTE (XX-YY) -> fase duena)"
            }
        }
    }

    # B2a: password/secret en DTOs (fuera de tests, que los asertan por ausencia).
    if ($f -match '(?i)Dtos' -and $f -match '\.cs$' -and -not $isTest) {
        $n = 0
        foreach ($line in (Get-Content -LiteralPath $f)) {
            $n++
            if ($line -match '(?i)password|secret') {
                $failures += "${f}:${n} posible secreto en DTO"
            }
        }
    }

    # B2b: 'token' dentro de literales cache:* salvo blacklist:{jti}.
    if ($f -match '\.cs$' -and -not $isTest) {
        $n = 0
        foreach ($line in (Get-Content -LiteralPath $f)) {
            $n++
            if ($line -match 'cache:' -and $line -match '(?i)token' -and $line -notmatch 'blacklist:\{jti\}') {
                $failures += "${f}:${n} 'token' en llave cache (solo blacklist:{jti} permitido)"
            }
        }
    }

    # B3: auth explicita en Controllers (1 de los 3 patrones validos segun 03-17).
    if ($f -match 'Controllers/.*\.cs$' -and -not $isTest) {
        $content = Get-Content -LiteralPath $f -Raw
        $hasAdmin = $content -match 'Authorize\(Policy\s*=\s*"AdminPolicy"\)'
        $hasBare = $content -match '\[Authorize\]'
        $hasAnon = $content -match '\[AllowAnonymous\]'
        if (-not ($hasAdmin -or $hasBare -or $hasAnon)) {
            $failures += "${f}:1 sin auth explicita (AdminPolicy | [Authorize] | [AllowAnonymous])"
        }
        # A2: proxy estructural, solo aviso (GET-only 03-14 no valida).
        if ($content -notmatch 'ValidateAsync') {
            $warnings += "${f}:1 sin llamada a ValidateAsync (advisory; GET-only exento)"
        }
    }

    # B4: catch nuevo en Controllers (03-16 pendiente; deuda existente no cuenta).
    if ($f -match 'Controllers/.*\.cs$' -and -not $isTest) {
        foreach ($line in (Get-AddedLines $f)) {
            if ($line -match '\bcatch\b') {
                $failures += "${f}: catch nuevo en Controller (canonico 03-16: sin try/catch ad-hoc)"
                break
            }
        }
    }

    # A1: NOTE sin referencia (XX-YY).
    if ($f -match '\.cs$' -and -not $isTest) {
        $n = 0
        foreach ($line in (Get-Content -LiteralPath $f)) {
            $n++
            if ($line -match '\bNOTE\b' -and $line -notmatch 'NOTE\s*\([0-9]{2}-[0-9]{2}\)') {
                $warnings += "${f}:${n} NOTE sin duena (formato: NOTE (XX-YY) -> fase)"
            }
        }
    }

    # A3: llave cache agregada sin interpolacion $"" (CA1305).
    if ($f -match 'Services/.*\.cs$' -and -not $isTest) {
        foreach ($line in (Get-AddedLines $f)) {
            if ($line -match 'cache:' -and $line -notmatch '\$"') {
                $warnings += $f + ': llave cache sin interpolacion $"... (advisory, CA1305)'
                break
            }
        }
    }
}

foreach ($w in $warnings) { Write-Host "WARN $w" }
if ($failures.Count -gt 0) {
    foreach ($e in $failures) { Write-Host "FAIL $e" }
    Write-Host ("critic: FAIL ({0} bloqueantes, {1} avisos)" -f $failures.Count, $warnings.Count)
    exit 1
}

Write-Host ("critic: PASS ({0} files scanned; 401 identicos / S1541 / Stryker >=80% se verifican con tests, ver PR template)" -f $changed.Count)
exit 0
