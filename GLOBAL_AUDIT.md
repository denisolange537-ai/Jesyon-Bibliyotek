# Global Application Audit

## Phase 0

**Date:** 2026-08-26  
**Scope:** read-only audit before Employees/Payroll implementation  
**Status:** Complete. No application code was modified.

## Executive Summary

The repository contains a single WPF desktop application and one automated test project. The application is a library-management baseline using .NET 10, EF Core 10, SQLite, dependency injection, hosted startup, and service/ViewModel-based business logic.

The existing library modules are implemented and covered by automated tests. Employees, payroll, company management, granular permissions, and user-management workflows do not currently exist. The main prerequisite before adding sensitive HR/payroll data is to establish authorization at the service/command boundary and to define the required Employee-to-User relationship without breaking the existing authentication flow.

Phase 0 validation completed successfully:

- `dotnet restore GestionBibliotheque.slnx`: passed.
- `dotnet build GestionBibliotheque.slnx -c Release --no-restore`: passed.
- `dotnet test GestionBibliotheque.slnx -c Release --no-build`: passed, 21/21 tests.

## Repository and Architecture

### Solution and projects

- `GestionBibliotheque.slnx`: solution containing the application and test projects.
- `src/GestionBibliotheque/GestionBibliotheque.csproj`: WPF executable targeting `net10.0-windows`.
- `tests/GestionBibliotheque.Tests/GestionBibliotheque.Tests.csproj`: xUnit test project targeting `net10.0-windows`.

### Verified packages and platform

The application references:

- `Microsoft.EntityFrameworkCore.Design` 10.0.0.
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0.
- `SQLitePCLRaw.lib.e_sqlite3` 2.1.13.
- `Microsoft.Extensions.Identity.Core` 10.0.0.
- `Microsoft.Extensions.Hosting` 10.0.0.

### Current architecture

The application uses a pragmatic layered WPF structure:

- `App.xaml.cs` creates the .NET host, starts it, initializes the database, opens login, then opens the main shell.
- `Configuration/ServiceCollectionExtensions.cs` registers EF Core, services, ViewModels, and views with dependency injection.
- `Data/` contains the EF Core context, initializer, design-time factory, and migrations.
- `Models/` contains persistence/domain entities.
- `Services/` contains business operations and query services.
- `ViewModels/` adapts services to WPF commands and observable UI state.
- XAML views and limited code-behind provide the UI.

There is no repository abstraction layer. New modules should follow the existing service plus EF Core context pattern unless a real complexity boundary requires otherwise.

## Existing Features

| Feature | Main locations | Database | Status | Tests |
|---|---|---|---|---|
| Authentication and first-run administrator | `Services/AuthenticationService.cs`, `Services/AuthenticationSession.cs`, `LoginWindow.xaml(.cs)` | `Users`, `Roles` | Implemented | `AuthenticationServiceTests.cs` |
| Books | `Services/BookService.cs`, `ViewModels/BooksViewModel.cs`, `BooksView.xaml(.cs)` | `Books`, `AuditLogs` | Implemented | `BookServiceTests.cs` |
| Members | `Services/MemberService.cs`, `ViewModels/MembersViewModel.cs`, `MembersView.xaml(.cs)` | `Members`, `AuditLogs` | Implemented | `MemberServiceTests.cs` |
| Borrowing | `Services/BorrowingService.cs`, `ViewModels/BorrowingViewModel.cs`, `BorrowingView.xaml(.cs)` | `Borrowings`, `Books`, `Members`, `AuditLogs` | Implemented | `BorrowingServiceTests.cs` |
| Returns and fines | `Services/ReturnService.cs`, `ViewModels/ReturnsViewModel.cs`, `ReturnsView.xaml(.cs)` | `Borrowings`, `Fines`, `Reservations`, `AuditLogs` | Implemented | `ReturnServiceTests.cs` |
| Reservations | `Services/ReservationService.cs`, `ViewModels/ReservationsViewModel.cs`, `ReservationsView.xaml(.cs)` | `Reservations`, `Books`, `Members`, `AuditLogs` | Implemented | `ReservationServiceTests.cs` |
| Dashboard | `Services/DashboardService.cs`, `ViewModels/DashboardViewModel.cs`, `DashboardView.xaml(.cs)` | Read-only aggregate queries | Implemented | `DashboardServiceTests.cs` |
| Reports | `Services/ReportService.cs`, `ViewModels/ReportViewModel.cs`, `ReportsView.xaml(.cs)` | Read-only aggregate/list queries | Implemented | `ReportServiceTests.cs` |
| Library settings | `Services/LibrarySettingsService.cs`, `ViewModels/SettingsViewModel.cs`, `SettingsView.xaml(.cs)` | `LibrarySettings`, `AuditLogs` | Implemented | `SettingsBackupAuditTests.cs` |
| Backup, restore, audit viewer | `Services/BackupService.cs`, `Services/AuditService.cs`, settings UI | SQLite file, `AuditLogs` | Implemented with security limitations | `SettingsBackupAuditTests.cs` |
| Shell/navigation/theme/logout | `MainWindow.xaml(.cs)`, `App.xaml(.cs)` | Uses current session/settings | Implemented | No UI automation tests |

## Database Audit

### Context and lifecycle

- EF Core context: `Data/LibraryDbContext.cs`.
- Runtime context creation: `IDbContextFactory<LibraryDbContext>`.
- Runtime database: SQLite at `AppContext.BaseDirectory/library.db`, configured in `ServiceCollectionExtensions.cs`.
- Startup migration application: `Data/DatabaseInitializer.cs`.
- Design-time factory: `Data/LibraryDbContextFactory.cs`.
- Migrations and model snapshot: `Data/Migrations/`.

### Existing entities and tables

The verified EF Core model contains:

- `Role` / `Roles`.
- `User` / `Users`.
- `Book` / `Books`.
- `AuditLog` / `AuditLogs`.
- `Member` / `Members`.
- `Borrowing` / `Borrowings`.
- `Fine` / `Fines`.
- `Reservation` / `Reservations`.
- `LibrarySetting` / `LibrarySettings`.

### Relationships and delete behavior

- `User` belongs to `Role`; foreign key delete is restricted.
- `AuditLog` optionally references `User`; delete is `SET NULL`.
- `Borrowing` references `Book`, `Member`, and `User`; deletes are restricted.
- `Fine` references `Borrowing`; delete is restricted.
- `Reservation` references `Book` and `Member`; deletes are restricted.

### Indexes and constraints

- Unique role name.
- Unique username.
- Unique ISBN.
- Unique member number.
- Composite borrowing index on member and return timestamp.
- Composite reservation index on book, member, cancellation, and fulfillment timestamps.
- Foreign-key indexes generated by EF Core.
- Required/max-length properties are configured in `LibraryDbContext`.
- Monetary fine fields use precision `18,2`.

### Migration history

The source contains these migrations:

1. `20260822183118_InitialCreate`.
2. `20260822184915_AddBooksAndAudit`.
3. `20260822185638_AddMembers`.
4. `20260822185945_AddBorrowings`.
5. `20260822190436_AddFines`.
6. `20260822190830_AddReservations`.
7. `20260822221923_AddLibrarySettings`.

### Seed and database artifact

- `Administrator` and `Librarian` roles are seeded.
- No users, books, members, employees, or payroll data are seeded in the model.
- First-run administrator creation is performed by `AuthenticationService` when the user table is empty.
- A compiled `library.db` exists under Release output and publish output.
- The repository source and migrations establish the schema. The current row contents and applied migration rows of the compiled database were not queried because no SQLite CLI is installed in the environment. This remains an explicit verification item for a future database-specific check.

## Authentication and Authorization

### Authentication verified

- Login is initiated through `LoginWindow`.
- `AuthenticationService` normalizes usernames to lowercase and trims them.
- Passwords are hashed using `PasswordHasher<User>` and are not stored as plaintext.
- Initial password length is at least 12 characters.
- Inactive users cannot authenticate.
- Only the first administrator can be created through the first-run path.
- `AuthenticationSession` stores the current user for the application lifetime.
- Logout clears the session and closes the main window.

### Authorization gap

The current model has a `Role` foreign key and seeded role names, but no granular permission model or enforced authorization service was found. No `Permission`, role-policy, `IsInRole`, or equivalent checks protect service methods and commands.

Consequences:

- Administrator and Librarian currently have no enforced behavioral difference.
- Several read/write services can be called without a session check; borrowing, returns, and reservations do check authentication, but this is not a complete access-control policy.
- Hiding or showing UI controls alone would not satisfy the requested security requirements.
- User management, role assignment, password change, lockout/throttling, MFA, and password reset are absent.

This is a blocking architectural risk for payroll. Phase 2/3 must extend the existing authentication/session path and add service/command authorization before payroll data is exposed.

## Audit Logging and Security Risks

### Existing audit behavior

Audit logging is present for many business writes and is persisted in `AuditLogs`. `AuditService` provides recent entries for the settings view. The current audit model contains action, entity name/id, user reference, details, and timestamp, but does not provide the full old/new value model requested for sensitive payroll changes.

### Risks requiring later remediation

- Backup files are raw SQLite databases and are not encrypted or integrity-signed.
- Restore accepts a selected SQLite file without a documented schema/version validation boundary.
- Some UI error paths display raw exception messages, which can disclose technical or filesystem details.
- No lockout or throttling behavior was found for failed logins.
- The local SQLite database is stored in the application output directory and has no application-level encryption.
- No UI automation, accessibility automation, or visual regression tests exist.
- Source-level localization resources are not established; user-facing text is embedded in XAML/C#.
- `BorrowingService` adds its borrowing audit entry before saving the new borrowing, so its audit `EntityId` should be specifically validated before it is used as a correctness reference.
- The audit query uses a nullable user navigation with the null-forgiving operator; behavior for deleted users should be covered explicitly.

## UI, Navigation, and Dashboard

### Shell

`MainWindow.xaml(.cs)` implements a direct `ContentControl.Content` navigation pattern. It currently exposes:

- Dashboard.
- Books.
- Members.
- Borrowing.
- Returns and fines.
- Reservations.
- Reports.
- Settings.

There is no separate `Frame` navigation stack. New Employees, Payroll, Company, and Users/Permissions views should integrate with this shell pattern unless the navigation architecture is deliberately changed and regression-tested.

### Dashboard

`DashboardService` and `DashboardViewModel` load real SQLite aggregates for:

- Active books.
- Active members.
- Open borrowings.
- Overdue borrowings.
- Outstanding fines.

The shell header currently displays username and role. It does not display an employee full name or position because no Employee entity exists.

### Reports

`ReportService` and `ReportsView` provide library counts, availability, borrowing/overdue counts, outstanding fines, open reservations, and overdue lists. No employee or payroll report/export infrastructure was found.

### UI validation gap

The build and service tests pass, but interactive WPF startup, login, navigation, and visual behavior were not automated in this environment. Manual Windows UI verification remains required for any UI phase.

## Tests and Quality Evidence

Existing test files:

- `AuthenticationServiceTests.cs`.
- `BookServiceTests.cs`.
- `MemberServiceTests.cs`.
- `BorrowingServiceTests.cs`.
- `ReturnServiceTests.cs`.
- `ReservationServiceTests.cs`.
- `DashboardServiceTests.cs`.
- `ReportServiceTests.cs`.
- `DatabaseInitializationTests.cs`.
- `SettingsBackupAuditTests.cs`.
- `FoundationServiceRegistrationTests.cs`.

Current executable results on 2026-08-26:

- Restore: passed.
- Release build: passed.
- Test suite: 21 total, 21 succeeded, 0 failed, 0 skipped.
- Static diagnostics: no source errors were reported during audit inspection.
- UI interactive validation: not available in this headless environment.

The tests are primarily service/database integration-style tests. There are no dedicated tests yet for granular permissions, user-to-employee identity, company configuration, payroll calculations, tax rules, payroll workflow transitions, employee validation, authorization bypass attempts, or interactive UI behavior.

## Missing Components for the Requested Extension

No source implementation was found for:

- Employee, department, position, employment status, documents, or compensation history.
- Employee service, repository, ViewModel, view, validation, or tests.
- Employee-to-user relationship or current employee projection.
- Company profile and configurable company/fiscal information.
- Payroll period, payroll record, earnings, deductions, tax rules, contributions, payslips, payment state, or approval history.
- Payroll calculation interfaces/engines and explainable breakdowns.
- Granular permissions, role-permission relationships, permission checks, or user/role management screens.
- Payroll/employee dashboard metrics and reports/exports.
- Tax configuration and official source/reference metadata.
- Payroll-specific audit old/new values and immutable history rules.
- Migrations, indexes, seed strategy, and test fixtures for the above.

## Conflicts and Design Decisions to Resolve Before Phase 1

1. The requirement that every system user must be an Employee conflicts with the current `User` model, which has no Employee relationship. The relationship direction and required/optional migration strategy must be designed before creating either entity.
2. Existing first-run administrator creation must continue to work while creating the required administrator Employee record.
3. Existing roles are only `Administrator` and `Librarian`; new roles should not be seeded or duplicated until the permission model and operational need are defined.
4. Payroll tax values must not be invented. The initial tax configuration must support unverified rules with source/reference and effective-date metadata, and official validation must be a release gate.
5. Existing `LibrarySetting` is library-configuration data, not a complete company/payroll profile. Reuse it only where semantics match; do not overload unrelated keys.
6. Payroll history needs immutable or append-only treatment after approval/payment, while existing entities generally use ordinary update operations. This requires an explicit workflow and audit design.
7. Local SQLite and raw backup security may be insufficient for production payroll data. The acceptable deployment threat model and encryption/backup requirements must be established.

## Recommended Phase Ordering After Audit Approval

The requested phase ordering is technically sound with one security dependency:

- Phase 1: Employee foundation and validation, designed around the existing EF Core/service/ViewModel conventions.
- Phase 2: Required User-to-Employee identity and current employee information, including first-run administrator compatibility.
- Phase 3: Granular RBAC/permissions enforced in commands and services before sensitive workflows.
- Phase 4: Company profile and configurable fiscal/payroll metadata.
- Phase 5: Payroll persistence and explainable calculation foundation without unverified tax rates.
- Phase 6: Configurable Haiti tax engine with official-source validation status.
- Phase 7: Payroll state transitions, approvals, payments, and audit trail.
- Phase 8: Reports and exports using the existing report service pattern.
- Phase 9: Full regression and startup/login/navigation/database verification.

Each phase must use the existing quality gate: implementation, restore/build, tests, database migration verification where applicable, UI/integration/security checks where applicable, and regression review before proceeding.

## Phase 0 Decision

**Phase 0: PASS.** The global audit is complete, the baseline restore/build/tests pass, and the audit document is now available at `docs/GLOBAL_AUDIT.md`.

**Implementation status:** stopped as required. No Employee, Payroll, Company, RBAC, migration, or application-code implementation was started.

**Next allowed action:** review/approve this audit before Phase 1 begins.
