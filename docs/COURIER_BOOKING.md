# Courier booking

Gemora currently uses **Gemora Courier Sandbox**, a simulated academic provider.
It does not contact a real courier, purchase labels, schedule pickups, or provide live tracking.
No courier API credentials are required.

After an administrator approves the shipping plan, select **Record sandbox booking**
on the shipment's **Courier Booking** tab. The provider generates `SIM-BOOK-` and
`SIM-TRK-` references, which are saved with the approved service and booking time.
Repeating the request returns the original references instead of creating another booking.
Existing booking records and audit history are preserved.

The **Operations** tab lets an administrator advance the academic delivery workflow.
These updates notify the buyer with an **Academic simulation** label. Booking a
shipment does not automatically mark it delivered; delivery is a separate recorded step.

A separate administrator-only `POST /api/Shipments/{id}/booking` endpoint can record
actual courier-issued details in a future live workflow. It validates the tracking
number, booking date, confirmation and optional HTTPS tracking link, and rejects
simulated references. The sandbox screen uses `POST /api/Shipments/{id}/book` instead.
