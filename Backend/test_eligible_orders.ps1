$loginResponse = Invoke-RestMethod -Uri 'http://localhost:5198/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"seller@gemora.com","password":"seller123"}'
$token = $loginResponse.token
Write-Host "Token obtained successfully"

$headers = @{
    'Authorization' = "Bearer $token"
}

try {
    $orders = Invoke-RestMethod -Uri 'http://localhost:5198/api/orders/my-shipment-eligible' -Headers $headers
    Write-Host "Found $($orders.Length) eligible orders for seller"
    $orders | ConvertTo-Json -Depth 5
} catch {
    Write-Host "Error fetching eligible orders: $_"
}
