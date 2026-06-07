$ErrorActionPreference = "Stop"

Write-Host "1. Creating a booking..."
$bookingJson = @{
    guest = @{ fullName = "Jane Doe"; email = "jane@x.com"; phoneNumber = "+100"; nationalId = "A1" }
    style = 1
    checkIn = "2026-07-01"
    checkOut = "2026-07-04"
    preferredFloor = 2
    advancePayment = 120
} | ConvertTo-Json -Depth 5

$response = Invoke-RestMethod -Uri "http://localhost:5001/api/bookings" -Method Post -Body $bookingJson -ContentType "application/json"
$bookingId = $response.bookingId
$guestId = $response.guestId
$roomId = $response.roomId
Write-Host "Created Booking ID: $bookingId. Assigned Room: $($response.roomNumber)" -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host "2. Checking in..."
$checkInUrl = "http://localhost:5001/api/bookings/$bookingId/checkin"
Invoke-RestMethod -Uri $checkInUrl -Method Post
Write-Host "Checked in successfully." -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host "Fetching menu items..."
$menu = Invoke-RestMethod -Uri "http://localhost:5003/api/menu" -Method Get
$menuItemId = $menu[0].id

Write-Host "3. Ordering room service..."
$orderJson = @{
    bookingId = $bookingId
    guestId = $guestId
    roomId = $roomId
    items = @( @{ menuItemId = $menuItemId; quantity = 2 } )
} | ConvertTo-Json -Depth 5
$orderResponse = Invoke-RestMethod -Uri "http://localhost:5003/api/orders" -Method Post -Body $orderJson -ContentType "application/json"
$orderId = $orderResponse.orderId
Write-Host "Order Placed. ID: $orderId" -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host "Delivering room service..."
Invoke-RestMethod -Uri "http://localhost:5003/api/orders/$orderId/deliver" -Method Post
Write-Host "Order Delivered." -ForegroundColor Green

Start-Sleep -Seconds 2

Write-Host "4. Checking out..."
$checkoutResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/bookings/$bookingId/checkout" -Method Post
Write-Host "Checked out successfully. Final Bill Total: $($checkoutResponse.totalAmount)" -ForegroundColor Green

Write-Host "All end-to-end tests completed successfully!" -ForegroundColor Cyan
