using System.Text.Json.Serialization;
namespace AvailKit;
// Wire field names are camelCase; values are intentionally excluded from diagnostic strings.
public sealed class Availability
{
    [JsonPropertyName("slots")] public IReadOnlyList<Slot> Slots { get; }
    [JsonConstructor] public Availability(IReadOnlyList<Slot> slots)
    {
        Slots = Array.AsReadOnly((slots ?? throw AvailKitException.Input("invalid_input")).ToArray());
    }
    public override string ToString() => "AvailKit.Availability [redacted]";
}
public sealed class Booking
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("offeringId")] public string OfferingId { get; }
    [JsonPropertyName("resourceId")] public string ResourceId { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("end")] public string End { get; }
    [JsonPropertyName("status")] public string Status { get; }
    [JsonPropertyName("customerId")] public string? CustomerId { get; }
    [JsonConstructor] public Booking(string id, string offeringId, string resourceId, string start, string end, string status, string? customerId = null)
    {
        Id = id;
        OfferingId = offeringId;
        ResourceId = resourceId;
        Start = start;
        End = end;
        Status = status;
        CustomerId = customerId;
    }
    public override string ToString() => "AvailKit.Booking [redacted]";
}
public sealed class BookingInput
{
    [JsonPropertyName("offeringId")] public string OfferingId { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("customerId")] public string? CustomerId { get; }
    [JsonConstructor] public BookingInput(string offeringId, string start, string? customerId = null)
    {
        OfferingId = offeringId;
        Start = start;
        CustomerId = customerId;
    }
    public override string ToString() => "AvailKit.BookingInput [redacted]";
}
public sealed class BookingPage
{
    [JsonPropertyName("items")] public IReadOnlyList<Booking> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public BookingPage(IReadOnlyList<Booking> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.BookingPage [redacted]";
}
public sealed class BookingPageInput
{
    [JsonPropertyName("title")] public string Title { get; }
    [JsonPropertyName("enabled")] public bool Enabled { get; }
    [JsonConstructor] public BookingPageInput(string title, bool enabled)
    {
        Title = title;
        Enabled = enabled;
    }
    public override string ToString() => "AvailKit.BookingPageInput [redacted]";
}
public sealed class BookingPageSettings
{
    [JsonPropertyName("pageId")] public string? PageId { get; }
    [JsonPropertyName("title")] public string Title { get; }
    [JsonPropertyName("enabled")] public bool Enabled { get; }
    [JsonPropertyName("needsRepublish")] public bool NeedsRepublish { get; }
    [JsonConstructor] public BookingPageSettings(string? pageId, string title, bool enabled, bool needsRepublish)
    {
        PageId = pageId;
        Title = title;
        Enabled = enabled;
        NeedsRepublish = needsRepublish;
    }
    public override string ToString() => "AvailKit.BookingPageSettings [redacted]";
}
public sealed class Customer
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("displayName")] public string DisplayName { get; }
    [JsonPropertyName("email")] public string? Email { get; }
    [JsonPropertyName("phone")] public string? Phone { get; }
    [JsonPropertyName("archived")] public bool Archived { get; }
    [JsonPropertyName("erased")] public bool? Erased { get; }
    [JsonConstructor] public Customer(string id, string displayName, string? email, string? phone, bool archived, bool? erased = null)
    {
        Id = id;
        DisplayName = displayName;
        Email = email;
        Phone = phone;
        Archived = archived;
        Erased = erased;
    }
    public override string ToString() => "AvailKit.Customer [redacted]";
}
public sealed class CustomerErased
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("erased")] public bool Erased { get; }
    [JsonConstructor] public CustomerErased(string id, bool erased)
    {
        Id = id;
        Erased = erased;
    }
    public override string ToString() => "AvailKit.CustomerErased [redacted]";
}
public sealed class CustomerInput
{
    [JsonPropertyName("displayName")] public string DisplayName { get; }
    [JsonPropertyName("email")] public string? Email { get; }
    [JsonPropertyName("phone")] public string? Phone { get; }
    [JsonConstructor] public CustomerInput(string displayName, string? email = null, string? phone = null)
    {
        DisplayName = displayName;
        Email = email;
        Phone = phone;
    }
    public override string ToString() => "AvailKit.CustomerInput [redacted]";
}
public sealed class CustomerPage
{
    [JsonPropertyName("items")] public IReadOnlyList<Customer> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public CustomerPage(IReadOnlyList<Customer> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.CustomerPage [redacted]";
}
public sealed class Empty
{
    [JsonConstructor] public Empty()
    {
    }
    public override string ToString() => "AvailKit.Empty [redacted]";
}
public sealed class Events
{
    [JsonPropertyName("events")] public IReadOnlyList<BookingEvent> Items { get; }
    [JsonPropertyName("nextAfter")] public long NextAfter { get; }
    [JsonPropertyName("retentionFloor")] public long RetentionFloor { get; }
    [JsonPropertyName("highWater")] public long HighWater { get; }
    [JsonConstructor] public Events(IReadOnlyList<BookingEvent> items, long nextAfter, long retentionFloor, long highWater)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
        RetentionFloor = retentionFloor;
        HighWater = highWater;
    }
    public override string ToString() => "AvailKit.Events [redacted]";
}
public sealed class HistoryPruneInput
{
    [JsonPropertyName("kind")] public string Kind { get; }
    [JsonPropertyName("limit")] public long Limit { get; }
    [JsonConstructor] public HistoryPruneInput(string kind, long limit)
    {
        Kind = kind;
        Limit = limit;
    }
    public override string ToString() => "AvailKit.HistoryPruneInput [redacted]";
}
public sealed class HistoryPruneResult
{
    [JsonPropertyName("asOf")] public string AsOf { get; }
    [JsonPropertyName("cutoff")] public string Cutoff { get; }
    [JsonPropertyName("deleted")] public long Deleted { get; }
    [JsonPropertyName("hasMore")] public bool HasMore { get; }
    [JsonConstructor] public HistoryPruneResult(string asOf, string cutoff, long deleted, bool hasMore)
    {
        AsOf = asOf;
        Cutoff = cutoff;
        Deleted = deleted;
        HasMore = hasMore;
    }
    public override string ToString() => "AvailKit.HistoryPruneResult [redacted]";
}
public sealed class Hours
{
    [JsonPropertyName("windows")] public IReadOnlyList<HoursWindow> Windows { get; }
    [JsonConstructor] public Hours(IReadOnlyList<HoursWindow> windows)
    {
        Windows = Array.AsReadOnly((windows ?? throw AvailKitException.Input("invalid_input")).ToArray());
    }
    public override string ToString() => "AvailKit.Hours [redacted]";
}
public sealed class MaintenanceInput
{
    [JsonPropertyName("limit")] public long Limit { get; }
    [JsonConstructor] public MaintenanceInput(long limit)
    {
        Limit = limit;
    }
    public override string ToString() => "AvailKit.MaintenanceInput [redacted]";
}
public sealed class MaintenanceResult
{
    [JsonPropertyName("asOf")] public string AsOf { get; }
    [JsonPropertyName("deleted")] public long Deleted { get; }
    [JsonPropertyName("hasMore")] public bool HasMore { get; }
    [JsonConstructor] public MaintenanceResult(string asOf, long deleted, bool hasMore)
    {
        AsOf = asOf;
        Deleted = deleted;
        HasMore = hasMore;
    }
    public override string ToString() => "AvailKit.MaintenanceResult [redacted]";
}
public sealed class Offering
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("resourceId")] public string ResourceId { get; }
    [JsonPropertyName("durationMinutes")] public long DurationMinutes { get; }
    [JsonPropertyName("archived")] public bool Archived { get; }
    [JsonConstructor] public Offering(string id, string name, string resourceId, long durationMinutes, bool archived)
    {
        Id = id;
        Name = name;
        ResourceId = resourceId;
        DurationMinutes = durationMinutes;
        Archived = archived;
    }
    public override string ToString() => "AvailKit.Offering [redacted]";
}
public sealed class OfferingInput
{
    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("resourceId")] public string ResourceId { get; }
    [JsonPropertyName("durationMinutes")] public long DurationMinutes { get; }
    [JsonConstructor] public OfferingInput(string name, string resourceId, long durationMinutes)
    {
        Name = name;
        ResourceId = resourceId;
        DurationMinutes = durationMinutes;
    }
    public override string ToString() => "AvailKit.OfferingInput [redacted]";
}
public sealed class OfferingPage
{
    [JsonPropertyName("items")] public IReadOnlyList<Offering> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public OfferingPage(IReadOnlyList<Offering> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.OfferingPage [redacted]";
}
public sealed class PublicBooking
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("end")] public string End { get; }
    [JsonPropertyName("status")] public string Status { get; }
    [JsonPropertyName("offering")] public PublicOffering Offering { get; }
    [JsonPropertyName("page")] public PublicPage Page { get; }
    [JsonPropertyName("pageEnabled")] public bool PageEnabled { get; }
    [JsonConstructor] public PublicBooking(string id, string start, string end, string status, PublicOffering offering, PublicPage page, bool pageEnabled)
    {
        Id = id;
        Start = start;
        End = end;
        Status = status;
        Offering = offering;
        Page = page;
        PageEnabled = pageEnabled;
    }
    public override string ToString() => "AvailKit.PublicBooking [redacted]";
}
public sealed class PublicBookingInput
{
    [JsonPropertyName("offeringId")] public string OfferingId { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("contact")] public CustomerInput Contact { get; }
    [JsonConstructor] public PublicBookingInput(string offeringId, string start, CustomerInput contact)
    {
        OfferingId = offeringId;
        Start = start;
        Contact = contact;
    }
    public override string ToString() => "AvailKit.PublicBookingInput [redacted]";
}
public sealed class PublicOffering
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("durationMinutes")] public long DurationMinutes { get; }
    [JsonPropertyName("timeZone")] public string TimeZone { get; }
    [JsonConstructor] public PublicOffering(string id, string name, long durationMinutes, string timeZone)
    {
        Id = id;
        Name = name;
        DurationMinutes = durationMinutes;
        TimeZone = timeZone;
    }
    public override string ToString() => "AvailKit.PublicOffering [redacted]";
}
public sealed class PublicOfferingPage
{
    [JsonPropertyName("items")] public IReadOnlyList<PublicOffering> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public PublicOfferingPage(IReadOnlyList<PublicOffering> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.PublicOfferingPage [redacted]";
}
public sealed class PublicPage
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("title")] public string Title { get; }
    [JsonPropertyName("environment")] public string Environment { get; }
    [JsonPropertyName("emailNotifications")] public bool? EmailNotifications { get; }
    [JsonConstructor] public PublicPage(string id, string title, string environment, bool? emailNotifications = null)
    {
        Id = id;
        Title = title;
        Environment = environment;
        EmailNotifications = emailNotifications;
    }
    public override string ToString() => "AvailKit.PublicPage [redacted]";
}
public sealed class RescheduleInput
{
    [JsonPropertyName("expectedStart")] public string ExpectedStart { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonConstructor] public RescheduleInput(string expectedStart, string start)
    {
        ExpectedStart = expectedStart;
        Start = start;
    }
    public override string ToString() => "AvailKit.RescheduleInput [redacted]";
}
public sealed class Resource
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("timeZone")] public string TimeZone { get; }
    [JsonPropertyName("capacity")] public long Capacity { get; }
    [JsonPropertyName("archived")] public bool Archived { get; }
    [JsonConstructor] public Resource(string id, string name, string timeZone, long capacity, bool archived)
    {
        Id = id;
        Name = name;
        TimeZone = timeZone;
        Capacity = capacity;
        Archived = archived;
    }
    public override string ToString() => "AvailKit.Resource [redacted]";
}
public sealed class ResourceInput
{
    [JsonPropertyName("name")] public string Name { get; }
    [JsonPropertyName("timeZone")] public string TimeZone { get; }
    [JsonPropertyName("capacity")] public long Capacity { get; }
    [JsonConstructor] public ResourceInput(string name, string timeZone, long capacity)
    {
        Name = name;
        TimeZone = timeZone;
        Capacity = capacity;
    }
    public override string ToString() => "AvailKit.ResourceInput [redacted]";
}
public sealed class ResourcePage
{
    [JsonPropertyName("items")] public IReadOnlyList<Resource> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public ResourcePage(IReadOnlyList<Resource> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.ResourcePage [redacted]";
}
public sealed class Usage
{
    [JsonPropertyName("asOf")] public string AsOf { get; }
    [JsonPropertyName("period")] public UsagePeriod Period { get; }
    [JsonPropertyName("counters")] public UsageCounters Counters { get; }
    [JsonConstructor] public Usage(string asOf, UsagePeriod period, UsageCounters counters)
    {
        AsOf = asOf;
        Period = period;
        Counters = counters;
    }
    public override string ToString() => "AvailKit.Usage [redacted]";
}
public sealed class UsageCounter
{
    [JsonPropertyName("used")] public long Used { get; }
    [JsonPropertyName("limit")] public long Limit { get; }
    [JsonPropertyName("remaining")] public long Remaining { get; }
    [JsonConstructor] public UsageCounter(long used, long limit, long remaining)
    {
        Used = used;
        Limit = limit;
        Remaining = remaining;
    }
    public override string ToString() => "AvailKit.UsageCounter [redacted]";
}
public sealed class UsageCounters
{
    [JsonPropertyName("monthlyBookingCreates")] public UsageCounter MonthlyBookingCreates { get; }
    [JsonPropertyName("catalogObjects")] public UsageCounter CatalogObjects { get; }
    [JsonPropertyName("bookingRecords")] public UsageCounter BookingRecords { get; }
    [JsonPropertyName("committedEvents")] public UsageCounter CommittedEvents { get; }
    [JsonPropertyName("webhookSubscriptions")] public UsageCounter WebhookSubscriptions { get; }
    [JsonPropertyName("webhookDeliveries")] public UsageCounter WebhookDeliveries { get; }
    [JsonPropertyName("liveReplays")] public UsageCounter LiveReplays { get; }
    [JsonPropertyName("customerRecords")] public UsageCounter CustomerRecords { get; }
    [JsonConstructor] public UsageCounters(UsageCounter monthlyBookingCreates, UsageCounter catalogObjects, UsageCounter bookingRecords, UsageCounter committedEvents, UsageCounter webhookSubscriptions, UsageCounter webhookDeliveries, UsageCounter liveReplays, UsageCounter customerRecords)
    {
        MonthlyBookingCreates = monthlyBookingCreates;
        CatalogObjects = catalogObjects;
        BookingRecords = bookingRecords;
        CommittedEvents = committedEvents;
        WebhookSubscriptions = webhookSubscriptions;
        WebhookDeliveries = webhookDeliveries;
        LiveReplays = liveReplays;
        CustomerRecords = customerRecords;
    }
    public override string ToString() => "AvailKit.UsageCounters [redacted]";
}
public sealed class UsagePeriod
{
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("end")] public string End { get; }
    [JsonConstructor] public UsagePeriod(string start, string end)
    {
        Start = start;
        End = end;
    }
    public override string ToString() => "AvailKit.UsagePeriod [redacted]";
}
public sealed class Webhook
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("url")] public string Url { get; }
    [JsonPropertyName("enabled")] public bool Enabled { get; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; }
    [JsonConstructor] public Webhook(string id, string url, bool enabled, string createdAt)
    {
        Id = id;
        Url = url;
        Enabled = enabled;
        CreatedAt = createdAt;
    }
    public override string ToString() => "AvailKit.Webhook [redacted]";
}
public sealed class WebhookCreate
{
    [JsonPropertyName("url")] public string Url { get; }
    [JsonConstructor] public WebhookCreate(string url)
    {
        Url = url;
    }
    public override string ToString() => "AvailKit.WebhookCreate [redacted]";
}
public sealed class WebhookCreated
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("url")] public string Url { get; }
    [JsonPropertyName("enabled")] public bool Enabled { get; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; }
    [JsonPropertyName("signingSecret")] public string SigningSecret { get; }
    [JsonConstructor] public WebhookCreated(string id, string url, bool enabled, string createdAt, string signingSecret)
    {
        Id = id;
        Url = url;
        Enabled = enabled;
        CreatedAt = createdAt;
        SigningSecret = signingSecret;
    }
    public override string ToString() => "AvailKit.WebhookCreated [redacted]";
}
public sealed class WebhookDelivery
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("subscriptionId")] public string SubscriptionId { get; }
    [JsonPropertyName("eventId")] public string EventId { get; }
    [JsonPropertyName("state")] public string State { get; }
    [JsonPropertyName("createdAt")] public string CreatedAt { get; }
    [JsonPropertyName("attempts")] public long Attempts { get; }
    [JsonPropertyName("maxAttempts")] public long MaxAttempts { get; }
    [JsonPropertyName("nextAttemptAt")] public string? NextAttemptAt { get; }
    [JsonPropertyName("leaseExpiresAt")] public string? LeaseExpiresAt { get; }
    [JsonPropertyName("lastAttemptAt")] public string? LastAttemptAt { get; }
    [JsonPropertyName("lastOutcome")] public string? LastOutcome { get; }
    [JsonPropertyName("lastHttpStatus")] public long? LastHttpStatus { get; }
    [JsonConstructor] public WebhookDelivery(string id, string subscriptionId, string eventId, string state, string createdAt, long attempts, long maxAttempts, string? nextAttemptAt, string? leaseExpiresAt, string? lastAttemptAt, string? lastOutcome, long? lastHttpStatus)
    {
        Id = id;
        SubscriptionId = subscriptionId;
        EventId = eventId;
        State = state;
        CreatedAt = createdAt;
        Attempts = attempts;
        MaxAttempts = maxAttempts;
        NextAttemptAt = nextAttemptAt;
        LeaseExpiresAt = leaseExpiresAt;
        LastAttemptAt = lastAttemptAt;
        LastOutcome = lastOutcome;
        LastHttpStatus = lastHttpStatus;
    }
    public override string ToString() => "AvailKit.WebhookDelivery [redacted]";
}
public sealed class WebhookDeliveryPage
{
    [JsonPropertyName("items")] public IReadOnlyList<WebhookDelivery> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public WebhookDeliveryPage(IReadOnlyList<WebhookDelivery> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.WebhookDeliveryPage [redacted]";
}
public sealed class WebhookDisable
{
    [JsonConstructor] public WebhookDisable()
    {
    }
    public override string ToString() => "AvailKit.WebhookDisable [redacted]";
}
public sealed class WebhookPage
{
    [JsonPropertyName("items")] public IReadOnlyList<Webhook> Items { get; }
    [JsonPropertyName("nextAfter")] public string? NextAfter { get; }
    [JsonConstructor] public WebhookPage(IReadOnlyList<Webhook> items, string? nextAfter)
    {
        Items = Array.AsReadOnly((items ?? throw AvailKitException.Input("invalid_input")).ToArray());
        NextAfter = nextAfter;
    }
    public override string ToString() => "AvailKit.WebhookPage [redacted]";
}
public sealed class HoursWindow
{
    [JsonPropertyName("weekday")] public long Weekday { get; }
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("end")] public string End { get; }
    [JsonConstructor] public HoursWindow(long weekday, string start, string end)
    {
        Weekday = weekday;
        Start = start;
        End = end;
    }
    public override string ToString() => "AvailKit.HoursWindow [redacted]";
}
public sealed class Slot
{
    [JsonPropertyName("start")] public string Start { get; }
    [JsonPropertyName("end")] public string End { get; }
    [JsonPropertyName("remainingCapacity")] public long RemainingCapacity { get; }
    [JsonConstructor] public Slot(string start, string end, long remainingCapacity)
    {
        Start = start;
        End = end;
        RemainingCapacity = remainingCapacity;
    }
    public override string ToString() => "AvailKit.Slot [redacted]";
}
public sealed class BookingEvent
{
    [JsonPropertyName("id")] public string Id { get; }
    [JsonPropertyName("sequence")] public long Sequence { get; }
    [JsonPropertyName("type")] public string Type { get; }
    [JsonPropertyName("bookingId")] public string BookingId { get; }
    [JsonPropertyName("occurredAt")] public string OccurredAt { get; }
    [JsonConstructor] public BookingEvent(string id, long sequence, string type, string bookingId, string occurredAt)
    {
        Id = id;
        Sequence = sequence;
        Type = type;
        BookingId = bookingId;
        OccurredAt = occurredAt;
    }
    public override string ToString() => "AvailKit.BookingEvent [redacted]";
}
