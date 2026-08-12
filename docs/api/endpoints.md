# Endpoints

> [!TIP]
> The core cinema-first endpoints are also described in the
> [OpenAPI specification](../../openapi/cineplexx-rs.yaml), which can be used
> with OpenAPI-compatible tools for easier interactive testing.

Base URL:

```text
https://app.cineplexx.rs/api
```

All endpoints listed here have been observed and currently require no authentication.

Concrete URLs are included where useful so the responses can be inspected directly in a browser.

## Path and query placeholders

```text
{locationId}  Numeric location identifier
{cinemaId}    Numeric cinema identifier
{movieId}     Cineplexx movie identifier
{sessionId}   Numeric screening identifier
{sessionKey}  {cinemaId}-{sessionId}
{YYYY-MM-DD}  Date, e.g. 2026-08-12
```

## Core client endpoints

These endpoints are sufficient for the main cinema-first SeatPeek workflow.

### Locations

```http
GET /v1/locations
```

Returns locations and their associated cinemas.

Use this to determine which cinemas belong to a selected city.

Example:

https://app.cineplexx.rs/api/v1/locations

### Cinema details

```http
GET /v1/cinemas/{cinemaId}
```

Returns information about one cinema.

Example using cinema `1116`:

https://app.cineplexx.rs/api/v1/cinemas/1116

### Cinema sessions

```http
GET /v1/cinemas/{cinemaId}/sessions
```

Returns the cinema programme grouped by date.

Use this as the main starting point for discovering screenings at a cinema.

Sessions include identifiers and technologies that can be used for filtering.

Example using cinema `1116`:

https://app.cineplexx.rs/api/v1/cinemas/1116/sessions

This endpoint is also useful for obtaining current `sessionId` values for the session and seat-plan endpoints below.

### Session details

```http
GET /v1/sessions/{sessionKey}
```

where:

```text
sessionKey = {cinemaId}-{sessionId}
```

Returns details for one screening, including showtime, movie data, and aggregate seat availability.

Relevant fields include:

```text
movieId
cinemaId
sessionId
showtime
seatsAvailable
seatsTotal
screenName
sessionAttributesNames
scheduledFilm
```

For example, if a cinema programme returns:

```text
cinemaId  = 1116
sessionId = 11942
```

the corresponding session key is:

```text
1116-11942
```

and the request would be:

https://app.cineplexx.rs/api/v1/sessions/1116-11942

> Session examples are time-sensitive because screenings eventually expire. Use a current session from the cinema programme when testing this endpoint.

### Seat plan

```http
GET /v1/seat-plan/{cinemaId}/{sessionId}
```

Returns seat-level availability for one screening.

Use this only when individual seat information is required.

For:

```text
cinemaId  = 1116
sessionId = 11942
```

the request would be:

https://app.cineplexx.rs/api/v1/seat-plan/1116/11942

> Seat-plan examples are also time-sensitive. Use identifiers from a currently scheduled session.

## Other cinema endpoints

```http
GET /v1/cinemas
```

Returns cinema information.

Example:

https://app.cineplexx.rs/api/v1/cinemas

```http
GET /v1/cinemasweb/with-movies?date={YYYY-MM-DD}&locationId={locationId}
```

Returns cinemas with movies showing on the selected date.

`cinemasweb` is a website-oriented aggregated endpoint.

Date-specific examples may naturally become outdated.

## Movies

### Movie catalogue

```http
GET /v1/movies
GET /v2/movies
```

Examples:

https://app.cineplexx.rs/api/v1/movies

https://app.cineplexx.rs/api/v2/movies

### Movie details

```http
GET /v1/movies/{id-or-slug}
```

Movie details can be requested using either the movie ID or its human-readable slug.

Using **Pera kojot protiv sistema**:

```text
movieId = HO00019842
slug    = pera-kojot-protiv-sistema
```

By ID:

https://app.cineplexx.rs/api/v1/movies/HO00019842

By slug:

https://app.cineplexx.rs/api/v1/movies/pera-kojot-protiv-sistema

> Also probably time-sensitive examples

### Filtered programme

```http
GET /v2/movies?date={YYYY-MM-DD}&location={locationId}
```

Returns movies for a particular date and location.

Example:

```text
https://app.cineplexx.rs/api/v2/movies?date=2026-08-12&location=2
```

### Recommended movies

```http
GET /v2/movies/top?date={YYYY-MM-DD}&location={locationId}
```

Example:

```text
https://app.cineplexx.rs/api/v2/movies/top?date=2026-08-12&location=2
```

### Upcoming movies

```http
GET /v2/movies/coming-soon?location=all
GET /v2/movies/coming-soon?date={YYYY-MM-01}&location=all
```

Examples:

https://app.cineplexx.rs/api/v2/movies/coming-soon?location=all

```text
https://app.cineplexx.rs/api/v2/movies/coming-soon?date=2026-08-01&location=all
```

## Movie filters

### Available programme dates

```http
GET /v2/movies/filters/dates/list?location={locationId}
```

Example using location `2`:

https://app.cineplexx.rs/api/v2/movies/filters/dates/list?location=2

### Recommended movie dates

```http
GET /v2/movies/filters/dates/list?top=true&location={locationId}
```

Example:

https://app.cineplexx.rs/api/v2/movies/filters/dates/list?top=true&location=2

### Coming-soon months

```http
GET /v2/movies/filters/months/list?comingSoon=true
```

Example:

https://app.cineplexx.rs/api/v2/movies/filters/months/list?comingSoon=true

### Movie/cinema-specific dates

```http
GET /v2/movies/filters/dates/list?id={movieId}&cinemaId={cinemaId}&location=all
```

## Movie sessions

Website-oriented movie-first session endpoints have also been observed:

```http
GET /v2/moviesweb/{movieId}/sessions?date={YYYY-MM-DD}&location={location}
GET /v3/moviesweb/{movieId}/sessions?location={location}
```

The exact relationship between the `/v2` and `/v3` variants is unknown.

## Events

```http
GET /v1/events?location={locationId}
GET /v1/events/filters/months/list
```

Examples:

https://app.cineplexx.rs/api/v1/events?location=2

https://app.cineplexx.rs/api/v1/events/filters/months/list

## Information

```http
GET /v1/information/legal-info
```

Returns metadata for legal and informational documents.

Example:

https://app.cineplexx.rs/api/v1/information/legal-info
