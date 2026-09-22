param([string]$Path = (Join-Path (Split-Path $PSScriptRoot -Parent) '.env.local'))
# Dot-source this file. Only routing settings are imported into this process;
# existing environment values take precedence. Values are never executed/logged.
if (Test-Path -LiteralPath $Path) {
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^\s*(Routing__[A-Za-z][A-Za-z0-9_]*)\s*=(.*)$') { continue }
        $routingName = $Matches[1]
        $routingValue = $Matches[2].Trim()
        if ($routingValue.Length -ge 2 -and
            (($routingValue.StartsWith('"') -and $routingValue.EndsWith('"')) -or
             ($routingValue.StartsWith("'") -and $routingValue.EndsWith("'")))) {
            $routingValue = $routingValue.Substring(1, $routingValue.Length - 2)
        }
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($routingName, 'Process'))) {
            [Environment]::SetEnvironmentVariable($routingName, $routingValue, 'Process')
        }
    }
}
