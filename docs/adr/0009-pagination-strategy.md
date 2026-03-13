# ADR-0009: Pagination Strategy — Offset-Based with PageResponse&lt;T&gt;

## Status
`Accepted`

## Date
2026-03-13

## Context

Multiple endpoints across the Suppliers and Scraping modules return collections of potentially large size (supplier list, screening history, risk list results). A consistent pagination contract is required that:

- Prevents unbounded queries from exhausting database or memory resources.
- Gives clients enough metadata to render paginated UI (page numbers, next/previous buttons) without performing extra calculations.
- Is reusable across all modules without duplicating the response shape.
- Is simple enough for Phase 1 and does not impose unnecessary infrastructure (e.g. stateful cursors).

Two fundamental approaches were evaluated: **offset-based pagination** and **cursor-based pagination**.

---

## Decision

**Use offset-based pagination with a shared `PageResponse<T>` / `PageMetadata` / `PageRequest` contract living in `Shared/Domain`.**

### Input: `PageRequest`

All paginated endpoints accept a `PageRequest` object (query string parameters):

| Parameter | Type | Default | Max | Description |
|-----------|------|---------|-----|-------------|
| `page` | `int` | `0` | — | Zero-based page index |
| `size` | `int` | `20` | `100` | Number of items per page |
| `sortBy` | `string?` | `null` | — | Field name to sort by |
| `sortDirection` | `string?` | `asc` | — | `asc` or `desc` |

A maximum page size of `100` is enforced by FluentValidation to prevent clients from issuing unbounded requests.

### Output: `PageResponse<T>`

All paginated endpoints return a `PageResponse<T>` envelope:

```json
{
  "content": [...],
  "meta": {
    "number": 0,
    "size": 20,
    "totalElements": 143,
    "totalPages": 8,
    "first": true,
    "last": false,
    "hasNext": true,
    "hasPrevious": false
  }
}
```

`PageMetadata` fields are pre-computed server-side so the client never needs to derive `hasNext`, `hasPrevious`, or `totalPages` itself.

### Shared placement

`PageResponse<T>`, `PageMetadata`, and `PageRequest` live in `Shared/Domain` and are used by:
- **Suppliers module** — `GET /api/v1/suppliers`
- **Scraping module** — all three list endpoints (`/lists/ofac`, `/lists/worldbank`, `/lists/icij`)

---

## Evaluated Options

### Option A — Offset-based pagination ✅ Selected

| Pros | Cons |
|------|------|
| Simple to implement with EF Core (`.Skip().Take()`) | Performance degrades at very high offsets (not a concern for this domain) |
| Supports random-access navigation (jump to page N) | Count query adds one extra DB round trip |
| Client can render full page-number UI | Results may shift between pages if data is mutated during browsing |
| Stateless — no server-side cursor to maintain | |

### Option B — Cursor-based pagination

| Pros | Cons |
|------|------|
| Stable under concurrent inserts/deletes | Cannot jump to arbitrary page — only next/previous |
| Scales to arbitrarily large offsets | More complex to implement and test |
| | Requires an opaque cursor token in the response |
| | Incompatible with page-number UI (PrimeNG Paginator) |

Cursor-based pagination is better suited for high-volume, real-time feeds (e.g. social timelines). The supplier and risk-list data in this platform is low-volume and updated infrequently, making offset pagination the correct choice.

### Option C — No pagination (return all results)

| Pros | Cons |
|------|------|
| Simplest implementation | Unbounded response size |
| | Breaks UI rendering for large datasets |
| | Not acceptable for production |

---

## Consequences

### Positive
- Uniform response shape across all collection endpoints — clients only need to learn one pattern.
- `PageMetadata` boolean helpers (`hasNext`, `hasPrevious`, `first`, `last`) simplify frontend pagination logic.
- `PageRequest` validation (max size 100) is enforced via FluentValidation — consistent error response on violation.
- Swapping the backing implementation (e.g. Dapper instead of EF Core) does not change the API contract.

### Negative / Mitigations

| Risk | Mitigation |
|------|-----------|
| Count query overhead on every paginated request | Accept for Phase 1. Phase 2 can add count caching or use `EF Core` split queries if profiling shows it as a bottleneck |
| Page drift on concurrent writes | Acceptable for compliance workflows — suppliers are not inserted at high frequency |
| Client requests `size=10000` | Enforced maximum of `100` via FluentValidation — returns HTTP 400 on violation |

---

## Dependencies

No additional packages required. Offset pagination is implemented via EF Core's built-in `.Skip()` / `.Take()` / `.CountAsync()` methods.

---

## Future Work

- **Phase 2:** Expose `X-Total-Count` header for clients that prefer header-based metadata over envelope-based.
- **Phase 2:** Add configurable default and max page sizes via `appsettings.json` under `Pagination:DefaultSize` and `Pagination:MaxSize`.
- **Phase 2:** Evaluate cursor-based pagination for audit log endpoints if volume exceeds 100k rows.
