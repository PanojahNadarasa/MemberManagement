## How to run
Requires the .NET 8 SDK.

#
Then open `https://localhost:7093/swagger/index.html` for interactive Swagger UI, or
use `MemberManagement.Api.http` with the VS Code REST Client extension /
Visual Studio's built-in `.http` runner.

# API

| Method | Route              | Description                          |
|--------|--------------------|---------------------------------------|
| GET    | `/api/members`     | List members. Optional `isActive` and `memberType` query filters. |
| GET    | `/api/members/{id}`| Get one member.                       |
| POST   | `/api/members`     | Create a member.                      |
| PUT    | `/api/members/{id}`| Replace a member's fields.            |
| DELETE | `/api/members/{id}`| Remove a member.   
|PATCH   | `/api/members/{id}` | status changes(Active,Inactive)       |

### Member fields

`registrationNumber` (string, unique), `firstName`, `lastName`, `email`,
`dateOfBirth` (`yyyy-MM-dd`), `memberType` (`Minor` | `Major` |
`DependantAdult`), `isActive` (bool).

### Key design decisions & trade-offs

- **Controller + Entity + Data only, no service/repository layer.** All
  business logic (duplicate-registration check, date-of-birth validation,
  age/type consistency) lives directly in `MemberController`. For a larger
  system I'd move this into a service so it's reusable and unit-testable
  independent of ASP.NET Core, but for one CRUD resource that layer would be
  ceremony rather than value.

- **Duplicate registration number is checked in the controller** (an
  `AnyAsync` query before insert) rather than only relying on a database
  constraint. This gives a clean `400 Bad Request` with a message instead of
  an unhandled database exception. I'd add a unique index on
  `RegistrationNumber` at the `ApplicationDbContext` level too, so uniqueness
  is still guaranteed under concurrent requests and not just at the
  application layer.
   **Duplicate registration number is checked in the controller** (an
  `AnyAsync` query before insert) rather than only relying on a database
  constraint. This gives a clean `400 Bad Request` with a message instead of
  an unhandled database exception. I'd add a unique index on
  `RegistrationNumber` at the `ApplicationDbContext` level too, so uniqueness
  is still guaranteed under concurrent requests and not just at the
  application layer.

- **Duplicate-registration and not-found responses reuse `400`/`404` rather
  than `409 Conflict`.** That's a valid, simpler choice for this scope — flagging
  it because a stricter REST reading would return `409` for a uniqueness
  conflict on create.
  - **Validation is split two ways**: field-level rules (required fields,
  email format, etc.) would sit on `MemberEntity` via `DataAnnotations` and
  are enforced automatically by `[ApiController]`'s model validation before
  the action runs. Cross-field business rules that annotations can't
  express — date of birth not in the future, and `MemberType` being
  consistent with computed age — are checked explicitly in the controller
  via `ValidateMemberType`.

- **Assumption on `MemberType` semantics** (intentionally ambiguous in the
  brief): `Minor` = under 18, and both `Major` and `DependantAdult` = 18 or
  older. I read `DependantAdult` as a benefits/relationship classification
  (an adult still dependent on another member for benefit purposes) rather
  than a separate age band, so it shares the adult age floor with `Major`
  but isn't otherwise distinguished here. Worth confirming — the field name
  alone doesn't settle which reading is intended.

- **`PUT` doesn't re-check registration-number uniqueness** the way `POST`
  does. If `RegistrationNumber` is changed via `PUT` to a value already used
  by another member, that's currently not caught before `SaveChangesAsync`.
  Flagging as a known gap — the fix mirrors the create-path check, scoped to
  exclude the member's own id.

  - **Added a `PATCH /{id}/status` endpoint** beyond the base CRUD set, to
  toggle `IsActive` without requiring a full `PUT` payload — a common real
  workflow (deactivating a member) that a full replace makes clumsier than
  it needs to be.
