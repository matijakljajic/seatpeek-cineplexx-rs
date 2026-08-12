# API response samples

This directory contains representative, trimmed responses from the undocumented Cineplexx Serbia API. They are documentation fixtures and future test data.

The responses were observed on **2026-08-12** using cinema **1111** (`CINEPLEXX 4D DELTA CITY`) and session **1111-70992**. Session and seat-plan samples are snapshots: the screening may no longer be active and its live availability will change.

Samples retain API structure and fields useful to clients, while omitting irrelevant or potentially copyrighted material such as images, descriptions, trailers, and promotional metadata. They are intentionally not complete copies of upstream responses.

## Refreshing samples

Use the documented discovery flow rather than reusing the time-sensitive session identifier above:

```text
locations
    ↓
cinema
    ↓
cinema sessions
    ↓
session details
    ↓
seat plan
```

1. `GET https://app.cineplexx.rs/api/v1/locations`
2. Choose a cinema ID and call `GET /v1/cinemas` and `GET /v1/cinemas/{cinemaId}`.
3. Call `GET /v1/cinemas/{cinemaId}/sessions`, then select a returned session key.
4. Call `GET /v1/sessions/{sessionKey}` and use its `session.sessionId`.
5. Call `GET /v1/seat-plan/{cinemaId}/{sessionId}`.

See the [experimental OpenAPI specification](../openapi/cineplexx-rs.yaml) for the observed flow and field descriptions.

## Live contract check

`CineplexxLiveApiContractTests` repeats the discovery flow against the live API, trying currently returned sessions until both the session-details and seat-plan requests return `200`. It then checks only required response shapes and non-volatile invariants. The test is skipped unless explicitly enabled:

```bash
RUN_LIVE_API_CONTRACT_TESTS=true dotnet test \
  tests/SeatPeek.Cineplexx.Rs.Api.Tests/SeatPeek.Cineplexx.Rs.Api.Tests.csproj \
  --filter 'Category=LiveApiContract'
```

**Check live Cineplexx API contract (read-only)** verifies the current cinema-first API flow without changing repository or Wiki content. **Refresh Cineplexx API samples and Wiki examples** runs the same validation, then refreshes these samples and the live values in the Wiki endpoints page.
