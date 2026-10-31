# Commercial Readiness Roadmap

> **Sous plan travay aktyèl:** [`planfinal.md`](planfinal.md) rasanble tout 8 etap, task ledger ak status aktyèl. Swiv dokiman sa a pou travay; roadmap sa a rete sous kondisyon komèsyal C0–C4 ak istorik faz yo.

Date: 2026-10-03

## Development contract update — 2026-10-03

This section updates the implementation scope and release gates using [`COMMERCIAL_SALE_AND_PILOT_PLAN.md`](COMMERCIAL_SALE_AND_PILOT_PLAN.md) as the current product boundary. It supersedes the older “Current Exact Next Step” and the payroll pilot acceptance item below where they conflict. It does not claim that any gate has passed or authorize a general release.

The complete 8-stage sequence, task-by-task workflow, acceptance gates, current status and task ledger are in [`planfinal.md`](planfinal.md). Follow one task at a time through **AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP**. Respect its documented owner exception and do not treat an incomplete commercial gate as passed.

### Product boundary

- **Candidate label:** `0.9.0-pilot`, provisional only. Before a pilot build, record it in product/build metadata and attach a build ID, date, artifact hash and release notes.
- **Supported pilot topology:** Windows x64 desktop, local SQLite, one library and one computer. Do not promise LAN/shared-database, cloud, multiple branches/companies, installer lifecycle, or service levels without separate design and verification.
- **Included workflows:** books, members, borrowing, returns/fines, reservations, library dashboard/basic reports, library settings, manual backup/restore and audit viewer, subject to the gates below.
- **Explicitly excluded:** payroll, payslips and tax/legal-compliance claims. Keep payroll/tax out of the pilot contract and customer acceptance criteria. No jurisdictional rules may be enabled or marketed as legally valid until a qualified local tax/accounting specialist reviews the authoritative source and approves tested rule fixtures in writing.
- **Evidence rule:** historical build/test/publish results are useful context, not current release evidence. The latest recorded baseline is 68/68 tests, but the 2026-10-03 test reruns produced no output and were stopped. Re-establish current evidence before pilot distribution.

### Contracted work sequence and acceptance gates

#### C0 — Re-establish the release baseline

Run restore, Release build, complete automated tests, EF pending-model check, diagnostics, and win-x64 publish for the exact candidate. Investigate and record any command that hangs or fails; do not treat a timeout as a pass.

**Accept when:** all commands have captured exit codes/output; tests pass; no pending model changes; the artifact opens on a clean supported Windows x64 machine; build ID/hash and release notes are recorded. If blocked, report the exact failing command and stop release progression.

#### C1 — Pilot deployment and data/recovery contract

Document the supported one-computer setup, actual database/log/backup locations, customer operator responsibilities, access assumptions, backup frequency chosen with the pilot customer, restore steps, and known data-protection limitations. Confirm the customer approves the categories of data used and access/retention/deletion responsibilities. Run a recovery exercise including a valid backup, invalid/interrupted backup, restore verification, and restart after restore.

**Accept when:** an operator can follow the written guide; required library records survive recovery and are checked; failure/rollback behavior is recorded; the customer and product owner sign the pilot data/recovery responsibilities. Do not load real member data until this gate is accepted. No legal retention period or recovery SLA is invented.

#### C2 — Security, access and interactive library workflows

Review named pilot accounts, administrator access and service-boundary permissions. Document unavailable lockout, password recovery/change and MFA controls, and restrict the pilot to a controlled Windows machine/user environment. Manually verify login/logout, books, members, borrowing, return/fines, reservations, reports/settings, backup/restore and post-restore restart in a real Windows session. Record keyboard/focus and supported display-scaling issues.

**Accept when:** all in-scope critical workflows complete without unresolved data-loss or access-control defects; pilot users and roles are approved; known limitations have written owner/customer acceptance; critical defects are zero. A failure blocks the pilot.

#### C3 — Signed pilot agreement and controlled pilot

Limit the pilot to one library, one computer, named operators, no payroll/tax, and no shared network database. Agree duration, support contact/hours, workflow scope, success measures, stop conditions, issue reporting, data return/deletion at exit, and customer sign-off. Train operators and maintain an issue log.

**Accept when:** signed pilot agreement and training record exist; all agreed workflows meet customer acceptance criteria; there are zero open critical issues; high risks are fixed or explicitly accepted by the responsible owner; recovery evidence and final customer acceptance are recorded. Passing C3 permits a decision on the next release; it does not automatically approve general sale.

#### C4 — General-sale readiness (separate release decision)

Complete the sale gates in `COMMERCIAL_SALE_AND_PILOT_PLAN.md`: account lifecycle and role administration, privacy/data protection and retention policy, verified deployment/install/upgrade/uninstall lifecycle, licensing/third-party notices, support/operations documentation, required report/export and localization for the chosen market, accessibility/UI/recovery/security regression, and current artifact-specific build/test evidence. Any wider topology needs its own architecture and support contract.

**Accept when:** no open critical finding remains; high findings are closed or formally accepted; clean-machine installation/update/rollback/uninstall and data preservation are evidenced; privacy/support/legal obligations for the selected market have qualified review; the product owner records a separate general-sale approval. Payroll/tax remains excluded until its own jurisdiction-specific legal and technical acceptance gate passes.

### Workstreams retained for product hardening

Phases 1–7 below remain engineering workstreams, but their priority is governed by the C0–C4 gates above and by the bounded library-only pilot scope. Payroll/tax work is not a dependency for the library-only pilot while it is disabled/excluded; it remains a hard blocker for selling or claiming payroll/tax capability.

This roadmap follows the required gate order: AUDIT -> FINDINGS -> PRIORITY -> PLAN -> IMPLEMENTATION. No implementation was started during the audit.

## Phase 1: Payroll and Data Integrity Hardening (P0)

**Objective:** prevent inconsistent payroll data and guarantee auditable state transitions.

**Files/surfaces:** `Services/PayrollEntryService.cs`, `Services/PayrollCalculationService.cs`, `Services/PayrollPeriodService.cs`, `Data/LibraryDbContext.cs`, payroll models and tests.

**Work:** derive monetary values from salary/components/rules; validate component reconciliation; reject caller-supplied mismatches; use explicit transactions for state plus audit; add concurrency protection; correct tax-configuration audit detail; prove Closed is immutable.

**Gate:** Release build, focused payroll tests, negative authorization/integrity tests, concurrency test, migration check, diagnostics and documentation. Stop if any gate fails.

## Phase 2: Authentication and Configurable Authorization (P0)

**Objective:** provide defensible least privilege and account lifecycle controls.

**Work:** persisted permissions and role assignments, role-management UI/service, lockout/throttling, password change/reset, session timeout, reauthentication for sensitive actions, administrator self-lockout protection, complete permission matrix tests and security event audit.

**Gate:** all role/action tests pass; direct service calls are denied correctly; no privilege escalation; build/test/migration/security regression pass.

## Phase 3: Backup, Restore and Privacy Controls (P0/P1)

**Objective:** make recovery safe and protect employee/payroll data.

**Work:** encrypted/signed backups, protected directories, schema/version/integrity validation, staging restore, pre-restore snapshot, rollback, retention, verification, recovery-point/time documentation, privacy and deletion/retention policy.

**Gate:** valid, corrupt, incompatible, tampered and interrupted restore tests; recovery drill; audit verification; documentation review.

## Phase 4: Tax Rules and Payroll Compliance Contract (P0)

**Objective:** make payroll rule behavior explicit without inventing legal rates.

**Work:** define rule engine contract for country, brackets, thresholds, exemptions, employee/employer responsibilities, deductions, effective dates and source references. Obtain legal/accounting review per country. Keep unverified Haiti rules disabled and fail safely according to an approved business policy.

**Gate:** rule fixtures reviewed by qualified domain owner; no undocumented rate; deterministic calculation tests and audit trace.

## Phase 5: Internationalization and Commercial Reporting (P1)

**Objective:** support target markets without hard-coded Haitian assumptions.

**Work:** resource files and language selection for Haitian Creole, French and English; culture-aware dates/numbers; currency and timezone configuration; pluralization; PDF/XLSX/CSV/print export with company metadata and confidentiality markings; report filters.

**Gate:** locale snapshot tests, currency/date tests, export format tests, accessibility review and regression pass.

## Phase 6: UI, Accessibility and Operational UX (P1)

**Objective:** make repeated workflows dependable for customers.

**Work:** complete keyboard/focus/accessibility review, DPI/scaling tests, loading/empty/error/success states, friendly error mapping, diagnostics screen, notifications and safe confirmation flows.

**Gate:** Windows interactive test matrix at supported scaling levels, UI automation where feasible, accessibility checklist and regression pass.

## Phase 7: Deployment and Commercial Packaging (P1/P2)

**Objective:** provide a supportable Windows product lifecycle.

**Work:** choose MSI/MSIX/installer approach; code signing; versioning; install/uninstall; data/log/backup locations; upgrade and migration recovery; support/about/license/third-party notices; privacy and operator guides.

**Gate:** clean-machine install, upgrade, rollback, uninstall, signed artifact verification, release notes and support runbook.

## Phase 8: Pilot Readiness and Review (P1)

**Objective:** validate only the contracted library workflows with a controlled customer pilot. Payroll acceptance is excluded from this pilot.

**Work:** threat-model review, security/access review, library backup recovery exercise, interactive library workflow validation, performance baseline, customer training and issue triage. Payroll/legal-tax review is a separate prerequisite only if payroll/tax is later added to the sale scope.

**Gate:** zero open critical findings; all high findings accepted or closed; documented deployment and recovery evidence; signed pilot acceptance.

## Rules for Every Phase

1. Define objective, files, tests and evidence before editing.
2. Implement the smallest focused change.
3. Build and run focused tests immediately.
4. Run security, regression and migration checks.
5. Update documentation and diagnostics.
6. Stop immediately when a gate fails; do not begin the next phase.

## Current Exact Next Step

Execute C0 to establish current build/test/publish evidence, then C1 and C2 before any customer data or pilot use. C3 requires signed pilot terms. Do not start or advertise general sale before C4. Keep payroll/tax disabled or contractually excluded until jurisdiction-specific expert validation and a separate acceptance gate.
