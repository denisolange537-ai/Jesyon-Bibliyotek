# Achitekti

## Estrikti aktyèl

- `src/GestionBibliotheque`: aplikasyon WPF prensipal la.
- `tests/GestionBibliotheque.Tests`: tès otomatik yo.

## Apwòch

Aplikasyon an sèvi ak WPF. `App` kreye hôte .NET la, li chaje konfigirasyon `appsettings.json`, li anrejistre sèvis yo, epi li louvri `MainWindow` atravè DI. Logging estanda hôte .NET la disponib depi nan demaraj la.

## Bazdone ak modil biznis

M2 itilize EF Core ak SQLite. `LibraryDbContext` defini `Role` ak `User`, ak yon relasyon User -> Role, index inik sou non role ak username, epi seed pou Administrator ak Librarian. `DatabaseInitializer` aplike migration yo pandan startup. Migration yo nan `src/GestionBibliotheque/Data/Migrations`.

 Modil biznis yo ajoute etap pa etap, san yo pa mete lojik biznis nan code-behind View yo.

## Otantifikasyon

`AuthenticationService` posede règ first-run ak login. Li itilize `PasswordHasher<User>` pou pa janm estoke modpas an klè, normalize username yo, bloke kreyasyon plis pase yon premye administratè, epi rejte itilizatè inaktif. `AuthenticationSession` kenbe itilizatè aktyèl la pandan lavi aplikasyon an; `LoginWindow` rele sèvis la pou setup/login, epi `MainWindow` ka dekonekte session lan.

## Books

`BookService` posede CRUD, rechèch, validation ISBN/kopi/ane, deactivate, ak audit. Chak ekriti liv ansanm ak audit log la fèt nan yon transaction SQLite. `BooksViewModel` adapte sèvis la pou UI a, epi `BooksView` montre DataGrid, rechèch ak fòm detay la. Books rete nan bazdone a apre restart paske se SQLite ki sous verite a.

`MemberService` posede CRUD, rechèch, validation kontak, deactivate ak audit pou manm yo; `MembersViewModel` ak `MembersView` bay koòdone a. Deactivate se soft-delete pou konsève relasyon istorik yo.

`BorrowingService` posede prete a: li verifye session, manm/liv aktif ak kopi disponib, kalkile due date 14 jou, epi nan yon sèl transaction li kreye borrowing, diminye kopi disponib, ak ekri audit. `BorrowingViewModel` ak `BorrowingView` bay workflow prete a.

`ReturnService` posede retou, kalkil fine pa jou, peman, ak fulfillment premye reservation ouvè a nan transaction la. `ReservationService` posede kreye/anile reservation yo epi anpeche duplicate ouvè. `DashboardService` li sèlman query reyèl pou estatistik shell Akèy la; li pa gen valè mock.

`ReportService` li sèlman query reyèl pou endikatè bibliyotèk yo ak lis prete anreta. `ReportViewModel` adapte snapshot la pou `ReportsView`, ki aksesib nan navigation shell la; li pa gen done mock epi li pa chanje schema bazdone a.

`LibrarySettingsService` konsève non bibliyotèk la, kantite jou prete, amann pa jou ak tèm nan tab `LibrarySettings`. `BorrowingService` ak `ReturnService` li paramèt sa yo pou règleman yo rete santralize. `BackupService` sèvi ak SQLite backup API ak koneksyon san pooling; `AuditService` li dènye audit entries yo, epi `SettingsView` bay jesyon paramèt, backup, restore ak audit.

## Employee ak authorization foundation

`Employee` se idantite travayè a; `User` kenbe credentials login yo epi li gen yon lyen `EmployeeId`. First-run administrator la kreye toude nan menm transaction. `AuthenticationService` chaje Employee a pandan login epi refize account oswa Employee ki inaktif. `AuthenticationSession` rete sèl session current-user la.

`AuthorizationService` se sèl sèvis permission foundation lan. Li rezoud permissions dapre role ki deja egziste yo epi services yo verifye permission yo anvan operasyon sansib. `EmployeeService` suiv menm pattern EF Core + service + ViewModel + XAML la; deactivate Employee konsève done istorik epi dezaktive account asosye yo. Migration `20260826215414_AddEmployeesAndUserIdentity` kreye tab Employee a epi backfill users istorik yo.

## Company Management Foundation

Company se yon profil òganizasyon aktif pou aplikasyon an. `Department` fè pati Company, `Position` fè pati Department, epi Employee ka gen optional `DepartmentId`/`PositionId` pou konsève legacy free-text data pandan migration. `CompanyService`, `DepartmentService` ak `PositionService` aplike validation, soft-deactivation, audit ak service-level permissions. Migration `20260826224013_AddCompanyDepartmentsAndPositions` ajoute tables, foreign keys ak unique indexes yo. Payroll ak salary/tax domain pa antre nan architecture la ankò.

## Salary ak Payroll Period Foundation

`SalaryRecord` kenbe salary history Employee a ak type, amount, frequency, currency ak effective dates. `SalaryService` verifye active Employee, anpeche overlap epi bay salary ki aplikab pou yon date. `PayrollPeriod` ak `PayrollPeriodService` bay lifecycle Draft, Calculated, Reviewed, Approved, Paid ak Closed avèk transition permissions ak audit. Pa gen tax rate oswa payroll net calculation ki hard-coded.

`PayrollEntry` snapshot salary/payroll amounts pou yon Employee nan yon PayrollPeriod, pandan `PayrollComponent` kenbe earnings/deductions kòm liy separe. `PayrollCalculationService` se yon engine pure pou gross/deductions/net ak input explicit; li pa aplike tax san configuration. `TaxRule`/`TaxRuleService` kenbe versioned tax configuration, effective dates ak source reference, epi pèmèt yon règ `IsConfigured=false` lè validation legal poko fèt. `PayslipService` ak `PayrollReportService` li sèlman done payroll ki deja nan menm database a; payslip mande entry Paid oswa Closed.
