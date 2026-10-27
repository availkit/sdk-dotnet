

SDK **0.1.1**. Standalone build and synthetic test instructions are in
[DEVELOPMENT.md](DEVELOPMENT.md). [Versioned release notes](https://github.com/availkit/sdk-dotnet/releases)
record dated verification and available distribution channels. Source availability
does not imply registry publication or mobile/service acceptance.
Install examples name this SDK version; check the release notes for artifact availability.

# AvailKit .NET SDK 0.1.1

`AvailKit.0.1.1.nupkg` is a conventional hosted NuGet package. Download the nupkg from `https://availkit.dev/downloads/AvailKit.0.1.1.nupkg`, verify its SHA256 against the public `SHA256SUMS`, put it in a local package folder, add that folder as a NuGet source and reference version0.1.1. This is hosted distribution, not a nuget.org availability/ownership claim. It targets **net10.0**, the supported .NET10 LTS line, with C#14; use current security updates. SDK10.0.402 is pinned for source verification. Other target frameworks are unverified. The package uses only the .NET runtime: no external runtime packages, native account APIs or private application repository.

Download `availkit-dotnet-examples-0.1.1.zip`, place the verified nupkg in `packages/` at the ZIP root, enter `examples/FirstBooking`, then run `dotnet restore --configfile NuGet.Config` and `dotnet run --no-restore -- /absolute/private/config.json`. Its project references the downloaded package from the local folder; no private app checkout is needed. The complete example discovers times, creates an owned test booking, reads it, moves it, then cancels exactly that booking. It requires a pre-existing owned test offering with hours and an issued test Read+Book key. It does not create contacts/catalog or erase data. Each acknowledgment gates the next step; any error stops with original private intents retained.

<!-- docs:install:start -->
```sh
curl -fsS https://availkit.dev/downloads/AvailKit.0.1.1.nupkg -o AvailKit.0.1.1.nupkg
curl -fsS https://availkit.dev/downloads/availkit-dotnet-examples-0.1.1.zip -o availkit-dotnet-examples-0.1.1.zip
curl -fsS https://availkit.dev/downloads/SHA256SUMS -o SHA256SUMS
sha256sum --check --ignore-missing SHA256SUMS
unzip availkit-dotnet-examples-0.1.1.zip -d availkit-dotnet-consumer
mkdir -p availkit-dotnet-consumer/packages
cp AvailKit.0.1.1.nupkg availkit-dotnet-consumer/packages/
cd availkit-dotnet-consumer/examples/FirstBooking
dotnet restore --configfile NuGet.Config
```
<!-- docs:install:end -->

On Linux, create a fresh owned0700 config directory and another owned0700 intent directory, a0600 key file containing only the credential and one optional newline, and a0600 config:

```json
{"workspaceId":"YOUR_32_HEX_ID","environment":"test","credentialFile":"/absolute/private/key","intentDirectory":"/absolute/private/new-intents","offeringId":"YOUR_32_HEX_ID","from":"2030-01-01T00:00:00Z","to":"2030-01-02T00:00:00Z"}
```

Choose actual future UTC dates no more than seven days apart. Optional `baseUrl` defaults to `https://api.availkit.com`. Never put a credential in process arguments. This sample requires explicit `test` intent and a matching issued test credential; the label cannot certify the key’s actual server environment. `PrivateFiles` is Linux x64 only: it checks owner/mode/type from the opened descriptor, pins the owned0700 directory, uses openat/no-follow, bounds input size, exclusively creates0600 intent files and fsyncs file/directory. It rejects symlinks, special files, shared permissions and replacement. On other platforms read credentials from your application vault and persist `RequestIntent.ExportPrivate()` using that vault; the HTTP SDK itself is portable .NET10. Do not treat chmod-only checks as equivalent to ownership/handle checks.

`WorkspaceClient(workspaceId, AvailKit.Environment.Test, credential, options)` covers all34 stable workspace operations; `PublicBookingClient(pageId, options)` covers all seven public operations. Both accept cancellation tokens on every asynchronous operation, own their transport, and should be disposed on shutdown. WorkspaceID/environment are explicit intent; actual scope/permissions derive from the issued key. A label cannot switch a key from test to live. Read is needed for reads; Edit catalog for catalog/hours/page writes; Book for booking changes; a linked customer additionally needs Manage. Contacts and webhook administration require Manage; erasure/history pruning additionally Privacy; expired replay pruning requires Privacy. No SDK method creates account credentials, sessions, passkeys or recovery, and no browser/native account API is invented. Do not embed workspace credentials in mobile/browser code.

Requests have a finite total cancellation budget (default10s,1ms–120s), response limit (default1MiB, hard8MiB),32 admitted calls per client and four connections per server. HTTPS verifies hostname and the current platform trust store. HTTP works only for explicitly configured localhost/127.0.0.1/::1 test origins. Redirects, cookies, ambient proxies, credential authenticators and decompression are disabled. There is no SDK retry policy/loop. Each mutation uses single-serialization content, preventing its body from being resent by an internal transport followup. Do not add a retry handler. Cancellation/timeout after mutation admission is **unknown**, not proof the server rolled back. `AvailKitException.Uncertain` identifies that case; `Kind`, `Code`, `Status` and `Diagnostics` contain only finite safe fields, never raw URLs, headers, responses, request/contact values, nested exceptions or signing secrets. Recognized structured4xx rejection is known; an unrecognized error code on a mutation stays unknown; protocol-invalid mutation success is unknown.

Use `PrepareBook`, `PrepareCancel`, `PrepareRescheduleBooking` (public `PrepareReschedule`) to snapshot original body/key/scope/capabilities. `ExportPrivate`/`ImportPrivate` never send. Persist before the first `ExecuteAsync(intent)`, retain after uncertainty and only explicitly replay the **same** original intent under identical scope. Local expiry rejects preparations at24h; server receipt retention, privacy pruning and authority changes can still deny an otherwise young replay. Do not edit body/key/tokens, rotate authority or invent a new key as a retry. Only these booking operations have replay guarantees. Other writes (including one-time webhook creation) send once without a replay contract. An authoritative read can inspect a known booking, but cannot discover an unknown create from capability alone, recover its lost acknowledgment or unlock a contradictory mutation by itself.

Public clients carry published pageIDs and per-booking capabilities, never workspace/account keys. Generate `Secrets.NewBookingToken()` and `NewContactToken()` before creation. Contact-reuse proof and booking capability are distinct private values; retain the exported create intent in secure storage. Never put capabilities in URLs, logs or analytics. Models use immutable getters/copies and camelCase wire names; all `ToString` methods redact values, including contact and one-time signing-secret DTOs. Validation covers strict duplicate-free JSON, exact success status/media, bounded schema/types/IDs/UTC timestamps, pagination, matching resource/booking/page identity, chronological times, maintenance assertions and usage counters. RequestID is32 lowercase hex; intent key16–128 printable ASCII without surrounding whitespace. Move timestamps have millisecond precision or coarser. IANA timezone existence and webhook target DNS/address admission remain server checks.

Collection calls fetch one bounded page. Pass `NextAfter` unchanged only to the same collection, key/environment and filters; impose a finite application page budget. The `Events` response exposes its wire `events` array as `Items` (to avoid a C# type/member name collision). Events use numeric cursor/retention floor and surface retention expiry rather than silently resetting. Availability is advisory and booking can conflict. Usage remaining counts are advisory, not reservations. `CreateWebhookAsync` returns `SigningSecret` once: store it privately immediately and never print the DTO's properties.

`Webhooks.Verify(rawBody, signature, secret)` authenticates exact raw bytes before parsing with bounded16KiB, canonical32-byte secret, exact signature grammar, HMAC-SHA256, constant-time comparison and absolute timestamp tolerance (300s default, up to3600s). Only then transactionally deduplicate stable `AvailKit-Delivery-ID`; retries retain identity and refresh signatures. Signature verification does not grant permission to log contact payloads.

Source build/check: `dotnet run --project tests/AvailKit.Tests.csproj -c Release` and `dotnet pack src/AvailKit/AvailKit.csproj -c Release`. `python3 build.py --output /absolute/artifacts` runs tests, deterministic pack and assembles the normalized package and standalone example ZIP. `coverage.json` records the canonical OpenAPI hash and41-operation matrix; `contract.json` is the maintained strict subset; [METHODS.md](METHODS.md) lists every typed method. Separate live installed-consumer acceptance is required in addition to local fixture tests.

Runtime references checked25October2026: [current .NET10 SDK](https://dotnet.microsoft.com/en-us/download), [HTTP client guidance](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/httpclient-guidelines), [descriptor-based Unix permissions](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.getunixfilemode?view=net-10.0). Registry publication is not activated by this SDK.

After creating the protected configuration and fresh intent directory, explicitly run the example:

<!-- docs:run:start -->
```sh
dotnet run --no-restore -- /absolute/private/config.json
```
<!-- docs:run:end -->

The example ZIP and nupkg include `coverage.json` and `runtime-policy.json` at their roots. Runtime support policy and operation coverage do not establish registry publication.


See [CHANGELOG.md](CHANGELOG.md), [SUPPORT.md](SUPPORT.md) and
[SECURITY.md](SECURITY.md) for release policy and private reporting prerequisites.
`contract-projection.json` pins only this SDK's supported wire operations;
additive unrelated API routes do not imply this SDK implements them.

The separate optional business export endpoint and current versioned browser
recovery protocol are not methods of this SDK. Use their documented browser/
TypeScript integration where appropriate; this package preserves its declared
workspace/public booking surface rather than inventing account/native APIs.
