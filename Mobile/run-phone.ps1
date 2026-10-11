param([string]$DeviceId)
$ErrorActionPreference = 'Stop'
$toolsRoot = 'D:\GemoraTools'
$env:ANDROID_HOME = "$toolsRoot\android-sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:ANDROID_USER_HOME = "$toolsRoot\android-user"
$env:PUB_CACHE = "$toolsRoot\pub-cache"
$env:GRADLE_USER_HOME = "$toolsRoot\gradle"
$env:TEMP = "$toolsRoot\temp"
$env:TMP = $env:TEMP
$env:JAVA_HOME = 'D:\AS\jbr'
$env:FLUTTER_TOOL_ARGS = '--old_gen_heap_size=384'
$adb = "$env:ANDROID_HOME\platform-tools\adb.exe"
$env:Path = "$toolsRoot\flutter\bin;$env:ANDROID_HOME\platform-tools;$env:JAVA_HOME\bin;$env:Path"
Push-Location $PSScriptRoot
try {
    $devices = @((& $adb devices) | Select-String '^(\S+)\s+device$' | ForEach-Object { $_.Matches[0].Groups[1].Value } | Where-Object { $_ -notlike 'emulator-*' })
    if (!$DeviceId) {
        if ($devices.Count -ne 1) { throw 'Connect one Android phone, enable USB debugging, unlock it, and accept Allow USB debugging. Use -DeviceId if multiple phones are connected.' }
        $DeviceId = $devices[0]
    }
    if ($devices -notcontains $DeviceId) { throw 'The selected phone is not connected or authorized for USB debugging.' }
    & $adb -s $DeviceId reverse tcp:5198 tcp:5198
    if ($LASTEXITCODE -ne 0) { throw 'Could not connect the phone to the backend over USB.' }
    flutter --no-version-check run -d $DeviceId --dart-define=API_SERVER_URL=http://127.0.0.1:5198
    if ($LASTEXITCODE -ne 0) { throw 'Could not run Gemora on the phone. See the error above.' }
} finally { Pop-Location }
