$ErrorActionPreference = "Stop"

$taskName = "ASUS Keyboard Backlight"
$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\AsusKeyboardBacklight"

Get-Process -Name "AsusKeyboardBacklight" -ErrorAction SilentlyContinue | Stop-Process -Force
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}
if (Test-Path -LiteralPath $installDirectory) {
    Remove-Item -LiteralPath $installDirectory -Recurse -Force
}

Write-Output "ASUS Keyboard Backlight was removed. User settings were kept."
