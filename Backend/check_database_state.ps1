$loginResponse = Invoke-RestMethod -Uri 'http://localhost:5198/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"seller@gemora.com","password":"seller123"}'
$token = $loginResponse.token

$headers = @{
    'Authorization' = "Bearer $token"
}

Write-Host "=== CHECKING GEM LISTINGS ==="
$listings = Invoke-RestMethod -Uri 'http://localhost:5198/api/gems/my-listings' -Headers $headers
Write-Host "Found $($listings.Length) gem listings for seller:"
$listings | Select-Object id, title, price, currency, caratWeight, color, gemType | Format-Table

Write-Host "`n=== CHECKING SHIPMENTS ==="
$shipments = Invoke-RestMethod -Uri 'http://localhost:5198/api/shipments/my' -Headers $headers
Write-Host "Found $($shipments.Length) shipments for seller:"
$shipments | Select-Object id, orderId, status, trackingNumber, courierName, declaredValue, currency | Format-Table
