
$ErrorActionPreference = "Stop"

$url = "http://localhost:5198/api/marketplace/gems?search=%25&page=1&pageSize=50"

Write-Host "======================================="
Write-Host "C2-BUG-001 - Literal Wildcard Search"
Write-Host "======================================="

try {
    $response = Invoke-RestMethod -Method Get -Uri $url

    $items = @(
        $response.items | Where-Object { $null -ne $_ }
    )

    $unexpected = @(
        $items | Where-Object {
            $_.title -notlike "*%*" -and
            $_.gemType -notlike "*%*" -and
            $_.description -notlike "*%*"
        }
    )

    Write-Host "Items returned: $($items.Count)"
    Write-Host "Unexpected matches: $($unexpected.Count)"

    foreach ($gem in $unexpected) {
        Write-Host "Unexpected gem: $($gem.id) - $($gem.title)"
    }

    if ($unexpected.Count -gt 0) {
        Write-Host "FAIL - Wildcard treated as search pattern"
        exit 1
    }

    Write-Host "PASS - Only literal matches returned"
    exit 0
}
catch {
    Write-Host "TEST ERROR: $($_.Exception.Message)"
    exit 1
}
