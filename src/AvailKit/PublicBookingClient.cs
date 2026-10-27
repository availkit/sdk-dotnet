namespace AvailKit;
public sealed class PublicBookingClient : ClientCore
{
 public PublicBookingClient(string pageId, ClientOptions? options = null) : base(pageId, options ?? new()) {}
 public Task<PublicPage> GetPageAsync(CancellationToken cancellationToken = default) => CallAsync<PublicPage>("getPublicPage", new string[]{}, null, new Dictionary<string, string>(), null, new Dictionary<string, string>(), cancellationToken);
 public Task<PublicOfferingPage> ListOfferingsAsync(string? after, long limit, CancellationToken cancellationToken = default) => CallAsync<PublicOfferingPage>("listPublicOfferings", new string[]{}, null, Query("after", after, "limit", limit), null, new Dictionary<string, string>(), cancellationToken);
 public Task<Availability> AvailabilityAsync(string? offeringId, string? from, string? to, CancellationToken cancellationToken = default) => CallAsync<Availability>("publicAvailability", new string[]{}, null, Query("offeringId", offeringId, "from", from, "to", to), null, new Dictionary<string, string>(), cancellationToken);
 public RequestIntent PrepareBook(PublicBookingInput body, string idempotencyKey, string bookingToken, string contactToken) => Prepare("bookPublic", new string[]{}, body, idempotencyKey, Access(bookingToken, contactToken));
 public Task<PublicBooking> BookAsync(PublicBookingInput body, string idempotencyKey, string bookingToken, string contactToken, CancellationToken cancellationToken = default) => ExecuteAsync<PublicBooking>(PrepareBook(body, idempotencyKey, bookingToken, contactToken), cancellationToken);
 public Task<PublicBooking> GetBookingAsync(string bookingId, string bookingToken, CancellationToken cancellationToken = default) => CallAsync<PublicBooking>("getPublicBooking", new string[]{bookingId}, null, new Dictionary<string, string>(), null, Access(bookingToken, null), cancellationToken);
 public RequestIntent PrepareCancel(string bookingId, string idempotencyKey, string bookingToken) => Prepare("cancelPublicBooking", new string[]{bookingId}, new Empty(), idempotencyKey, Access(bookingToken, null));
 public Task<PublicBooking> CancelAsync(string bookingId, string idempotencyKey, string bookingToken, CancellationToken cancellationToken = default) => ExecuteAsync<PublicBooking>(PrepareCancel(bookingId, idempotencyKey, bookingToken), cancellationToken);
 public RequestIntent PrepareReschedule(string bookingId, RescheduleInput body, string idempotencyKey, string bookingToken) => Prepare("reschedulePublicBooking", new string[]{bookingId}, body, idempotencyKey, Access(bookingToken, null));
 public Task<PublicBooking> RescheduleAsync(string bookingId, RescheduleInput body, string idempotencyKey, string bookingToken, CancellationToken cancellationToken = default) => ExecuteAsync<PublicBooking>(PrepareReschedule(bookingId, body, idempotencyKey, bookingToken), cancellationToken);
 public Task<PublicBooking> ExecuteAsync(RequestIntent intent, CancellationToken cancellationToken = default) => ExecuteAsync<PublicBooking>(intent, cancellationToken);
}
