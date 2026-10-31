# Product Gaps

Date: 2026-09-05
Classification uses IMPLEMENTED, PARTIAL, MISSING, BUG, UNVERIFIED and NOT_APPLICABLE. Evidence is based on current source/tests; historical status documents are not treated as proof by themselves.

## Critical and High Gaps

| ID | Status | Severity | Gap | Evidence / affected code | Risk | Priority |
|---|---|---|---|---|---|---|
| G-001 | PARTIAL | High | `SaveCalculatedAsync` rejects caller totals; recalculation/lifecycle transactions and `PayrollPeriod.Version` concurrency protection are implemented, while controlled correction remains undefined | `Services/PayrollEntryService.cs`, `Services/PayrollPeriodService.cs`, `Models/PayrollPeriod.cs` | Payroll financial integrity | P0 |
| G-002 | IMPLEMENTED | High | PayrollPeriod lifecycle transitions and audit writes are transactionally protected with optimistic concurrency checks | `Services/PayrollPeriodService.cs`, `Models/PayrollPeriod.cs` | State/audit divergence | P0 |
| G-003 | PARTIAL | High | Backup/restore now validates SQLite integrity/schema/migrations, stages files, creates a safety backup and attempts rollback; encryption, retention and automated recovery policy remain undefined | `Services/BackupService.cs` | Data loss/disclosure | P0 |
| G-004 | MISSING | High | Lockout, throttling, password reset/change, MFA and session timeout | `Services/AuthenticationService.cs` | Account compromise | P0 |
| G-005 | MISSING | High | Persisted configurable role-permission model and role administration | `Services/AuthorizationService.cs` | Least-privilege failure | P0 |
| G-006 | PARTIAL | High | Tax engine supports one rate/threshold and no legal Haiti rules; configuration validation and authorization tests pass, while overlap policy and legal source verification remain dependencies | `Services/PayrollEntryService.cs`, `TaxRuleService.cs`, `tests/GestionBibliotheque.Tests/TaxRuleServiceTests.cs` | Incorrect payroll/legal exposure | P0 |
| G-007 | MISSING | High | Local data and backups have no documented encryption/privacy controls | Models/config/backup paths | Personal data exposure | P1 |

## Medium Gaps

| ID | Status | Severity | Gap | Risk | Priority |
|---|---|---|---|---|---|
| G-008 | PARTIAL | Medium | Recalculation audit now records configured/not-configured tax state; other payroll audit detail remains limited | Audit accuracy/completeness | P1 |
| G-009 | PARTIAL | Medium | Audit has no old/new values, reason, correlation ID or immutable archive policy | Weak investigations | P1 |
| G-010 | MISSING | Medium | PDF, XLSX, CSV and printing | Customer workflow incomplete | P1 |
| G-011 | MISSING | Medium | Resource localization, Haitian Creole/French/English language switching and locale abstraction | International sale blocked | P1 |
| G-012 | PARTIAL | Medium | Reports lack a complete filter/export contract and may be unpaginated | Scale and usability | P2 |
| G-013 | PARTIAL | Medium | Raw exception messages shown in some UI dialogs | Information disclosure/poor UX | P1 |
| G-014 | UNVERIFIED | Medium | Keyboard-only, focus, screen reader, contrast, DPI and restart-after-restore behavior | Accessibility/regression risk | P1 |
| G-015 | PARTIAL | Medium | SQLite is not a demonstrated enterprise/multi-user LAN solution | Deployment constraints | P1 |
| G-016 | IMPLEMENTED | Medium | `PayrollPeriod.Version` optimistic concurrency token protects payroll period mutations; stress/multi-process coverage remains limited | Residual concurrency risk | P1 |

## Low and Informational Gaps

| ID | Status | Severity | Gap | Priority |
|---|---|---|---|---|
| G-017 | MISSING | Low | Installer, signing, update/uninstall and upgrade migration path | P2 |
| G-018 | MISSING | Low | License/third-party attribution and privacy documentation | P1 for sale |
| G-019 | MISSING | Low | Support information, diagnostics screen and first-run commercial provisioning | P2 |
| G-020 | PARTIAL | Info | Architecture is single-company; multi-branch/multi-tenant roadmap only | P2 |
| G-021 | PARTIAL | Info | Some historical status documents still contain obsolete milestone/test counts | P1 documentation cleanup |

## Existing Capabilities

Authentication basics, EF migrations, SQLite persistence, books/members/borrowing/returns/reservations, employee/company foundations, salary history, payroll status lifecycle, tax-rule data structure, reports, audit viewer, settings, backup/restore foundation, DI, logging and 68 passing automated tests are present. These capabilities should be hardened rather than reimplemented.

## Not Applicable at Current Desktop Scope

A web/API threat boundary and cloud multi-tenant infrastructure are not required to sell a single-machine desktop product. They become applicable if LAN/server/cloud deployment is promised. The roadmap should preserve this boundary explicitly.
