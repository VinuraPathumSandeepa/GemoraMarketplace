
$ErrorActionPreference = "Stop"

$testId = "C2-02"
$baseUrl = "http://localhost:5198"

# Generate a unique keyword that cannot match test data
$keyword = "NO_MATCH_GEM_" + [guid]::NewGuid().ToString("N")
$encoded = [uri]::EscapeDataString($keyword)

$url = "$baseUrl/api/marketplace/gems?search=$encoded&page=1&pageSize=12&sort=newest"

$resultsDir = Join-Path $PSScriptRoot "..\..\TestResults\Component2"
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null

Write-Output "========================================"
Write-Output "GEMORA MARKETPLACE AUTOMATED API TEST"
Write-Output "Test ID: $testId"
Write-Output "Test: Marketplace Filtering - No Match"
Write-Output "========================================"

try {
    # Execute HTTP request against real API
    $response = Invoke-WebRequest `
        -Uri $url `
        -Method Get `
        -TimeoutSec 30 `
        -UseBasicParsing

    $data = $response.Content | ConvertFrom-Json

    # Save the real API response
    $response.Content | Set-Content `
        -Path (Join-Path $resultsDir "C2-02-response.json") `
        -Encoding UTF8

    Write-Output "Search keyword: $keyword"
    Write-Output "HTTP status: $($response.StatusCode)"
    Write-Output "Response: $($response.Content)"

    # Assert expected HTTP status
    if ([int]$response.StatusCode -ne 200) {
        throw "Expected HTTP 200."
    }

    # Assert empty matching results
    if (@($data.items).Count -ne 0) {
        throw "Expected zero matching gemstones."
    }

    # Assert pagination metadata
    if ([int]$data.page -ne 1) {
        throw "Expected page 1."
    }

    if ([int]$data.pageSize -ne 12) {
        throw "Expected page size 12."
    }

    if ([int]$data.totalItems -ne 0) {
        throw "Expected totalItems = 0."
    }

    if ([int]$data.totalPages -ne 0) {
        throw "Expected totalPages = 0."
    }

    Write-Output ""
    Write-Output "PASS - C2-02 Marketplace Filtering"
    Write-Output "All assertions passed."

    exit 0
}
catch {
    Write-Output ""
    Write-Output "FAIL - C2-02 Marketplace Filtering"
    Write-Output "Reason: $($_.Exception.Message)"
    exit 1
}
