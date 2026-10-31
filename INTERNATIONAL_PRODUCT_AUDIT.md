# International Product Audit

Date: 2026-09-05
Scope: read-only audit of source, XAML, EF Core model/migrations, services, ViewModels, tests, configuration and documentation. `bin/` and `obj/` were excluded from code conclusions. No application code, database schema, package, or migration was modified.

## 1. Executive Summary

The application is a functioning Windows WPF library-management system with SQLite persistence, EF Core migrations, dependency injection, authentication, service-layer authorization, employee/company foundations, payroll lifecycle foundations, reporting, audit records, and backup/restore.

It is **NOT READY** for commercial or international sale. The main blockers are payroll input integrity, incomplete RBAC administration and authentication lifecycle controls, unsafe backup/restore hardening, incomplete tax-engine semantics, absence of export, lack of localization, and insufficient privacy/deployment/commercial controls. Passing tests do not remove these blockers.

## 2. Verified Gates

| Check | Result | Evidence |
|---|---|---|
| `dotnet --info` | PASS | SDK 10.0.302; runtime 10.0.10; WindowsDesktop runtime 10.0.10; x64 Windows |
| `dotnet restore GestionBibliotheque.slnx` | PASS | Restore completed; 3 projects |
| `dotnet build GestionBibliotheque.slnx -c Release --no-restore` | PASS | Application and test project built without reported warnings |
| `dotnet test GestionBibliotheque.slnx -c Release --no-build` | PASS | 52 passed, 0 failed, 0 skipped |
| EF pending model changes | PASS | `No changes have been made to the model since the last migration.` |
| NuGet vulnerable package scan | UNVERIFIED | The command completed restore/build output but did not provide an advisory result in the captured output |
| Current publish command | UNVERIFIED | A later terminal session lacked `dotnet` in PATH; historical docs report self-contained win-x64 publish, but this audit does not mark a new publish PASS |
| Git diff/status | UNVERIFIED | `git` is not installed/available in the audit terminal |

## 3. Current Inventory

- Windows WPF executable, target `net10.0-windows`, nullable enabled.
- EF Core 10 SQLite data access with migrations and startup initialization.
- Models include users, roles, books, members, borrowing, fines, reservations, audit logs, settings, employees, company/departments/positions, salary records, payroll periods/entries/components and tax rules.
- Services and ViewModels are present for the listed domains.
- UI strings are primarily embedded in XAML/C# in Haitian Creole; no resource localization layer was found.
- Existing tests cover 20 test source files and the executed suite reported 52 tests.
- No export implementation for PDF, XLSX, CSV or printing was found.

## 4. Classification Summary

| Domain | Classification | Priority |
|---|---|---|
| WPF/DI/service separation | PARTIAL | Medium |
| Authentication basics | IMPLEMENTED | Medium |
| Authentication hardening | MISSING | High |
| Service-level authorization | IMPLEMENTED/PARTIAL | High |
| Configurable RBAC | MISSING | High |
| Employee/company lifecycle | IMPLEMENTED/PARTIAL | Medium |
| Payroll lifecycle | IMPLEMENTED/PARTIAL | Critical/High |
| Tax compliance | PARTIAL/NOT LEGALLY VERIFIED | High |
| EF schema/migrations | IMPLEMENTED/PARTIAL | Medium |
| Reports | PARTIAL | Medium |
| Export/printing | MISSING | Medium |
| Backup/restore | PARTIAL | High |
| Internationalization | MISSING/PARTIAL | High |
| Accessibility/UI automation | UNVERIFIED/PARTIAL | Medium |
| Privacy/commercial operations | MISSING | High |
| Deployment/installer/update | PARTIAL/UNVERIFIED | High |

## 5. Architecture Audit

**Evidence:** `Configuration/ServiceCollectionExtensions.cs`, `Data/LibraryDbContext.cs`, `Services/`, `ViewModels/`, `App.xaml.cs`.

**Implemented:** DI, host startup, EF context factory, service layer, ViewModels, and migrations are established. Most business operations are outside code-behind.

**Partial:** authorization, validation, audit and transaction boundaries are not uniformly centralized. Some UI handlers display exception text directly. The service graph uses optional dependencies and creates fallback `AuthorizationService` instances, which makes policy composition harder to govern. There is no explicit domain layer or repository abstraction; this can be acceptable for a small desktop product but should be documented and tested as a deliberate architecture.

**Risk:** accidental bypass, inconsistent policy, difficult future multi-tenant/service deployment.

## 6. Database Audit

**Implemented:** unique indexes exist for usernames, roles, employee numbers, ISBNs, member numbers, payroll period/employee pairs and tax-rule versions. Monetary precision is configured. Foreign keys mostly restrict deletion; payroll components cascade from entries. Migrations exist and EF reports no pending model changes.

**Partial:** SQLite is reasonable for single-user and small organizations, conditionally suitable for small controlled LAN use, and not a strong default for enterprise multi-user deployment. Business constraints such as valid status values, non-negative values, tax responsibility, overlap rules and concurrency are mostly service-level only. Created/updated timestamps and optimistic concurrency are not a complete cross-entity standard.

**Risk:** direct database edits or concurrent writes can violate invariants. SQLite file replacement and backup behavior need operational controls.

## 7. Security and Authorization Audit

Password hashing, minimum 12-character validation, normalized usernames, active user/employee checks and logout exist in `Services/AuthenticationService.cs` and `AuthenticationSession.cs`.

**HIGH:** no account lockout, failed-login counter, throttling, password change/reset flow, MFA, or security-event audit policy.

**HIGH:** `Services/AuthorizationService.cs` maps permissions in memory by role name. The `Role` entity is persisted, but permissions are not persisted/configurable and no role-management UI exists. UI visibility checks are useful but cannot be the security boundary; service checks are present for many operations, but the complete permission/action matrix is not exhaustively tested.

**HIGH:** sensitive payroll and employee data are local SQLite data without encryption-at-rest or a documented OS-protection requirement. Backups inherit this exposure.

See `SECURITY_AUDIT.md` for threat scenarios and required tests.

## 8. Employee and Company Audit

**Implemented:** employee CRUD/deactivation, employee number, contacts, employment status, account linkage, company profile, departments, positions, salary history, service permissions and audit calls exist.

**Partial:** employee/user/role separation is structurally present, but user administration is incomplete. There is no full role editor, password lifecycle, access review, or termination/offboarding workflow with recovery and audit guarantees. Company is single-profile only; branch and tenant boundaries are not implemented.

## 9. Payroll Audit

The lifecycle `Draft -> Calculated -> Reviewed -> Approved -> Paid -> Closed` exists and save operations reject `Paid`/`Closed` entries. Unique period/employee entries exist and payslips require finalized states.

**CRITICAL/HIGH BUG:** `PayrollEntryService.SaveCalculatedAsync` accepts caller-supplied base, gross, taxable, tax, deductions, employer contribution and net values. It checks arithmetic for net but does not recompute values from salary/rules or verify component totals. A privileged caller or future UI defect can persist inconsistent payroll data.

**HIGH:** lifecycle updates and audit inserts are not consistently wrapped in one explicit transaction.

**MEDIUM:** calculation audit text always says `TaxConfiguration=NotConfigured`, even when a configured rule may have been applied.

**Recommendation:** make calculated values server-derived, validate all components, use explicit transaction boundaries, add concurrency/closing tests, and make the audit record accurate.

## 10. Tax Audit

`TaxRule` supports country, rate, threshold, effective dates, source reference, version and configured state. The implementation does not add a Haiti rate and does not provide legal proof; Haiti rules are therefore **NOT LEGALLY VERIFIED**.

The current calculation supports a single rate and threshold. It does not model brackets, exemptions, statutory social contributions, employer contributions by rule, deductions, dependents, tax filing periods, or rule-specific formulas. A missing/unconfigured rule results in zero employee tax, which may be acceptable only as an explicit controlled draft policy, not as a production payroll default.

No legal/tax rate is asserted by this audit.

## 11. UI/UX and Accessibility

WPF views, navigation, theme switching, keyboard shortcuts, login Enter behavior and several accessible names exist. Interactive WPF behavior, DPI scaling, screen-reader behavior, focus order, contrast validation and full keyboard workflows were not executable in this environment and remain UNVERIFIED. UI text and raw exception messages are inconsistent with a polished international product.

## 12. Reporting and Export

Library, borrowing/overdue, dashboard, payroll summary and payslip display services exist. Filtering/export requirements are incomplete. PDF, Excel/XLSX, CSV and printing were not found and are classified MISSING.

## 13. Backup, Restore and Continuity

SQLite backup API, permission checks, confirmation UI and persistence tests exist. However, raw backup files are not encrypted or signed; restore does not validate schema/version/integrity before replacing the active database; there is no retention, automatic backup, corruption detection, rollback staging or tested disaster-recovery runbook. This is HIGH risk for payroll and personal data.

## 14. Performance and Observability

Async EF APIs, cancellation and `AsNoTracking` are used. Lists are generally unpaginated, some searches transform database columns, and reports issue several aggregate queries. Logging exists through the host and ViewModels, but there is no production log policy, central diagnostics, metrics, correlation IDs or demonstrated secret-redaction test. Several UI paths expose exception messages.

## 15. Documentation, Privacy and Commercial Readiness

Existing architecture/status documents describe milestones but contain historical counts and statements that conflict with current code. They are not a substitute for release documentation. Missing or insufficient areas include installer/update/uninstall, data locations, signing, license/third-party notices, privacy/retention, support, threat model, restore runbook, operational diagnostics, first-run provisioning, role administration and customer configuration.

## 16. Product Readiness Score

| Area | Score |
|---|---:|
| Architecture | 6/10 |
| Security | 4/10 |
| Database | 6/10 |
| Authorization | 4/10 |
| Employee | 7/10 |
| Payroll | 5/10 |
| Tax | 3/10 |
| UI/UX | 5/10 |
| Accessibility | 4/10 |
| Internationalization | 2/10 |
| Reporting | 5/10 |
| Export | 1/10 |
| Backup | 3/10 |
| Testing | 5/10 |
| Performance | 5/10 |
| Deployment | 4/10 |
| Documentation | 4/10 |
| Commercial readiness | 3/10 |
| **Total** | **76/180 (42.2%)** |

Classification: **Major Rework Required**. One or more critical payroll/security/integrity blockers independently prevent production readiness.

## 17. Commercial Readiness Decision

**NOT READY.** This is not a statement that the current product is nonfunctional. It means the required security, payroll integrity, legal-tax, backup, internationalization and operational gates have not all passed.

## 18. Exact Next Phase

Do not start broad feature work. Start Phase 1 hardening from `COMMERCIAL_READINESS_ROADMAP.md`: payroll integrity and transaction safety, security/RBAC lifecycle, and backup/restore safety. Each slice must have implementation, focused tests, security/regression tests, migration check, diagnostics and documentation before the next slice begins.
