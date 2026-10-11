param([switch]$SkipBuild)
$ErrorActionPreference = 'Stop'
$env:PUB_CACHE = 'D:\GemoraTools\pub-cache'
$env:TEMP = 'D:\GemoraTools\temp'
$env:TMP = $env:TEMP
$env:FLUTTER_TOOL_ARGS = '--old_gen_heap_size=384'
Push-Location $PSScriptRoot
try {
    if (!$SkipBuild) {
        & D:\GemoraTools\flutter\bin\flutter.bat --no-version-check build web --debug --dart-define=API_SERVER_URL=http://localhost:5174
        if ($LASTEXITCODE -ne 0) { throw 'The browser build failed.' }
    }
    if (!(Test-Path 'build\web\index.html')) { throw 'Build the browser app first.' }
    Write-Host 'Open http://localhost:5174. Keep this terminal open. The backend must run on port 5198.'
    node .\serve-browser.cjs
} finally { Pop-Location }
