# PH2-DECISION-01 / PH2-DECISION-03 — Decision Record

Date: 2026-09-05  
Status: Owner decision record; all approvals remain `PENDING`; questionnaire validated for submission (2026-10-30)  
Source: `docs/PH2_DECISION_01_APPROVAL_GATE.md`

## Purpose

This document preserves the PH2-DECISION-03 decision record and the pending approvals shown below. The earlier status wording “Approved decisions recorded” was inconsistent with the approval table, which marks D01-D14 `PENDING`; this status is clarified here without changing the historical decision options or recommendations. No approval authorizes implementation by itself.

## D01 — Deployment Model

**Current state:**
The current application is a WPF/SQLite desktop application. The repository does not establish customer topology, concurrent-user count or supported LAN deployment.

**Options:**

- A — Single-user desktop: simplest support and best fit for current evidence.
- B — Shared Windows workstation: multiple operators on one controlled machine.
- C — LAN/shared SQLite: low infrastructure cost but higher locking/support risk.
- D — Future multi-user architecture: requires future architecture work.

**Technical recommendation:**
Treat current supported deployment as single-machine until a broader topology is approved.

**Main consequence:**
A single-machine decision limits concurrency but avoids promising unsupported LAN/enterprise behavior.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

## D02 — Database Data Directory

**Current state:**
SQLite currently lives at `AppContext.BaseDirectory/library.db`. The path is not configurable.

**Options:**

- A — Keep executable directory: portable, but couples data to binaries and uninstall/upgrade behavior.
- B — `%LocalAppData%`: standard per-user location, tied to Windows profile.
- C — Dedicated application-data directory: separates data from executable and improves ACL/backup control.
- D — User-configurable directory: flexible, but adds path and support complexity.

**Technical recommendation:**
Option C, a dedicated application-data directory separate from the executable. Exact per-user/per-machine placement depends on D01.

**Main consequence:**
A future data move requires migration/upgrade handling and preservation of existing `library.db` data.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

## D03 — Backup Destination

**Current state:**
The user selects a backup destination through the Settings UI. No approved destination policy exists.

**Options:**

- A — Same local disk: convenient but not disaster recovery.
- B — Different local disk: protects against some disk failures.
- C — External drive: portable but exposed to loss/theft.
- D — Network location: operationally useful but depends on permissions/connectivity.
- E — User-configurable destination: flexible but depends on user choice.
- F — Multiple destinations: stronger recovery with more operational complexity.

**Technical recommendation:**
Use a protected local working copy plus a separate recovery copy outside the primary disk failure domain after approval.

**Main consequence:**
A local backup copy is not enough for disk loss, ransomware or machine loss.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / APPROVE E / APPROVE F / DEFER`

**User Approval:** `PENDING`

## D04 — Backup Automation

**Current state:**
Manual backup exists. Scheduled, startup and shutdown automation are not implemented.

**Options:**

- A — Manual only: simplest, operator-dependent.
- B — Scheduled automatic backup: stronger coverage, requires retention/failure handling.
- C — Startup backup: can slow startup and misses crash/power-loss cases.
- D — Shutdown backup: unreliable after crashes or abrupt power loss.
- E — Hybrid manual plus scheduled: strongest practical option with more operations.

**Technical recommendation:**
Option E after destination, retention, failure notification and ownership are approved.

**Main consequence:**
Automation increases recovery coverage but also creates disk-space, scheduling and failure-notification responsibilities.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / APPROVE E / DEFER`

**User Approval:** `PENDING`

## D05 — Retention

**Current state:**
No retention or automatic deletion policy exists. No legal/accounting source is present.

**Options:**

- A — Manual retention only.
- B — Count-based retention.
- C — Age-based retention.
- D — Count plus age with protected/pinned copies.
- E — Defer pending business/legal decision.

**Technical recommendation:**
Option E until business and legal/accounting requirements are available. Technically support protected copies and policy-driven count/age rules later.

**Main consequence:**
No implementation can safely auto-delete backups until deletion authority and retention requirements are known.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

**Classification:** `BUSINESS/POLICY DECISION REQUIRED` and `LEGAL/SOURCE VALIDATION REQUIRED`

## D06 — RPO

**Current state:**
No Recovery Point Objective is defined.

**Options:**

- A — Business-defined target per deployment/data class.
- B — Manual-backup risk accepted.
- C — Defer until customer/operations requirements are gathered.

**Technical recommendation:**
Option C until the owner defines acceptable data loss for library, payroll/employee and audit data.

**Main consequence:**
Backup frequency and automation cannot be selected rationally without an RPO target.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / DEFER`

**User Approval:** `PENDING`

**Classification:** `BUSINESS/POLICY DECISION REQUIRED`

## D07 — RTO

**Current state:**
No Recovery Time Objective or recovery owner is defined.

**Options:**

- A — Business-defined target per deployment.
- B — Best-effort manual recovery.
- C — Defer until support and operations requirements are gathered.

**Technical recommendation:**
Option C until a recovery owner and acceptable downtime are defined.

**Main consequence:**
RTO determines whether manual restore is enough or prepared recovery tooling is required.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / DEFER`

**User Approval:** `PENDING`

**Classification:** `BUSINESS/POLICY DECISION REQUIRED`

## D08 — Database Encryption

**Current state:**
The SQLite database is not application-encrypted. OS-level protection is not established as a deployment contract.

**Options:**

- A — No database-level encryption.
- B — OS/filesystem protection.
- C — Encrypted SQLite provider.
- D — Selected-field protection.

**Technical recommendation:**
Do not select a technology before the threat model, deployment model and key-recovery requirements are approved. Option B is the lowest-complexity baseline; Option C requires provider, migration, licensing and key-management validation.

**Main consequence:**
Copied database protection, deployment complexity and recovery behavior depend on this choice.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

## D09 — Key Management

**Current state:**
No application encryption key exists.

**Options:**

- A — No application key because no database-level encryption is selected.
- B — Windows DPAPI-protected local key.
- C — Certificate Store/key-container design.
- D — Enterprise key-management service.
- E — Operator-managed recovery key.

**Technical recommendation:**
If encryption is approved, require protected key storage, recovery key/escrow, machine/profile replacement handling and key-loss testing. Do not create a key until D08 is approved.

**Main consequence:**
Lost keys can make encrypted database/backups permanently unreadable.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / APPROVE E / DEFER`

**User Approval:** `PENDING`

**Classification:** `TECHNICAL VALIDATION REQUIRED`

## D10 — Backup Encryption

**Current state:**
Backup files are not application-encrypted.

**Options:**

- A — No encryption.
- B — OS-protected backup location.
- C — Application-level encrypted backup.
- D — Enterprise-managed encryption.

**Technical recommendation:**
Encrypt copied/offline backups if they can leave the protected machine, but only after D08/D09 key decisions.

**Main consequence:**
Encryption improves stolen-backup protection but adds restore/key-loss failure modes.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

## D11 — Recovery Ownership

**Current state:**
The repository identifies no organizational recovery owner.

**Options:**

- A — System administrator.
- B — Library administrator.
- C — Business owner.
- D — Designated IT support.
- E — Shared responsibility with named escalation.

**Technical recommendation:**
Option E, only if named roles and escalation are documented.

**Main consequence:**
Without an owner, backups may exist but recovery may not be executed or tested.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / APPROVE E / DEFER`

**User Approval:** `PENDING`

## D12 — Multi-Company/Multi-Tenant

**Current state:**
Current architecture is single-company with no tenant identifier on core records.

**Options:**

- A — Single-company only.
- B — Future multi-company.
- C — Multi-tenant product.
- D — Undecided/deferred.

**Technical recommendation:**
Option D for now, with current implementation explicitly treated as single-company.

**Main consequence:**
A future multi-company decision affects storage isolation, backups, encryption keys, authorization and recovery boundaries.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / DEFER`

**User Approval:** `PENDING`

## D13 — Installer/Deployment Strategy

**Current state:**
Release and self-contained win-x64 executable artifacts exist. No installer, signing, update or uninstall workflow was verified.

**Options:**

- A — Portable executable distribution.
- B — Select an installer technology later.
- C — Managed enterprise deployment.
- D — Defer pending target-customer/deployment requirements.

**Technical recommendation:**
Option D until D01/D02 and distribution requirements are approved. Do not select MSI/MSIX or another technology in this record.

**Main consequence:**
Installer/uninstall/upgrade behavior determines whether database data is preserved safely.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / DEFER`

**User Approval:** `PENDING`

## D14 — Data Privacy Boundary

**Current state:**
The repository contains authentication, employee, member, payroll, tax, audit and company data in SQLite and backups. Technical protection exists partially; privacy, deletion, retention and access obligations are undefined.

**Options:**

- A — Technical controls only until legal review.
- B — Define a customer privacy/data-handling policy.
- C — Obtain jurisdiction-specific legal/privacy review.
- D — Combined B plus C.

**Technical recommendation:**
Option D before privacy-sensitive implementation. No legal obligation or retention period is selected here.

**Main consequence:**
Privacy decisions affect encryption, access review, backup retention, deletion and audit handling.

**Decision required from User:**
`APPROVE A / APPROVE B / APPROVE C / APPROVE D / DEFER`

**User Approval:** `PENDING`

**Classification:** `BUSINESS/POLICY DECISION REQUIRED` and `LEGAL/SOURCE VALIDATION REQUIRED`

## Decision Summary Table

| ID | Decision | Recommended option | User Approval | Dependency |
|---|---|---|---|---|
| D01 | Deployment model | Defer broader topology; treat current support as single-machine | PENDING | Product/operations |
| D02 | Database data directory | C — dedicated application-data directory | PENDING | D01/deployment |
| D03 | Backup destination | Local working copy plus separate recovery copy | PENDING | Operations/storage |
| D04 | Backup automation | E — hybrid manual plus scheduled | PENDING | D03/D05/D11 |
| D05 | Retention | Defer; policy/legal validation required | PENDING | Business/legal |
| D06 | RPO | Defer until business target exists | PENDING | D03/D04 |
| D07 | RTO | Defer until business target/owner exists | PENDING | D11/D13 |
| D08 | Database encryption | Threat-model decision required; no technology selected | PENDING | Privacy/security |
| D09 | Key management | Recovery-capable protected design if encryption selected | PENDING | D08/D11 |
| D10 | Backup encryption | Encrypt copied/offline backups if approved threat model requires | PENDING | D08/D09 |
| D11 | Recovery ownership | E — shared responsibility with named escalation | PENDING | Operations |
| D12 | Multi-company/tenant | D — deferred/undecided | PENDING | Product strategy |
| D13 | Installer/deployment | D — defer until deployment requirements are approved | PENDING | D01/D02 |
| D14 | Data privacy boundary | D — customer policy plus legal/privacy review | PENDING | Legal/policy |

## Implementation Lock

All implementation remains blocked until the required decisions are approved.

Do not start:

- database relocation;
- encryption;
- backup automation;
- retention/deletion;
- recovery implementation;
- installer/deployment packaging;
- multi-company work.

## User Decision Summary

All approvals remain:

**NONE — ALL PENDING USER DECISION**

## Future Decision-to-Implementation Dependencies

- D01/D02 → storage architecture → data movement/migration plan → path/ACL tests → deployment validation.
- D03/D04/D05/D06/D07/D11 → backup operations design → automation/retention/metadata → recovery drill.
- D08/D09/D10 → encryption/key design → provider/package review if needed → encrypted backup/restore tests → key-loss drill.
- D12 → product architecture decision → company/tenant isolation design.
- D14 → privacy/access/retention/redaction requirements → technical controls and documentation.

No dependency step is executed by this record.

## Deferred Decisions

- Exact retention periods and deletion rules.
- RPO/RTO targets.
- Encryption provider and key storage.
- Backup destinations and automation schedule.
- Recovery ownership.
- Installer/signing/update technology.
- Multi-company/multi-tenant direction.
- Privacy/legal boundary.

## Legal/External Dependencies

- Privacy and employment-data rules.
- Payroll/tax/accounting retention requirements.
- Deletion/export/access obligations.
- Encryption/key-custody requirements.
- Customer contractual recovery obligations.

## Environment Dependencies

- Interactive Windows session for ACL, DPI, accessibility and restart validation.
- Clean-machine installation/upgrade/uninstall environment.
- Failure-injection environment for disk full, permission denied, disconnected storage and interrupted operations.
- Off-machine/offline storage for recovery-copy drills.
- Key-recovery environment if encryption is approved.

## Approval Gate

This record is prepared for User/Project Owner response. It does not approve any option and does not authorize implementation.

**PH2-DECISION-02 — DECISION RECORD PREPARED — STOP**

---

## Owner Questionnaire Addendum — PH2 D01 (2026-10-30)

This addendum integrates the owner questionnaire validated for submission. It supplements the historical decision record above; it does not replace or approve any decision. Complete the responses explicitly. Use “Other” wherever the listed options do not capture the intended decision.

### Provisional orientations already communicated — preserve as context

- The accountant has a financial-access need for library data; its exact scope remains to be defined.
- The accountant may prepare, calculate and review payroll under the orientations already communicated.
- A person other than the accountant must approve payroll.
- The accountant’s access to employee data must be limited by field.
- The contemplated pilot is local, on one computer, with no payroll functionality. Client acceptance of this scope remains `UNKNOWN`.
- The provisional priority is to make local/offline operation on one workstation reliable first.
- A centralized client-server architecture for up to three computers on the same local network may be studied later.
- The host computer may also be used as a client workstation.
- Automatic failover to another computer and deferred synchronization of offline transactions are separate subjects requiring their own study and decisions.

**These orientations constitute the context already communicated by the owner. They do not constitute final approval of PH2 D01, a technology choice or authorization to implement.**

### P1 — Accountant’s financial rights

Choose one option or describe another scope:

- **A.** View aggregate amounts and balances only, without member identities.
- **B.** View amounts and transactions associated with members.
- **C.** View transactions and their detailed history.
- **D.** Define rights separately for each report and operation.
- **Other:** specify.

For each operation, indicate `AUTHORIZED`, `NOT AUTHORIZED` or specify a restriction:

1. View outstanding fines.
2. Record a payment.
3. Correct an operation.
4. Cancel an operation.
5. View the named payment history.

P1 defines financial rights and operations. It does not automatically decide whether reports display member identities; answer that separately in P2.

### P2 — Member identities in reports

Choose one option or describe another rule:

- **A.** Aggregate reports only, without member identities.
- **B.** Named reports are accessible to the accountant.
- **C.** Access differs by report type.
- **Other:** specify the reports and named information allowed.

P2 is independent of P1. Current technical evidence reports that `Reports.View` covers both aggregate data and a named list of overdue members. No separate report permission is implied by the options above.

### P3 — Employee profile fields

For each category, answer `AUTHORIZED`, `NOT AUTHORIZED`, or specify the exact restriction:

- Identity and employee number.
- Phone, address and email.
- Job title, department and employment status.
- Hire date.
- Associated user account, username and role.

P3 concerns employee profile and account fields. Individual salaries, salary components, tax data and payroll amounts are excluded from P3 and are addressed in P4.

### P4 — Salary, tax and payroll data

For each item, indicate whether the accountant may consult it, and specify any field-level limits:

- Individual salaries and salary history.
- Salary components.
- Tax rules, rates, thresholds and source references.
- Aggregate totals by payroll period.
- Individually calculated amounts.
- Payslips.
- Payment and closure information after processing.

Separately confirm whether the accountant may consult applicable tax rules through `Payroll.ViewRules`, regardless of the permission to calculate. Existing technical findings state that some calculation paths may depend on this permission. `Payroll.Calculate` does not automatically imply every consultation right listed above.

### P5 — Payroll preparation, calculation and review

The orientations already communicated are that the accountant may prepare, calculate and review payroll. The owner is **not** being asked to reconfirm the calculation permission. Choose the required separation between people:

- **A.** The same person may prepare, calculate and review.
- **B.** The accountant prepares and calculates; another person reviews.
- **C.** An independent review is required after preparation and calculation.
- **Other:** specify responsibilities and which people must be distinct.

The existing permissions distinguish preparation, calculation and review, but the reported code does not guarantee different operators.

### P6 — Payroll approval, payment and closure

The already communicated requirement that someone other than the accountant approves payroll is preserved and is not being reopened. Specify:

1. Which organizational role or responsible person should approve?
2. May the approver also confirm payment?
3. May the person who pays also close the period?
4. Which operations must be performed by different people?
5. **Other:** specify.

An application role such as `Administrator` does not by itself identify the business owner responsible for approval. The technical permissions are distinct, but the existing code does not establish identity separation between approver, payer and closer.

### P7 — Client acceptance of the pilot scope

Select one status: `YES`, `NO` or `UNKNOWN`.

The response must address this specific scope:

- One computer.
- Local operation.
- No payroll in the pilot.
- Agreed workflows and acceptance criteria.

Identify available evidence, such as a signed agreement, written scope approval or approved acceptance criteria. Do not infer client acceptance from internal project documents. No evidence found in this repository would not establish that the client refused or that no external document exists.

### Questionnaire status and governance

- PH2 D01: `PENDING`.
- Owner questionnaire: `VALIDATED FOR SUBMISSION TO THE OWNER`.
- Technology decision: `NOT MADE`.
- Final D01 approval: `NOT GRANTED`.
- Implementation authorization: `NOT GRANTED`.
- Client confirmation: `UNKNOWN` unless documentary evidence is provided and reviewed.

This questionnaire records questions for the owner; it does not answer P1-P7 on the owner’s behalf, approve a technology or authorize implementation.

**PH2 D01 — QUESTIONNAIRE READY FOR OWNER SUBMISSION — STOP**
