# Phase 2.0: Company Management Foundation Audit

**Dat:** 2026-08-26
**Scope:** Audit read-only anvan Phase 2 implementation
**Estati:** Audit fini; implementation poko kòmanse

## 1. Sa ki deja egziste

### Solution ak architecture

- Solution: `GestionBibliotheque.slnx`.
- Aplikasyon: WPF `.NET 10`, EF Core, SQLite, DI ak Generic Host.
- Tès: xUnit nan `tests/GestionBibliotheque.Tests`.
- Pattern UI/business: EF Core `DbContext` + service + ViewModel + WPF View/XAML.
- Navigation: `MainWindow` sèvi ak `ContentControl` ak Views ki anrejistre nan DI.
- Startup: `App` aplike migrations atravè `DatabaseInitializer`, ouvri login, epi montre shell la.

### Modèl ak database

`LibraryDbContext` gen `Role`, `User`, `Book`, `AuditLog`, `Member`, `Borrowing`, `Fine`, `Reservation`, `LibrarySetting` ak `Employee`.

Employee aktyèl la gen:

- `EmployeeNumber`, non, kontak ak adrès.
- `HireDate`, `EmploymentStatus`, `IsActive`.
- `JobTitle` ak `Department` kòm `string?` free-text.
- `CreatedAtUtc`, `UpdatedAtUtc` ak koleksyon `Users`.

User la gen `EmployeeId` nullable ak navigation `Employee`. Gen unique index sou `EmployeeId`, foreign key restrictive, ak unique `EmployeeNumber`.

### Authentication, session ak authorization

- `AuthenticationService` jere first-run Administrator, login, password hashing ak account creation.
- `PasswordHasher<User>` itilize; password plaintext pa estoke.
- Login rejte User oswa Employee ki inactive.
- `AuthenticationSession` se sèl session current-user la.
- `CurrentEmployeeService` bay employee id, employee number, non, job title, role ak permissions.
- `AuthorizationService` se sèl service authorization an; li fè diferans ant authentication, role ak permission epi li verifye aksyon yo nan service boundary.
- Permissions aktyèl yo se mapping code-based pou Administrator, Librarian ak Employee. Pa gen `RolePermission` entity oswa role/permission management UI.

### Services ak UI ki ka reuse

- `EmployeeService`: CRUD, search, validation, soft deactivation, account deactivation ak audit.
- `AuthenticationService`: Employee/User linkage ak role assignment pou accounts.
- `AuditService` ak `AuditLog`: baz pou audit administratif.
- `LibrarySettingsService`: settings jeneral bibliyotèk la; li pwoteje pa permissions.
- `MainWindow`, `CurrentEmployeeService` ak existing ViewModels: baz pou shell/header/navigation integration.
- `LibraryDbContext`, migration infrastructure ak `DatabaseInitializer`: baz pou tout nouvo schema changes.

Existing navigation kounye a gen Dashboard, Books, Members, Borrowing, Returns, Reservations, Employees, Reports ak Settings. Pa gen Company, Department, Position, Payroll, Salary, Tax oswa Payslip navigation.

## 2. Sa ki ka reuse

Phase 2 dwe reuse san duplicate:

- Existing `Employee`, `User`, `Role`, `AuthenticationSession`, `AuthorizationService`, `Permissions`, `AuditLog` ak `LibraryDbContext`.
- Existing DI registration nan `ServiceCollectionExtensions`.
- Existing WPF shell, ViewModel/command pattern ak visual language.
- Existing EF migration pattern ak clean/legacy SQLite test fixtures.
- Existing audit write pattern nan services yo.

Payroll pa dwe kreye yon dezyèm Employee, User, authentication, authorization, audit oswa DbContext system.

## 3. Sa ki manke

Pa gen okenn implementation pou:

- `Company` oswa `Organization` entity/table/service/view/test.
- Normalized `Department` entity/table/service/view/test.
- Normalized `Position`/Post entity/table/service/view/test.
- Company profile, legal identity, NIF, address, contact oswa branding data.
- Company/payroll settings, currency, payroll frequency oswa fiscal configuration.
- Employee foreign keys pou Company, Department oswa Position.
- Salary oswa salary-history model.
- Payroll period, payroll entry, earnings, deductions, taxes, contributions oswa payslips.
- Tax/fiscal rules, versions oswa verified Haitian tax sources.
- Company/Department/Position permissions.
- Company/Department/Position navigation and UI.
- Payroll calculations, reports, workflows oswa payroll audit history.

`Department` ak `JobTitle` ki sou Employee pa ranpli kondisyon normalized Department/Position management lan.

## 4. Sa ki bezwen modifye pou Phase 2

Sou baz audit la, Phase 2 implementation ap bezwen, apre plan/dènye apwobasyon:

1. Ajoute yon sèl Company/Organization representation ki respekte single-company scope la, si se sa ki konfime pou aplikasyon an.
2. Ajoute Department ak Position entities avèk lifecycle soft-deactivate, unique constraints ak foreign keys ki konsève istorik.
3. Deside kijan pou migrate existing free-text `Employee.Department` ak `Employee.JobTitle` san pèdi done; mapping ki pa konfime dwe rete unresolved, pa devine.
4. Ajoute Employee foreign keys ak validation pou references active/valid yo.
5. Ajoute permissions Company/Department/Position sèlman nan centralized `Permissions`/`AuthorizationService` ki deja egziste.
6. Ajoute audit events pou company, department, position ak employee organization changes.
7. Ajoute services ak tests anvan UI, epi entegre Views/ViewModels nan shell ki deja egziste.
8. Ajoute EF migrations ki teste sou database clean ak database ki deja gen Phase 1 data.
9. Mete ajou `ARCHITECTURE.md`, `PROJECT_STATUS.md` ak kreye `PHASE_2_STATUS.md` sèlman lè Phase 2 implementation/gate fini.

Pa gen okenn field oswa business rule isit la ki envante rate taks, salary, fiscal threshold oswa lòt règ legal.

## 5. Risk

- **Done istorik:** `Department`/`JobTitle` free-text pa garanti yon mapping otomatik san konfime valè yo.
- **Relasyon:** chanje Employee pou FK normalized ka afekte legacy rows ak migration backfill.
- **Single company scope:** specification la pale de Company/Organization men pa presize si sistèm nan dwe multi-company; sa dwe deside anvan modèl la.
- **Authorization:** permissions yo code-based, pa persisted; sa limite role-management flexibility epi li dwe rete konsistan ak payroll security.
- **Audit:** `AuditLog` pa gen old/new value structure; organization changes ka bezwen details ase pou history, san mete secrets.
- **Employee identity:** tout accounts dwe kontinye ale nan menm `User -> Employee -> Role -> Permissions` chain lan.
- **UI:** entèaktif WPF navigation pa gen automation coverage nan environment headless la.
- **Payroll dependency:** salary, fiscal rules ak tax sources pa ka defini pandan Phase 2 san yo pa kreye business assumptions ki pa verifye.

## 6. Dependencies ak kestyon ki dwe klè

Anvan implementation Phase 2, bagay sa yo bezwen konfime nan specification/product decision:

- Èske aplikasyon an se pou yon sèl Company sèlman oswa plizyè Company?
- Ki valè legal ki obligatwa pou Company: NIF, registration number ak lòt idantifyan?
- Èske `Department` ak `Position` dwe global oswa yo dwe toujou anba yon Company?
- Ki policy pou existing free-text department/job-title values ki pa ka match yon nouvo record?
- Èske yon Position ka deplase ant Departments san kraze Employee/payroll history?
- Ki currency, payroll frequency ak fiscal configuration ki nesesè? Valè legal ki pa verifye dwe rete configurable/unset.
- Ki roles ki dwe resevwa nouvo permissions yo?
- Ki nivo detay audit la mande pou organization changes?

## 7. Plan Phase 2 apre audit

1. Konfime scope single-company/multi-company ak migration policy pou free-text Employee fields.
2. Desine Company profile ak configurable settings ki minimòm nesesè.
3. Implement Department ak tests: create, read, update, deactivate, search, validation, authorization ak historical references.
4. Implement Position ak tests: create, read, update, deactivate, Department relation, validation ak authorization.
5. Integrate Employee foreign keys, preserve historical data, add migration/backfill tests ak audit tests.
6. Integrate Company/Department/Position UI ak permission-aware shell navigation.
7. Run milestone builds/tests/diagnostics/database validation apre chak logical milestone.
8. Run final restore, Release build, full tests, migration checks, authorization checks, diagnostics ak documentation gate.
9. Sèlman si tout Phase 2 gate conditions PASS, make `PHASE 2 = PASS` epi prepare audit Phase 3.

## Konklizyon audit la

Audit la montre architecture aktyèl la ase pou Phase 2 san kreye yon nouvo framework. Men Company, normalized Department ak Position pa egziste; Employee organization fields yo toujou free-text. Payroll, salary, tax ak payslip pa egziste ditou.

**Phase 2.0 Audit = COMPLETE**

**Phase 2 implementation = PA KÒMANSE nan etap sa a.**

**Payroll/Phase 3 = PA KÒMANSE.**
