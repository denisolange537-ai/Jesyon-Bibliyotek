# Phase 3.0: Payroll Audit

**Dat:** 2026-08-26
**Scope:** Audit read-only anvan Payroll implementation
**Estati:** Audit fini; Payroll implementation poko kòmanse

## Sa ki deja pare pou reuse

- `Company` profile ak Company single-organization scope la.
- `Department` normalized entity ki gen Company foreign key.
- `Position` normalized entity ki gen Department foreign key.
- `Employee` existing identity ak optional Department/Position foreign keys.
- `User -> Employee -> Role -> Permissions` authentication chain lan.
- `AuthenticationSession`, `AuthorizationService` ak centralized `Permissions`.
- `AuditLog`/`AuditService` pou administrative events.
- `LibraryDbContext`, EF Core migrations, SQLite ak `DatabaseInitializer`.
- Existing DI, WPF shell, MVVM ViewModel/Command pattern ak existing UI language.
- Existing validation, service-level authorization, soft-deactivation ak migration test patterns.

Depi audit la, Salary foundation ak PayrollPeriod lifecycle te ajoute nan menm architecture a: `SalaryRecord`/`SalaryService` ak `PayrollPeriod`/`PayrollPeriodService`, ak migration/test coverage. Payroll entries, calculations, taxes, payslips ak payroll UI toujou pa fini.

Depi lè sa a, PayrollEntry/Component, calculation engine, versioned TaxRule configuration, finalized-only Payslip service/UI ak Payroll summary report te ajoute. Tax engine la pa gen okenn Haiti rate default; tax rules rete explicitly configurable.

Phase 2 gate pase: restore, Release build, full tests, EF migration model check ak diagnostics te pase. Full suite la te gen 34/34 tests.

## Sa audit la jwenn sou Payroll domain lan

Pa gen implementation pou:

- Salary oswa salary history.
- Payroll period/pay period.
- Payroll entries, earnings, allowances, bonuses oswa overtime.
- Employee deductions, advances oswa loans.
- Employee taxes oswa employer contributions.
- Tax/fiscal rules, thresholds, brackets, ceilings oswa versions.
- Payroll calculation engine.
- Payroll lifecycle, review, approval, finalization, void oswa correction.
- Payslip generation/storage.
- Payroll reports oswa dashboard.
- Payroll permissions.
- Payroll-specific audit history.
- Payroll UI, navigation oswa ViewModels.

Pa gen existing `Salary`, `Payroll`, `Tax`, `Deduction`, `Payslip`, `PayrollPeriod` oswa `PayPeriod` entity/service/table nan code, migrations oswa tests.

## Database ak relasyon

`LibraryDbContext` gen Company, Department, Position ak Employee, men li pa gen okenn payroll table. Employee salary pa egziste; pa gen salary value pou payroll ta dwe overwrite oswa archive. Nenpòt payroll schema dwe ajoute sèlman avèk nouvo EF migration, restrictive foreign keys kote istorik mande sa, ak tests sou clean ak existing migrated database.

Salary history dwe fèt kòm nouvo istorik append-preserving structure si product decision konfime payroll bezwen li. Finalized payroll records pa dwe depann de salary current la pou recalculation.

## Authorization ak security dependencies

Payroll permissions pa egziste. Yo dwe ajoute nan existing `Permissions` ak `AuthorizationService`, pa nan yon dezyèm authorization system. Aksyon ki bezwen policy explicit gen ladan View, Create, Calculate, Recalculate, Review, Approve, Finalize, Void ak ManageRules.

Payroll service yo dwe verifye authorization nan service boundary la. UI visibility pa ase. Employee eligibility ak cross-employee access dwe fèt nan service layer la, epi finalized payroll dwe protected kont modification silansye.

Audit model aktyèl la ka reuse pou events, men payroll decisions bezwen audit details ki pa gen password oswa secrets. Si old/new financial values obligatwa, sa bezwen yon design decision ak migration olye pou itilize text details san limit.

## Tax/fiscal status

Pa gen tax rule oswa source legal verifye nan pwojè a. Pa gen okenn Haiti tax rate, bracket, exemption, ceiling, contribution rate oswa threshold ki ka itilize kòm expected value.

Kontra a egzije:

- Pa hard-code règ fiskal ki pa verifye.
- Sèvi ak configurable/versioned tax rule structure.
- Make rules yo `NotConfigured` oswa `NeedsConfiguration` lè source pa disponib.
- Pa prezante payroll calculation kòm legal/final san rules verifye.

Sa se yon dependency pou tax engine design lan, men li pa mande envante yon rate pou kontinye architecture domain lan.

## Desizyon domain ki rete pou konfime

- Payroll frequency: weekly, biweekly, monthly oswa lòt.
- Salary frequency ak currency.
- Employee eligibility policy pou `EmploymentStatus` values yo.
- Treatment final payment pou terminated/inactive employees.
- Earnings/deduction types ki configurable ak limit yo.
- Payroll lifecycle status names ak approval roles.
- Correction/adjustment policy pou finalized payroll.
- Tax sources ak effective dates pou Haiti.
- Employer contribution applicability.
- Payslip numbering, storage ak access policy.
- Cross-employee payroll visibility policy.
- Old/new financial values ki dwe rete nan audit history.

Pa gen okenn nan desizyon sa yo ki dwe devine nan code oswa test expected values.

## Risk

- **Financial correctness:** san verified tax/fiscal rules, kalkil legal pa ka make final.
- **Historical integrity:** salary changes dwe kenbe history epi pa chanje finalized periods.
- **Sensitive data:** payroll access dwe pi restrenn pase library read access.
- **Lifecycle immutability:** finalized records bezwen protection ak correction mechanism.
- **Migration compatibility:** nouvo payroll FKs dwe itilize existing Employee/Company structure epi pa kreye duplicate identity.
- **Audit completeness:** current AuditLog pa natirèlman structured pou old/new financial snapshots.
- **UI verification:** WPF visual automation pa disponib nan environment headless la.

## Plan Phase 3 apre audit

1. Konfime payroll frequency, currency, eligibility, lifecycle ak verified tax sources.
2. Desine salary history ki refer Employee ak preserve effective dates.
3. Desine PayrollPeriod ak immutable payroll records.
4. Desine configurable earnings/deductions ak explicit source/status fields.
5. Desine versioned tax rules ki ka rete unconfigured san envante rate.
6. Implement deterministic calculation engine separe de UI, avèk tests kantite pa etap.
7. Implement authorization, approval/finalization protection ak audit events.
8. Implement payslip/report queries ki sòti nan finalized data sèlman.
9. Ajoute migrations, clean/existing database tests ak UI integration nan existing shell.
10. Run full Phase 2 + Phase 3 validation gate; si nenpòt restore/build/test/migration/authorization check echwe, STOP.

## Konklizyon

Phase 2 Company Management Foundation disponib epi li se baz ki kòrèk pou Payroll. Audit la konfime Payroll domain lan pa egziste ditou. Sa evite kreye yon dezyèm Employee/User system oswa envante règ fiskal.

**Phase 3.0 Payroll Audit = COMPLETE**

**Payroll implementation = PA KÒMANSE nan etap sa a.**

**Tax/fiscal calculation = BLOCKED jiskaske verified rules/source ak konfigirasyon business yo disponib.**
