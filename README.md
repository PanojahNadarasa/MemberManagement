# Member Management API

A minimal ASP.NET Core Web API for managing club/organization members, with age-based
member type validation (Minor / Major / Dependant Adult) and status management.

## Tech Stack

- ASP.NET Core Web API
- Entity Framework Core
- MySQL / SQL (via `ApplicationDbContext`)

## Project Structure

```
MemberManagement/
├── Controllers/
│   └── MemberController.cs      # HTTP endpoints for member CRUD + status
├── Services/
│   ├── IMemberService.cs        # Service contract
│   └── MemberService.cs         # Business logic, validation, persistence
├── Data/
│   └── ApplicationDbContext.cs  # EF Core DbContext (DbSet<MemberEntity> members)
├── Entity/
│   └── MemberEntity.cs          # Database entity
├── DTOs/
│   ├── CreateMemberRequest.cs
│   └── UpdateMemberRequest.cs
└── Enums/
    └── MemberType.cs            # Minor, Major, DependantAdult
```

## Domain Model

### MemberEntity

| Property           | Type       | Notes                              |
|---------------------|------------|-------------------------------------|
| MemberId            | Guid       | Primary key, generated on create   |
| RegistrationNumber  | string     | Must be unique                     |
| FirstName           | string     |                                     |
| LastName            | string     |                                     |
| Email               | string     |                                     |
| DateOfBirth         | DateTime   | Cannot be in the future             |
| MemberType          | MemberType | Minor / Major / DependantAdult     |
| IsActive            | bool       |                                     |

### MemberType rules (enforced server-side)

- **Minor** — member must be **under 18**.
- **Major** — member must be **18 or older**.
- **DependantAdult** — member must be **18 or older**.

Age is calculated from `DateOfBirth` as of `DateTime.UtcNow`.

## API Endpoints

| Method | Route                          | Description                          |
|--------|---------------------------------|---------------------------------------|
| POST   | `/api/members`                 | Create a new member                   |
| GET    | `/api/members`                 | Get all members                       |
| GET    | `/api/members/{id}`            | Get a member by ID                    |
| PUT    | `/api/members/{id}`            | Update a member                       |
| DELETE | `/api/members/{id}`            | Delete a member (active members only) |
| PATCH  | `/api/members/{id}/status`     | Activate / deactivate a member        |

### POST `/api/members`

Creates a member after validating:
- `RegistrationNumber` is not already in use.
- `DateOfBirth` is not in the future.
- `MemberType` matches the age rules above.

**Responses**
- `201 Created` — returns the created member, `Location` header points to `GET /api/members/{id}`.
- `400 Bad Request` — validation failure (duplicate registration number, future DOB, or member type/age mismatch).

### GET `/api/members`

Returns all members (read-only, `AsNoTracking`).

- `200 OK` — array of members.

### GET `/api/members/{id}`

- `200 OK` — the member.
- `404 Not Found` — no member with that ID.

### PUT `/api/members/{id}`

Updates an existing member's details, re-running the same DOB and member-type validation as create.

- `200 OK` — returns the updated member.
- `400 Bad Request` — validation failure.
- `404 Not Found` — member does not exist.

### DELETE `/api/members/{id}`

Deletes a member. **Only members with `IsActive == true` can be deleted.**

- `204 No Content` — deleted successfully.
- `400 Bad Request` — member is not active.
- `404 Not Found` — member does not exist.

### PATCH `/api/members/{id}/status`

Body: raw `bool` — the new `IsActive` value.

- `200 OK` — returns the updated member.
- `404 Not Found` — member does not exist, or the update failed.

## Error Handling

Service methods catch and translate exceptions into user-facing messages:
- `DbUpdateException` → `"A database error occurred while ...".`
- Any other exception → `"An unexpected error occurred while ...".`

Controllers map service results to HTTP status codes:
- `"Member not found."` → `404`
- Any other error message → `400`

> **Note:** `DeleteMemberAsync` and `CalculateAge`/`ValidateMemberType` do not currently wrap
> database access in a try/catch the way the other service methods do — consider aligning this
> for consistent error handling across all operations.

## How to run

Requires the .NET 8 SDK.

#
Then open `https://localhost:7093/swagger/index.html` for interactive Swagger UI, or
use `MemberManagement.Api.http` with the VS Code REST Client extension /
Visual Studio's built-in `.http` runner.

## Testing

Integration tests (`MemberControllerTest`, xUnit) run against the real HTTP pipeline via
`WebApplicationFactory<Program>`, with `ApplicationDbContext` swapped to a fresh
EF Core in-memory database per test class instance and cleared before each test. Coverage
includes:

- Create member → `201 Created`
- Get all members → `200 OK`
- Get member by id (existing and non-existent) → `200 OK` / `404 Not Found`
- Update member → `200 OK`
- Activate / deactivate via `PATCH /{id}/status` → `200 OK`, verified against the DB afterward
- Delete member (created via the API first) → `204 No Content`, verified removed from the DB
- Duplicate registration number on create → `400 Bad Request` with the expected error message
- Future date of birth on create → `400 Bad Request` with the expected error message

## Key design decisions & trade-offs

- **Business logic lives in `MemberService`, behind `IMemberService`.** Duplicate-registration
  checks, date-of-birth validation, and age/type consistency (`ValidateMemberType`) are all in
  the service layer rather than the controller, keeping `MemberController` a thin HTTP adapter
  and the rules unit-testable independent of ASP.NET Core.

- **Duplicate registration number is checked in the service** (an `AnyAsync` query before
  insert) rather than relying only on a database constraint. This gives a clean
  `400 Bad Request` with a message instead of an unhandled database exception. A unique index
  on `RegistrationNumber` at the `ApplicationDbContext` level would still be worth adding, so
  uniqueness holds under concurrent requests and not just at the application layer.

- **Duplicate-registration and not-found responses reuse `400`/`404` rather than
  `409 Conflict`.** That's a valid, simpler choice for this scope — flagging it because a
  stricter REST reading would return `409` for a uniqueness conflict on create.

- **Assumption on `MemberType` semantics** (intentionally ambiguous in the brief): `Minor` =
  under 18, and both `Major` and `DependantAdult` = 18 or older. `DependantAdult` is read as a
  benefits/relationship classification (an adult still dependent on another member for benefit
  purposes) rather than a separate age band, so it shares the adult age floor with `Major` but
  isn't otherwise distinguished here. Worth confirming — the field name alone doesn't settle
  which reading is intended.

- **`UpdateMemberAsync` doesn't re-check registration-number uniqueness** the way
  `CreateMemberAsync` does. If `RegistrationNumber` is changed via `PUT` to a value already used
  by another member, that's currently not caught before `SaveChangesAsync`. Flagging as a known
  gap — the fix mirrors the create-path check, scoped to exclude the member's own id.

- **Added a `PATCH /{id}/status` endpoint** beyond the base CRUD set, to toggle `IsActive`
  without requiring a full `PUT` payload — a common real workflow (deactivating a member) that a
  full replace makes clumsier than it needs to be.
