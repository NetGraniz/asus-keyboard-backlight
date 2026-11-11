$ErrorActionPreference = "Stop"

$projectDirectory = $PSScriptRoot
$publishDirectory = Join-Path $projectDirectory "publish"
$installDirectory = Join-Path $env:LOCALAPPDATA "Programs\AsusKeyboardBacklight"
$executablePath = Join-Path $installDirectory "AsusKeyboardBacklight.exe"
$taskName = "ASUS Keyboard Backlight"
$currentUser = [Security.Principal.WindowsIdentity]::GetCurrent().Name
$prebuiltExecutable = Join-Path $projectDirectory "AsusKeyboardBacklight.exe"

if (Test-Path -LiteralPath $prebuiltExecutable) {
    $sourceExecutable = $prebuiltExecutable
}
else {
    dotnet publish (Join-Path $projectDirectory "AsusKeyboardBacklight.csproj") -c Release -o $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "The application build failed."
    }
    $sourceExecutable = Join-Path $publishDirectory "AsusKeyboardBacklight.exe"
}

Get-Process -Name "AsusKeyboardBacklight" -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
Copy-Item -LiteralPath $sourceExecutable -Destination $executablePath -Force

$action = New-ScheduledTaskAction -Execute $executablePath
$trigger = New-ScheduledTaskTrigger -AtLogOn -User $currentUser
$principal = New-ScheduledTaskPrincipal -UserId $currentUser -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Days 3650) -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)
Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Force | Out-Null
Start-ScheduledTask -TaskName $taskName

Write-Output "Installed: $executablePath"
Write-Output "Startup task: $taskName"
