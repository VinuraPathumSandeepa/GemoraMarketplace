param(
    [switch]$CheckOnly,
    [ValidateRange(1, 60)][int]$BootTimeoutMinutes = 15
)

$ErrorActionPreference = 'Stop'
$toolsRoot = 'D:\GemoraTools'
$env:ANDROID_HOME = "$toolsRoot\android-sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:ANDROID_USER_HOME = "$toolsRoot\android-user"
$env:ANDROID_AVD_HOME = "$toolsRoot\avd"
$env:PUB_CACHE = "$toolsRoot\pub-cache"
$env:GRADLE_USER_HOME = "$toolsRoot\gradle"
$env:TEMP = "$toolsRoot\temp"
$env:TMP = $env:TEMP
$env:JAVA_HOME = 'D:\AS\jbr'
$env:FLUTTER_TOOL_ARGS = '--old_gen_heap_size=384'
$env:Path = "$toolsRoot\flutter\bin;$env:ANDROID_HOME\platform-tools;$env:JAVA_HOME\bin;$env:Path"

Push-Location $PSScriptRoot
try {
    if (!(Test-Path "$toolsRoot\flutter\bin\flutter.bat")) {
        throw "Flutter is missing from $toolsRoot\flutter."
    }
    if ($CheckOnly) {
        flutter --no-version-check doctor -v
        exit $LASTEXITCODE
    }
    $adb = "$env:ANDROID_HOME\platform-tools\adb.exe"
    if (!(Test-Path $adb)) { throw 'Android SDK installation is incomplete.' }
    function Find-GemoraDevice {
        foreach ($entry in ((& $adb devices) | Select-String '^(emulator-\d+)\s+device$')) {
            $serial = $entry.Matches[0].Groups[1].Value
            $avdName = & $adb -s $serial emu avd name
            if (($avdName | ForEach-Object { $_.Trim() }) -contains 'Gemora_Android') { return $serial }
        }
    }

    $device = Find-GemoraDevice
    if ($device) {
        Write-Host 'Closing the Gemora emulator during compilation to free memory. Its data is preserved.'
        $stopper = Start-Process $adb -ArgumentList "-s $device emu kill" -WindowStyle Hidden -PassThru
        if (!$stopper.WaitForExit(15000)) {
            $stopper.Kill()
            throw 'Close the Gemora emulator window, then run this script again.'
        }
        $shutdownDeadline = (Get-Date).AddSeconds(30)
        while (Find-GemoraDevice) {
            if ((Get-Date) -gt $shutdownDeadline) { throw 'The Gemora emulator is still closing. Retry after it closes.' }
            Start-Sleep -Seconds 2
        }
    }

    Write-Host 'Building Gemora before starting the emulator...'
    flutter --no-version-check pub get
    if ($LASTEXITCODE -ne 0) { throw 'Could not resolve Flutter dependencies.' }
    flutter --no-version-check build apk --debug --target-platform android-x64 --no-pub
    if ($LASTEXITCODE -ne 0) { throw 'The Android build failed. See the build error above.' }
    & "$PSScriptRoot\android\gradlew.bat" -p "$PSScriptRoot\android" --stop
    if ($LASTEXITCODE -ne 0) { throw 'Could not stop Gradle to release memory before emulator startup.' }

    $avds = & "$env:ANDROID_HOME\emulator\emulator.exe" -list-avds
    if ($avds -notcontains 'Gemora_Android') { throw 'Create the Gemora_Android emulator before running this script.' }
    Write-Host "Starting the virtual phone; allowing up to $BootTimeoutMinutes minutes for boot..."
    $emulatorLog = "$toolsRoot\emulator-launch.log"
    $emulatorErrorLog = "$toolsRoot\emulator-launch-error.log"
    $emulator = Start-Process "$env:ANDROID_HOME\emulator\emulator.exe" -ArgumentList '-avd Gemora_Android -memory 1536 -cores 2 -no-snapshot -no-boot-anim' -WindowStyle Hidden -RedirectStandardOutput $emulatorLog -RedirectStandardError $emulatorErrorLog -PassThru
    $bootDeadline = (Get-Date).AddMinutes($BootTimeoutMinutes)
    $nextNotice = (Get-Date).AddSeconds(30)
    do {
        if ((Get-Date) -gt $bootDeadline) {
            throw "The virtual phone has not finished booting after $BootTimeoutMinutes minutes. Close unused apps, check the emulator window, and retry. Emulator data has been preserved."
        }
        $device = Find-GemoraDevice
        $booted = if ($device) { & $adb -s $device shell getprop sys.boot_completed } else { '' }
        if ((Get-Date) -ge $nextNotice) {
            Write-Host 'Waiting for Android to finish booting...'
            $nextNotice = (Get-Date).AddSeconds(30)
        }
        if (([string]$booted).Trim() -ne '1') { Start-Sleep -Seconds 3 }
    } until (([string]$booted).Trim() -eq '1')

    Write-Host 'Android is ready. Installing and opening Gemora...'
    flutter --no-version-check run -d $device --use-application-binary "$PSScriptRoot\build\app\outputs\flutter-apk\app-debug.apk"
    if ($LASTEXITCODE -ne 0) { throw 'Gemora could not be launched. See the Flutter error above.' }
} finally {
    Pop-Location
}

