param([string]$Path = (Join-Path (Split-Path $PSScriptRoot -Parent) '.env.local'))
# Import server-only local configuration. Existing environment values take
# precedence; values are never executed or logged.
if (Test-Path -LiteralPath $Path) {
    foreach ($line in Get-Content -LiteralPath $Path) {
        if ($line -match '^\s*(#|$)') { continue }
        if ($line -notmatch '^\s*(Routing__[A-Za-z][A-Za-z0-9_]*|SMTP_(?:HOST|PORT|USERNAME|PASSWORD|FROM_EMAIL|FROM_NAME))\s*=(.*)$') { continue }
        $settingName = $Matches[1]
        $settingValue = $Matches[2].Trim()
        if ($settingValue.Length -ge 2 -and
            (($settingValue.StartsWith('"') -and $settingValue.EndsWith('"')) -or
             ($settingValue.StartsWith("'") -and $settingValue.EndsWith("'")))) {
            $settingValue = $settingValue.Substring(1, $settingValue.Length - 2)
        }
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($settingName, 'Process'))) {
            [Environment]::SetEnvironmentVariable($settingName, $settingValue, 'Process')
        }
    }
}
