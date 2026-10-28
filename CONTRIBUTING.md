# Contributing to ANDtr

ANDtr is a web platform for searching a provided dataset of medical device safety and recall
notices. Employees select the notices relevant to a risk review, comment on them, store their
findings and share those findings with customers. Customers see only the findings shared with them,
and can add comments.

Once the core is working, two nice-to-haves are on the table: smarter search, and a PDF report of
findings.

New to the project? Start with [SETUP.md](SETUP.md) — it gets you building and running in about ten
minutes. This file covers the conventions we hold each other to afterwards.

## Stack

- ASP.NET Core MVC on .NET 10. The SDK is pinned in `global.json`, any 10.0.x feature band.
- Entity Framework Core with SQLite. The local dev database is `src/ANDtr.Web/app.db` and is never
  committed.
- ASP.NET Core Identity (individual accounts) with roles.
- xUnit for tests.
- ClosedXML for reading the Excel dataset.
- GitHub Actions CI (`.github/workflows/build.yml`): restore, build and test on every pull request
  and on every push to `main`.

## Repository layout

```
ANDtr.sln
src/ANDtr.Web/          MVC app (Controllers/, Views/, Models/, Data/, Areas/)
  Models/               Domain entities, one per file
  Data/ApplicationDbContext.cs
  Data/Migrations/      EF Core migrations
  Data/Roles.cs         Role name constants
  Data/DevDataSeeder.cs Development-only roles, test users, test organisation
  Data/NoticeImporter.cs Development-only dataset import
  Areas/Admin/          Admin MVC area
tests/ANDtr.Tests/      xUnit tests
.config/dotnet-tools.json   local tools (dotnet-ef)
data/private/           client dataset - git-ignored, never commit
```

## Roles and permissions

| Role | Can |
|---|---|
| `Employee` | search notices, select notices for a risk review, comment, create findings, share findings with customers |
| `Customer` | view findings shared with them, add comments; nothing else |
| `Admin` | manage users and customer organisations, manage the dataset, read the activity log |

Three rules, and the third is the one that bites:

- Use the constants in `Roles` (`Roles.Employee`, `Roles.Customer`, `Roles.Admin`) rather than string
  literals.
- Enforce access on the server with `[Authorize(Roles = Roles.Employee)]`, or with a policy. Hiding
  a button in a view is not access control.
- A customer must only ever see findings shared with *their* account. Check ownership in the query
  itself — the role check alone lets one customer read another customer's findings.
  `OrganisationService` does this for you: list reviews with `SharedWith(organisationId)` (the
  organisation comes from `GetOrganisationIdAsync(User)`), and before showing one review call
  `CanViewReviewAsync(User, reviewId)` and return `Forbid()` when it is false.

## Identity setup and dev seeding

`Program.cs` registers Identity with `AddIdentity<ApplicationUser, IdentityRole>`, using the rules in
`Data/IdentitySetup.cs`: email is the username, and passwords need at least 8 characters with an
upper-case letter and a digit. There is no self-registration — only an admin creates accounts. Login,
logout and Access denied live in `AccountController` (`/Account/Login`, `/Account/AccessDenied`).
`ApplicationSignInManager` refuses sign-in for users with `IsActive = false` and signs out a user on
their next request once they are deactivated.

`DevDataSeeder` runs **only when the environment is Development**. It applies migrations, creates
the `Employee`, `Customer` and `Admin` roles, creates one test user per role, creates one
`CustomerOrganisation` and assigns the test customer to it. It then calls `NoticeImporter`, which
imports the dataset if the `Notices` table is empty.

Test user credentials come from `dotnet user-secrets` — never from code, never from committed
config:

```bash
dotnet user-secrets --project src/ANDtr.Web set "Seed:Employee:Email" "employee@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Employee:Password" "<password>"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Customer:Email" "customer@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Customer:Password" "<password>"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Admin:Email" "admin@andtr.local"
dotnet user-secrets --project src/ANDtr.Web set "Seed:Admin:Password" "<password>"
```

If a secret is missing, or Identity rejects the password (minimum 8 characters, with an upper-case
letter and a digit), the seeder logs a warning and skips that user. Startup never fails because of
seeding.

## Domain model

The whole model ships in one migration, `InitialDomainModel`, so nobody needs to add a second
migration to start their story. Entities live in `src/ANDtr.Web/Models/`, one per file, and the
relationships, required fields and indexes are configured in `ApplicationDbContext.OnModelCreating`.

| Entity | Fields |
|---|---|
| `ApplicationUser : IdentityUser` | `FullName`, `OrganisationId` (nullable; required for customers, null for staff), `IsActive` (defaults to `true`), `CreatedAt`, `LastLoginAt?` |
| `CustomerOrganisation` | `Id`, `Name` (unique), `ContactEmail`, `Notes?`, `CreatedAt` |
| `Notice` | `Id`, `ProductCode`, `ProductCodeName`, `RegulationNumber`, `WebAddress`, `RecallNumber`, `ProductDescription`, `TradeName?`, `RecallClass`, `CenterClassificationDate`, `PostedInternetDate?`, `TerminationDate?`, `FeiNumber?`, `FirmName`, `ManufacturerRecallReason` |
| `RiskReview` | `Id`, `Name`, `OwnerId` (employee), `OrganisationId?` (null until shared), `CreatedAt`, `SharedAt?` |
| `ReviewNotice` | `Id`, `RiskReviewId`, `NoticeId`, `AddedAt` |
| `Comment` | `Id`, `RiskReviewId`, `ReviewNoticeId?`, `AuthorId`, `Body`, `IsInternal`, `CreatedAt` |
| `Notification` | `Id`, `UserId`, `RiskReviewId`, `Message`, `CreatedAt`, `IsRead` |
| `ActivityLog` | `Id`, `UserId`, `Action`, `RiskReviewId?`, `CreatedAt` |

Things worth knowing before you build on it:

- `Notice` mirrors the dataset columns and nothing else. `RecallNumber` is **not** unique — 33 recalls
  appear twice, under two different product codes. The unique key is `(RecallNumber, ProductCode)`,
  which is indexed as such; `RecallNumber` alone has its own non-unique index for search.
- A `Comment` with no `ReviewNoticeId` is a comment on the review as a whole; with one, it is a
  comment on a single notice inside that review. When a comment has a `ReviewNoticeId`, that
  `ReviewNotice` must belong to the same `RiskReviewId`. Set both from the same review in code; the
  database does not enforce this.
- `ReviewNotice` has a unique index on `(RiskReviewId, NoticeId)`, so a notice cannot be added to the
  same review twice.
- Deleting a `RiskReview` cascades to its `ReviewNotice`, `Comment` and `Notification` rows, and
  nulls the `RiskReviewId` on its `ActivityLog` rows. Users and notices are never deleted by a
  cascade — those relationships are restricted.

## Area ownership

One folder or area per feature. Employee and customer features are plain controllers; the admin
features live in the `Admin` MVC area. Every controller carries
`[Authorize(Roles = ...)]`, and every `Index` view is a heading and nothing more until its owner
builds it out.

| Feature | Controller | Role | Owner | Also works here |
|---|---|---|---|---|
| Search | `Controllers/SearchController.cs` | `Employee` | _TBC_ | |
| Reviews | `Controllers/ReviewsController.cs` | `Employee` | _TBC_ | _TBC_ — comments and sharing |
| Shared reviews | `Controllers/SharedReviewsController.cs` | `Customer` | _TBC_ — list | _TBC_ — detail view and comments |
| Notifications | `Controllers/NotificationsController.cs` | `Customer` | _TBC_ | |
| Admin users | `Areas/Admin/Controllers/UsersController.cs` | `Admin` | Vrunda | Veer — deactivation |
| Admin organisations | `Areas/Admin/Controllers/OrganisationsController.cs` | `Admin` | Vrunda | |
| Admin dataset | `Areas/Admin/Controllers/DatasetController.cs` | `Admin` | Veer | |
| Admin activity log | `Areas/Admin/Controllers/ActivityLogController.cs` | `Admin` | Veer | |

After signing in, each role lands on its own page — `HomeController.Index` redirects `Employee` to
Search, `Customer` to Shared reviews and `Admin` to Admin users. The shared layout shows only the
signed-in role's links.

## Testing

- Tests live in `tests/ANDtr.Tests` and use xUnit. Name them `Method_Scenario_ExpectedResult`.
- Database tests use in-memory SQLite (`DataSource=:memory:`, keeping the connection open), not the
  EF InMemory provider. `DevDataSeederTests` is the example to copy.
- Integration tests built on `WebApplicationFactory` **must** call `builder.UseEnvironment("Testing")`.
  The factory defaults to Development, which would run the dev seeder against a real database.
  `WebAppFactory` already does this and signs in through the real `/Account/Login` form, so
  `AuthorizationTests` exercises the actual cookie and role pipeline. Add a route there whenever you
  add a controller.
- New features need tests, and CI has to be green before anything merges.

## Coding conventions

- Follow `.editorconfig`: file-scoped namespaces, `_camelCase` private fields, PascalCase public
  members, `I` prefix on interfaces, `Async` suffix on async methods, LF line endings.
- Nullable reference types are enabled. Fix the warnings rather than suppressing them.
- Use the async EF Core calls (`ToListAsync`, `SaveChangesAsync`) in controllers and services.
- Keep controllers thin. Query and business logic belongs in services registered with DI.
- No secrets in code or in committed config. `appsettings.Development.json` is committed and must
  stay secret-free; use `dotnet user-secrets`.
- Give migrations descriptive names — `AddFindingComments`, not `Update1`.
- Never commit the client dataset or any `*.db` file.

## Branch workflow

`main` is always buildable, and **nobody pushes to it directly**.

- One branch per piece of work: `feature/<short-name>`, `fix/<short-name>` or `chore/<short-name>`.
- Open a pull request. At least one teammate reviews it, and CI has to pass, before it merges.
- Keep commits small and focused, and write your own — individual contributions are assessed from
  the git history. Prefix messages with `feat:`, `fix:`, `chore:`, `docs:`, `test:` or `ci:`.
- Pull `main` and rebase or merge before opening a pull request, so conflicts stay small. Migrations
  conflict easily — tell the team before you add one.

## Commands

Run these from the repository root.

```bash
dotnet tool restore                                   # installs dotnet-ef from .config/dotnet-tools.json
dotnet build
dotnet test
dotnet run --project src/ANDtr.Web                    # https://localhost:7095
dotnet ef migrations add <Name> --project src/ANDtr.Web
dotnet ef database update --project src/ANDtr.Web     # optional: the dev seeder migrates on startup
```
