# Stable SDK method matrix

All34 workspace and7 public operations are required and implemented. Request and response types are immutable; UTC timestamps preserve the wire RFC3339 string.

| Operation | Client method | Response | HTTP |
| --- | --- | --- | --- |
| createResource | WorkspaceClient.CreateResourceAsync | Resource | POST `/v1/workspaces/{workspaceId}/resources` |
| listResources | WorkspaceClient.ListResourcesAsync | ResourcePage | GET `/v1/workspaces/{workspaceId}/resources` |
| setHours | WorkspaceClient.ReplaceHoursAsync | Hours | PUT `/v1/workspaces/{workspaceId}/resources/{resourceId}/hours` |
| getHours | WorkspaceClient.GetHoursAsync | Hours | GET `/v1/workspaces/{workspaceId}/resources/{resourceId}/hours` |
| createOffering | WorkspaceClient.CreateOfferingAsync | Offering | POST `/v1/workspaces/{workspaceId}/offerings` |
| listOfferings | WorkspaceClient.ListOfferingsAsync | OfferingPage | GET `/v1/workspaces/{workspaceId}/offerings` |
| availability | WorkspaceClient.AvailabilityAsync | Availability | GET `/v1/workspaces/{workspaceId}/availability` |
| book | WorkspaceClient.BookAsync | Booking | POST `/v1/workspaces/{workspaceId}/bookings` |
| listBookings | WorkspaceClient.ListBookingsAsync | BookingPage | GET `/v1/workspaces/{workspaceId}/bookings` |
| cancel | WorkspaceClient.CancelAsync | Booking | POST `/v1/workspaces/{workspaceId}/bookings/{bookingId}/cancel` |
| events | WorkspaceClient.EventsAsync | Events | GET `/v1/workspaces/{workspaceId}/events` |
| getResource | WorkspaceClient.GetResourceAsync | Resource | GET `/v1/workspaces/{workspaceId}/resources/{resourceId}` |
| updateResource | WorkspaceClient.ReplaceResourceAsync | Resource | PUT `/v1/workspaces/{workspaceId}/resources/{resourceId}` |
| getOffering | WorkspaceClient.GetOfferingAsync | Offering | GET `/v1/workspaces/{workspaceId}/offerings/{offeringId}` |
| updateOffering | WorkspaceClient.ReplaceOfferingAsync | Offering | PUT `/v1/workspaces/{workspaceId}/offerings/{offeringId}` |
| getBooking | WorkspaceClient.GetBookingAsync | Booking | GET `/v1/workspaces/{workspaceId}/bookings/{bookingId}` |
| createWebhook | WorkspaceClient.CreateWebhookAsync | WebhookCreated | POST `/v1/workspaces/{workspaceId}/webhooks` |
| listWebhooks | WorkspaceClient.ListWebhooksAsync | WebhookPage | GET `/v1/workspaces/{workspaceId}/webhooks` |
| disableWebhook | WorkspaceClient.DisableWebhookAsync | Webhook | POST `/v1/workspaces/{workspaceId}/webhooks/{subscriptionId}/disable` |
| listWebhookDeliveries | WorkspaceClient.ListWebhookDeliveriesAsync | WebhookDeliveryPage | GET `/v1/workspaces/{workspaceId}/webhooks/{subscriptionId}/deliveries` |
| getUsage | WorkspaceClient.GetUsageAsync | Usage | GET `/v1/workspaces/{workspaceId}/usage` |
| rescheduleBooking | WorkspaceClient.RescheduleBookingAsync | Booking | POST `/v1/workspaces/{workspaceId}/bookings/{bookingId}/reschedule` |
| archiveResource | WorkspaceClient.ArchiveResourceAsync | Resource | POST `/v1/workspaces/{workspaceId}/resources/{resourceId}/archive` |
| archiveOffering | WorkspaceClient.ArchiveOfferingAsync | Offering | POST `/v1/workspaces/{workspaceId}/offerings/{offeringId}/archive` |
| pruneHistory | WorkspaceClient.PruneHistoryAsync | HistoryPruneResult | POST `/v1/workspaces/{workspaceId}/maintenance/history` |
| createCustomer | WorkspaceClient.CreateCustomerAsync | Customer | POST `/v1/workspaces/{workspaceId}/customers` |
| listCustomers | WorkspaceClient.ListCustomersAsync | CustomerPage | GET `/v1/workspaces/{workspaceId}/customers` |
| getCustomer | WorkspaceClient.GetCustomerAsync | Customer | GET `/v1/workspaces/{workspaceId}/customers/{customerId}` |
| replaceCustomer | WorkspaceClient.ReplaceCustomerAsync | Customer | PUT `/v1/workspaces/{workspaceId}/customers/{customerId}` |
| archiveCustomer | WorkspaceClient.ArchiveCustomerAsync | Customer | POST `/v1/workspaces/{workspaceId}/customers/{customerId}/archive` |
| eraseCustomer | WorkspaceClient.EraseCustomerAsync | CustomerErased | POST `/v1/workspaces/{workspaceId}/customers/{customerId}/erase` |
| pruneExpiredReplays | WorkspaceClient.PruneExpiredReplaysAsync | MaintenanceResult | POST `/v1/workspaces/{workspaceId}/maintenance/expired-replays` |
| getPublicPage | PublicBookingClient.GetPageAsync | PublicPage | GET `/v1/public/pages/{pageId}` |
| listPublicOfferings | PublicBookingClient.ListOfferingsAsync | PublicOfferingPage | GET `/v1/public/pages/{pageId}/offerings` |
| publicAvailability | PublicBookingClient.AvailabilityAsync | Availability | GET `/v1/public/pages/{pageId}/availability` |
| bookPublic | PublicBookingClient.BookAsync | PublicBooking | POST `/v1/public/pages/{pageId}/bookings` |
| getPublicBooking | PublicBookingClient.GetBookingAsync | PublicBooking | GET `/v1/public/pages/{pageId}/bookings/{bookingId}` |
| cancelPublicBooking | PublicBookingClient.CancelAsync | PublicBooking | POST `/v1/public/pages/{pageId}/bookings/{bookingId}/cancel` |
| reschedulePublicBooking | PublicBookingClient.RescheduleAsync | PublicBooking | POST `/v1/public/pages/{pageId}/bookings/{bookingId}/reschedule` |
| getBookingPage | WorkspaceClient.GetBookingPageAsync | BookingPageSettings | GET `/v1/workspaces/{workspaceId}/booking-page` |
| replaceBookingPage | WorkspaceClient.ReplaceBookingPageAsync | BookingPageSettings | PUT `/v1/workspaces/{workspaceId}/booking-page` |
