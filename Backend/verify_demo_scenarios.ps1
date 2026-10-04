# Component 3 Demo Scenarios Verification Script
# This script verifies all 6 demo scenarios (A-F) are properly seeded

Write-Host "=========================================="
Write-Host "Component 3 Demo Scenarios Verification"
Write-Host "=========================================="
Write-Host ""

# Login as seller
$sellerLogin = Invoke-RestMethod -Uri 'http://localhost:5198/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"seller@gemora.com","password":"seller123"}'
$sellerToken = $sellerLogin.token
$sellerHeaders = @{ 'Authorization' = "Bearer $sellerToken" }

# Login as admin
$adminLogin = Invoke-RestMethod -Uri 'http://localhost:5198/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"admin@gemora.com","password":"admin123"}'
$adminToken = $adminLogin.token
$adminHeaders = @{ 'Authorization' = "Bearer $adminToken" }

# Login as buyer
$buyerLogin = Invoke-RestMethod -Uri 'http://localhost:5198/api/auth/login' -Method Post -ContentType 'application/json' -Body '{"email":"buyer@gemora.com","password":"buyer123"}'
$buyerToken = $buyerLogin.token
$buyerHeaders = @{ 'Authorization' = "Bearer $buyerToken" }

Write-Host "=== SCENARIO A: Fresh Paid Order ==="
$eligibleOrders = Invoke-RestMethod -Uri 'http://localhost:5198/api/orders/my-shipment-eligible' -Headers $sellerHeaders
$scenarioA = $eligibleOrders | Where-Object { $_.totalAmount -eq 850000 -and $_.currency -eq 'LKR' }
if ($scenarioA) {
    Write-Host "✓ Scenario A found: $($scenarioA.gemTitle) - LKR$($scenarioA.totalAmount)"
    Write-Host "  Order ID: $($scenarioA.id)"
    Write-Host "  Status: $($scenarioA.status)"
    Write-Host "  Address: $($scenarioA.shippingAddress), $($scenarioA.shippingRegion)"
} else {
    Write-Host "✗ Scenario A NOT FOUND"
}

Write-Host ""
Write-Host "=== SCENARIO B: Plan Waiting for Admin ==="
$allShipments = Invoke-RestMethod -Uri 'http://localhost:5198/api/shipments/my' -Headers $sellerHeaders
$scenarioB = $allShipments | Where-Object { $_.status -eq 'Planning' }
if ($scenarioB) {
    Write-Host "✓ Scenario B found: Shipment in Planning status"
    Write-Host "  Shipment ID: $($scenarioB.id)"
    Write-Host "  Order ID: $($scenarioB.orderId)"
    Write-Host "  Declared Value: LKR$($scenarioB.declaredValue)"
} else {
    Write-Host "✗ Scenario B NOT FOUND"
}

Write-Host ""
Write-Host "=== SCENARIO C: Approved / Ready for Booking ==="
$scenarioC = $allShipments | Where-Object { $_.status -eq 'ReadyForBooking' }
if ($scenarioC) {
    Write-Host "✓ Scenario C found: Shipment ready for booking"
    Write-Host "  Shipment ID: $($scenarioC.id)"
    Write-Host "  Order ID: $($scenarioC.orderId)"
    Write-Host "  Declared Value: LKR$($scenarioC.declaredValue)"
} else {
    Write-Host "✗ Scenario C NOT FOUND"
}

Write-Host ""
Write-Host "=== SCENARIO D: Booked + Insured + In Transit ==="
$scenarioD = $allShipments | Where-Object { $_.status -eq 'InTransit' }
if ($scenarioD) {
    Write-Host "✓ Scenario D found: Shipment in transit"
    Write-Host "  Shipment ID: $($scenarioD.id)"
    Write-Host "  Tracking: $($scenarioD.trackingNumber)"
    Write-Host "  Courier: $($scenarioD.courierName)"
    Write-Host "  Declared Value: LKR$($scenarioD.declaredValue)"

    # Check tracking events
    try {
        $trackingD = Invoke-RestMethod -Uri "http://localhost:5198/api/shipments/$($scenarioD.id)/tracking" -Headers $sellerHeaders
        Write-Host "  Tracking Events: $($trackingD.Length) events recorded"
    } catch {
        Write-Host "  Warning: Could not fetch tracking events"
    }

    # Check insurance
    try {
        $insuranceD = Invoke-RestMethod -Uri "http://localhost:5198/api/shipments/$($scenarioD.id)/insurance" -Headers $sellerHeaders
        if ($insuranceD) {
            Write-Host "  Insurance: Policy $($insuranceD.policyNumber) - $($insuranceD.providerName)"
        }
    } catch {
        Write-Host "  Warning: Could not fetch insurance record"
    }
} else {
    Write-Host "✗ Scenario D NOT FOUND"
}

Write-Host ""
Write-Host "=== SCENARIO E: Delivery Exception ==="
$scenarioE = $allShipments | Where-Object { $_.status -eq 'Exception' }
if ($scenarioE) {
    Write-Host "✓ Scenario E found: Shipment with exception"
    Write-Host "  Shipment ID: $($scenarioE.id)"
    Write-Host "  Tracking: $($scenarioE.trackingNumber)"
    Write-Host "  Declared Value: LKR$($scenarioE.declaredValue)"
} else {
    Write-Host "✗ Scenario E NOT FOUND"
}

Write-Host ""
Write-Host "=== SCENARIO F: Completed Delivery ==="
$scenarioF = $allShipments | Where-Object { $_.status -eq 'Delivered' }
if ($scenarioF) {
    Write-Host "✓ Scenario F found: Delivered shipment"
    Write-Host "  Shipment ID: $($scenarioF.id)"
    Write-Host "  Tracking: $($scenarioF.trackingNumber)"
    Write-Host "  Declared Value: LKR$($scenarioF.declaredValue)"
} else {
    Write-Host "✗ Scenario F NOT FOUND"
}

Write-Host ""
Write-Host "=========================================="
Write-Host "Summary"
Write-Host "=========================================="
Write-Host "Total eligible orders for seller: $($eligibleOrders.Length)"
Write-Host "Total shipments for seller: $($allShipments.Length)"
Write-Host ""
Write-Host "Scenarios Found:"
Write-Host "  Scenario A (Fresh Order): $(if($scenarioA){'YES'}else{'NO'})"
Write-Host "  Scenario B (Planning): $(if($scenarioB){'YES'}else{'NO'})"
Write-Host "  Scenario C (ReadyForBooking): $(if($scenarioC){'YES'}else{'NO'})"
Write-Host "  Scenario D (InTransit): $(if($scenarioD){'YES'}else{'NO'})"
Write-Host "  Scenario E (Exception): $(if($scenarioE){'YES'}else{'NO'})"
Write-Host "  Scenario F (Delivered): $(if($scenarioF){'YES'}else{'NO'})"
