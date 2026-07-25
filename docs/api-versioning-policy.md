# TMS API Versioning Policy

## What counts as a breaking change

A change is breaking if any existing client would receive a different response without changing its code:

- Removing a field from a response DTO
- Renaming a field in a response DTO
- Changing a field's data type
- Changing an HTTP status code for an existing outcome
- Tightening validation on an existing field (e.g. making optional required)
- Changing the default sort order of a collection response
- Removing an endpoint entirely

Any of these requires a new version.

## What counts as non-breaking (additive)

A change is additive if existing clients can ignore it safely:

- Adding a new optional field to a response (clients ignore unknown fields)
- Adding a new optional query parameter with a documented default
- Adding a new endpoint
- Relaxing validation (making a required field optional)
- Adding a new error code to the errors dictionary

These may ship without a version bump.

## Sunset window

V1 runs for a minimum of 6 months after V2 ships. This accommodates rural training centres and mobile clients on quarterly maintenance schedules. The sunset date is committed in V1DeprecationMiddleware and visible in the Sunset response header from day one of V2.

## Communication

From the day V2 ships:
- Every V1 response carries `Deprecation: true`, `Sunset: <RFC 7231 date>`, and `Link: </api/v2/...>; rel="successor-version"`
- A CHANGELOG entry documents what changed between versions
- An email is sent to every team holding an API key with the sunset date and migration guide
- A calendar invite is sent for the V1 shutdown date

## Skipping versions

Clients are not required to migrate through every intermediate version. V1 clients may migrate directly to V3 when V3 ships. The sunset clock for V1 does not restart when intermediate versions are released.

## Version readers

The primary version reader is URL segment (`/api/v1/`, `/api/v2/`).This is the default and applies to all clients

### Header-based reader (partner opt-in escape hatch)

Some partners — particularly mobile clients with URLs cached at a CDN layer — cannot change the URL path without a CDN invalidation. For these cases, an optional header reader is available as a partner-by-partner opt-in.

When enabled, a partner may send `X-Api-Version: 2.0` on any request to resolve V2 even when the URL path is unversioned:

```
GET /api/courses
X-Api-Version: 2.0
```

This is configured in `AddApiVersioning` by combining both readers:

```csharp
options.ApiVersionReader = ApiVersionReader.Combine(
    new UrlSegmentApiVersionReader(),
    new HeaderApiVersionReader("X-Api-Version"));
```

### Rules for the header escape hatch

- **Not the default.** URL segment versioning is the primary contract for all clients. The header reader is an accommodation, not a replacement.
- **Partner opt-in only.** A partner must request this in writing. It is documented per partner in the API key registry so support knows which clients are using it.
- **URL segment takes precedence.** If a request carries both a versioned URL (`/api/v2/courses`) and a header (`X-Api-Version: 1.0`), the URL segment wins. One reader must be primary — URL segment is primary here.
- **Incident response visibility.** During an incident, engineers read logs. A URL segment (`/api/v1/courses`) is immediately visible in any log line. A version header requires decoding the request headers separately. URL segment is authoritative during incident response for this reason.
- **Deprecation headers still apply.** A V1 request made via the header escape hatch (`X-Api-Version: 1.0` on an unversioned URL) does not receive the `Deprecation`, `Sunset`, and `Link` headers because the middleware checks the path (`/api/v1`), not the header. Partners using the escape hatch must be informed of the sunset date through the communication channels listed above instead.
 An optional header reader (`X-Api-Version`) is available as an opt-in escape hatch for partners with cached CDN URLs. URL segment is authoritative during incident response — it is visible in logs without decoding headers.


