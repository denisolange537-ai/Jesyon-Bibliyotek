# PH2-DECISION-01 — Business & Architecture Decision Gate

Date: 2026-09-05  
Status: Approval gate prepared; no implementation authorized

## 1. Executive Summary

The repository is a Windows WPF desktop application using .NET 10, EF Core 10 and SQLite. The runtime database currently lives at `AppContext.BaseDirectory/library.db`. Startup applies EF migrations. Backup and restore have integrity/schema/migration validation, staging, pre-restore safety backup, authorization and audit events. Database encryption, retention automation, durable disaster-recovery copies, installer lifecycle and approved RPO/RTO targets are not defined or implemented.

This document presents technical recommendations and records that no business owner approval, legal source or deployment decision is currently present in repository evidence. It intentionally does not approve decisions on behalf of the project owner and does not authorize implementation.

## 2. Current Verified State

- Target framework: `net10.0-windows`; WPF enabled; nullable reference types enabled.
- Provider: SQLite through EF Core 10.
- Runtime path: `AppContext.BaseDirectory/library.db`.
- Startup: `Database.MigrateAsync()`.
- Database location is not currently configurable.
- No explicit WAL/journal configuration exists in application code.
- Backup uses SQLite `BackupDatabase`, temporary files, integrity/schema/migration checks and validated replacement.
- Restore validates source, creates a safety backup, stages the restore, validates the active result and attempts rollback on failure.
- Backup/restore requires `SettingsManage` and writes success audit events.
- Database-level encryption is not implemented or selected.
- Backup encryption, retention automation, durable off-machine recovery and installer lifecycle are not implemented.
- Current application architecture is single-company; no tenant identifier or multi-tenant boundary was verified.
- Full regression baseline is 68/68 PASS; this decision task does not rerun or modify tests.

## 3. Decision Principles

1. Do not invent legal, tax, privacy, accounting, retention, RPO or RTO requirements.
2. Separate technical recommendation from business approval.
3. Separate backup copy from disaster-recovery copy.
4. Treat employee, payroll, tax, authentication and member data as sensitive assets based on actual repository fields.
5. Preserve existing data and migration history during future implementation.
6. Prefer reversible, testable and supportable decisions.
7. Do not implement any decision before explicit approval and an implementation task.

## 4. D01 Deployment Model

### Current state

The repository does not establish the customer topology or number of concurrent users. SQLite and an executable-local database are consistent with a single-machine desktop deployment, but LAN/shared-database behavior is not a verified commercial target.

### Options

| Option | Advantages | Risks/implications |
|---|---|---|
| A. Single-user desktop | Simplest support and SQLite fit | Limited concurrent use and sharing |
| B. Shared Windows workstation | One controlled machine can serve multiple operators | Requires Windows account/data ACL and session policy |
| C. LAN/shared SQLite | Low infrastructure cost | Locking, corruption, backup and support complexity |
| D. Future multi-user architecture | Better scale path | Requires product and architecture redesign; not current scope |

### Technical recommendation

Treat the current product as **single-machine/single-company until a supported topology is approved**. Do not promise LAN or enterprise multi-user deployment from current evidence.

### Final approval

**Status: NEEDS USER DECISION**

## 5. D02 Database Data Directory

### Current state

The database currently lives at `AppContext.BaseDirectory/library.db`.

### Options

- A. Keep executable directory: portable, but weak separation from binaries and upgrade/uninstall risk.
- B. `%LocalAppData%`: standard per-user location, but tied to the Windows profile.
- C. Dedicated application-data directory: clear separation, ACLs, backup and upgrade control.
- D. User-configurable directory: flexible, but adds path, permissions, network and support complexity.

### Technical recommendation

Use a dedicated application-data directory separate from the executable. The exact per-user/per-machine location depends on D01.

### Final approval

**Status: APPROVAL REQUIRED**  
**Status: NEEDS USER DECISION**

## 6. D03 Backup Destination

### Current state

Backup destination is selected by the user through a file dialog. The service validates the resulting SQLite file, but no approved backup-location policy exists.

### Options

- A. Same local disk: convenient, but not disaster recovery.
- B. Different local disk: protects against some disk failures.
- C. External drive: portable, but removable-media loss/theft risk.
- D. Network location: operationally useful, but depends on connectivity and permissions.
- E. User-configurable destination: flexible, but safety depends on user choice.
- F. Multiple destinations: stronger recovery, but higher operational and retention complexity.

### Technical recommendation

Support a protected local working backup plus at least one recovery copy outside the primary disk failure domain once policy is approved. Do not assume cloud storage.

### Final approval

**Status: NEEDS USER DECISION**

## 7. D04 Backup Automation

### Current state

Manual backup exists. Automatic scheduling, startup backup and shutdown backup are not implemented.

### Options

- A. Manual only: simplest, but depends on operator discipline.
- B. Scheduled automatic: stronger coverage, but needs retention, failure notification and scheduling ownership.
- C. Startup backup: can slow startup and does not cover machines that never restart cleanly.
- D. Shutdown backup: unreliable after crash or power loss.
- E. Hybrid manual plus scheduled: strongest practical option, with more operational complexity.

### Technical recommendation

Hybrid manual plus scheduled backup is recommended after destination, retention, failure notification and ownership are approved. Shutdown-only backup is not recommended as the primary mechanism.

### Final approval

**Status: NEEDS USER DECISION**

## 8. D05 Retention

### Current state

No retention or automatic deletion policy exists. No legal/accounting retention source is present in the repository.

### Technical options

The future implementation can support count-based, age-based, protected/pinned and manually deleted backups with deletion audit. It can also distinguish working backups from archive/recovery copies.

### Business policy required

The owner must decide backup count, age classes, protected copies, deletion authority and whether payroll/audit data needs separate handling.

### Legal/accounting boundary

No day/month/year value is selected here. Legal/accounting retention must come from an authoritative source.

### Final approval

**Status: NEEDS LEGAL/SOURCE VALIDATION** and **NEEDS USER DECISION**

## 9. D06 RPO

### Meaning

Recovery Point Objective is the maximum acceptable amount of data loss measured in time after an incident.

### Implications

A lower RPO requires more frequent backups, stronger automation, reliable destinations and monitoring. A higher RPO permits less frequent/manual backups but increases possible data loss.

### Technical recommendation

Define RPO separately for library activity, employee/payroll records and audit data if the business requires different protection. No target is selected.

### Final approval

**Status: NEEDS USER DECISION**

## 10. D07 RTO

### Meaning

Recovery Time Objective is the maximum acceptable time to restore usable service after an incident.

### Implications

A lower RTO requires prepared recovery media/data, documented operators, tested restore steps and possibly redundant infrastructure. A higher RTO permits a manual recovery process.

### Technical recommendation

Set RTO per supported deployment model and document who performs recovery. No SLA or time target is invented.

### Final approval

**Status: NEEDS USER DECISION**

## 11. D08 Database Encryption

### Current state

The SQLite database is not application-encrypted. OS-level disk/profile protection has not been established as a deployment contract.

### Options

| Option | Security | Complexity | Backup/restore effect |
|---|---|---|---|
| A. No database-level encryption | Relies on OS and filesystem controls | Low | Simple, weak against copied-file access |
| B. OS/filesystem protection | Strong when device/volume is protected | Low/Medium | Minimal application change; depends on deployment |
| C. Encrypted SQLite provider | Protects copied database content | High | Provider, migration, key and licensing compatibility required |
| D. Selected-field protection | Protects chosen values | Medium/High | Does not protect all database content or metadata |

### Technical recommendation

Require a threat-model and deployment decision before choosing. Do not add SQLCipher, another package or field encryption from this approval document.

### Final approval

**Status: APPROVAL REQUIRED**  
**Status: NEEDS USER DECISION**

## 12. D09 Key Management

If encryption is selected, keys must not be stored in source, ordinary configuration, SQLite rows or ordinary backup folders.

Possible designs include Windows DPAPI-protected local key material, certificate-store protection, enterprise key management or an operator-managed recovery key. The final design must define:

- key owner and authorized users;
- Windows user/machine binding;
- recovery key and escrow;
- key rotation;
- restoration on another machine;
- behavior after profile replacement;
- behavior when the key is lost.

### Final approval

**Status: NEEDS USER DECISION** and **NEEDS TECHNICAL VALIDATION**

## 13. D10 Backup Encryption

### Options

- A. No encryption: simple, but copied backups expose data.
- B. OS-protected backup: depends on filesystem/device controls.
- C. Application-level encrypted backup: protects portable/offline copies but requires key recovery.
- D. Enterprise-managed encryption: strong for managed customers but not universally available.

### Technical recommendation

Backups should be encrypted if they may leave the protected machine or be placed on removable/network media. The method and key owner require D08/D09 approval. No package or implementation is selected.

### Final approval

**Status: NEEDS USER DECISION**

## 14. D11 Recovery Ownership

Responsibilities include backup execution, restore approval, recovery testing, key recovery and disaster response.

Possible owners:

- system administrator;
- library administrator;
- business owner;
- designated IT support;
- shared responsibility.

The repository does not identify an organizational owner.

### Final approval

**Status: NEEDS USER DECISION**

## 15. D12 Multi-Company/Multi-Tenant

### Current state

The architecture has a single active Company profile and no tenant identifier on core records. Storage, backup and authorization assumptions are single-company.

### Options

- A. Single-company only.
- B. Future multi-company.
- C. Multi-tenant product.
- D. Undecided/deferred.

### Technical recommendation

Keep the current implementation single-company and explicitly defer multi-company/multi-tenant changes until a product decision exists. Do not claim tenant readiness.

### Final approval

**Status: NEEDS USER DECISION**

## 16. D13 Deployment/Installer

### Current state

Release and self-contained win-x64 executable artifacts exist. No installer project, signing configuration, update mechanism or uninstall workflow was verified.

### Decision areas

- Installer required or portable distribution acceptable.
- Code signing ownership/certificate.
- Upgrade and rollback behavior.
- Uninstall behavior and database preservation.
- Clean-machine installation support.
- Data/log/backup locations.

### Technical recommendation

Choose a supported installer and signing strategy before implementation. This document intentionally does not select MSI, MSIX or another technology.

### Final approval

**Status: NEEDS USER DECISION**

## 17. D14 Data Privacy Boundary

### Actual repository data categories

- Authentication: `Username`, `PasswordHash`, user/role/employee links.
- Employee: name, phone, email, address, hire date, employment status and organization references.
- Member: member number, name, email, phone and address.
- Payroll: salary history, payroll amounts, components, payment fields and payslip data.
- Tax: tax rule names, countries, rates, thresholds, source references and configuration state.
- Audit: user, action, entity, details and UTC timestamp.
- Company/organization: legal/commercial information, identifiers, contacts and structure.

### Technical boundary

These records exist in SQLite and are included in backup exposure. Password hashing exists, but database/backup encryption, retention and complete redaction policy are not established.

### Business/legal boundary

No privacy law, deletion period, payroll retention period or accounting requirement is selected. These require authoritative jurisdictional and customer-policy input.

### Final approval

**Status: NEEDS USER DECISION** and **NEEDS LEGAL/SOURCE VALIDATION**

## 18. Decision Matrix

| ID | Decision | Current state | Options | Technical recommendation | Final approval needed | Dependency |
|---|---|---|---|---|---|---|
| D01 | Deployment model | Undefined; current app is desktop/single-company | Single-user, shared workstation, LAN, future multi-user | Treat current support as single-machine until approved | NEEDS USER DECISION | Product/operations |
| D02 | Database data directory | `AppContext.BaseDirectory/library.db` | Executable, LocalAppData, dedicated, configurable | Dedicated application-data directory | NEEDS USER DECISION | D01/deployment |
| D03 | Backup destination | User-selected file path | Same disk, other disk, external, network, configurable, multiple | Local working copy plus separate recovery copy | NEEDS USER DECISION | Operations/storage |
| D04 | Backup automation | Manual only | Manual, scheduled, startup, shutdown, hybrid | Hybrid after policy approval | NEEDS USER DECISION | D03/D05/D11 |
| D05 | Retention | Undefined | Count, age, manual, protected | Policy-driven support with protected copies | NEEDS LEGAL/SOURCE VALIDATION | Business/legal |
| D06 | RPO | Undefined | Business target | Define per data class/deployment | NEEDS USER DECISION | D03/D04 |
| D07 | RTO | Undefined | Business target | Define per deployment/recovery owner | NEEDS USER DECISION | D11/D13 |
| D08 | Database encryption | Not implemented | None, OS, encrypted SQLite, fields | Decide from threat model; no blind package choice | NEEDS USER DECISION | Privacy/security |
| D09 | Key management | No application key | DPAPI, certificate store, enterprise KMS, operator key | Recovery-capable protected key design | NEEDS TECHNICAL VALIDATION | D08/D11 |
| D10 | Backup encryption | Not implemented | None, OS, application, enterprise | Encrypt copied/offline backups if threat model requires | NEEDS USER DECISION | D08/D09 |
| D11 | Recovery ownership | Undefined | Admin, library admin, owner, IT, shared | Shared responsibility must be explicitly assigned | NEEDS USER DECISION | Operations |
| D12 | Multi-company/tenant | Single-company architecture | Single, multi-company, tenant, deferred | Defer expansion until product decision | NEEDS USER DECISION | Product strategy |
| D13 | Installer/deployment | Artifacts exist; installer absent | Installer/portable/update alternatives | Select distribution/signing strategy before implementation | NEEDS USER DECISION | D01/D02 |
| D14 | Data privacy boundary | Sensitive categories verified; obligations undefined | Technical-only, policy/legal | Define privacy/retention/access boundary | NEEDS USER DECISION | Legal/policy |

## 19. Approval Status

No decision is marked `APPROVED` because no explicit business owner approval exists in repository evidence.

| Status | Decisions |
|---|---|
| APPROVED | None |
| REJECTED | None |
| DEFERRED | Multi-company implementation is technically deferred, but final product direction still needs approval |
| NEEDS USER DECISION | D01, D02, D03, D04, D06, D07, D08, D10, D11, D12, D13, D14 |
| NEEDS LEGAL/SOURCE VALIDATION | D05, D14 |
| NEEDS TECHNICAL VALIDATION | D09 |

## 20. User Decision Sheet

### D01 — Deployment Model
**Recommendation:** Treat current supported deployment as single-machine until approved.  
**Decision:** `____________________`

### D02 — Database Data Directory
**Recommendation:** Dedicated application-data directory separate from executable.  
**Decision:** `____________________`

### D03 — Backup Destination
**Recommendation:** Local working backup plus separate failure-domain recovery copy.  
**Decision:** `____________________`

### D04 — Backup Automation
**Recommendation:** Hybrid manual plus scheduled backup after policy approval.  
**Decision:** `____________________`

### D05 — Retention
**Recommendation:** Policy-driven count/age/protected-copy design; no invented period.  
**Decision:** `____________________`

### D06 — RPO
**Recommendation:** Set target per data class and deployment.  
**Decision:** `____________________`

### D07 — RTO
**Recommendation:** Set target per deployment and recovery owner.  
**Decision:** `____________________`

### D08 — Database Encryption
**Recommendation:** Decide from threat model and copied-file risk before selecting technology.  
**Decision:** `____________________`

### D09 — Key Management
**Recommendation:** Require protected storage, recovery key and loss/replacement procedure.  
**Decision:** `____________________`

### D10 — Backup Encryption
**Recommendation:** Encrypt backups that leave the protected machine after key policy approval.  
**Decision:** `____________________`

### D11 — Recovery Ownership
**Recommendation:** Assign named operational responsibility and escalation.  
**Decision:** `____________________`

### D12 — Multi-Company/Multi-Tenant
**Recommendation:** Keep current product single-company until product direction is approved.  
**Decision:** `____________________`

### D13 — Deployment/Installer
**Recommendation:** Approve installer/signing/update/uninstall strategy before packaging implementation.  
**Decision:** `____________________`

### D14 — Data Privacy Boundary
**Recommendation:** Approve technical access/encryption/retention boundary with legal review.  
**Decision:** `____________________`

## 21. Future Implementation Dependencies

```text
D01/D02 deployment and data path
  -> storage architecture and data migration plan
  -> isolated upgrade/clean-machine tests

D08/D09/D10 encryption and key decisions
  -> provider/cryptography selection
  -> key recovery design
  -> implementation/package review
  -> encrypted backup/restore tests
  -> key-loss/replacement recovery drill

D03/D04/D05/D06/D07/D11 backup operations
  -> destination/retention/scheduler design
  -> automation and metadata implementation
  -> failure notification
  -> recovery drill and RPO/RTO evidence

D14 privacy boundary
  -> access/retention/redaction requirements
  -> documentation and operational controls

D12 product direction
  -> company/tenant architecture decision
  -> authorization/storage/backup isolation design
```

No dependency step is executed by this document.

## 22. Deferred Decisions

- Exact database directory and migration from the current executable directory.
- Encryption provider/algorithm and key storage.
- Backup encryption/signing.
- Retention counts, ages and legal periods.
- Automatic backup schedule.
- RPO/RTO targets.
- Recovery owner.
- Installer/signing/update technology.
- Multi-company/tenant direction.
- Privacy/deletion/access policy.

## 23. Legal/External Dependencies

- Privacy and employment-data obligations.
- Payroll/tax/accounting retention requirements.
- Deletion/export/access requirements.
- Encryption/key-custody requirements.
- Customer contractual recovery obligations.
- No legal or accounting conclusion is made by this gate.

## 24. Environment Dependencies

- Real Windows interactive session for ACL, DPI, accessibility and restart behavior.
- Clean-machine installation/upgrade/uninstall environment.
- Disk-full, permission-denied, disconnected-network and interrupted-operation test environments.
- Off-machine/offline storage for recovery-copy drills.
- Key recovery environment if encryption is approved.

## 25. Approval Gate

This document is ready for owner review. It does not authorize implementation.

Implementation may start only after:

1. D01-D14 receive explicit statuses from the project owner or designated approvers.
2. Legal/source-dependent decisions have authoritative input.
3. Technical dependencies and migration/data movement risks are accepted.
4. A separate implementation task is authorized with focused tests and release gates.

## Final Gate

**PH2-DECISION-01 — DECISION GATE PREPARED — STOP**

No production code, tests, models, migrations, database, packages, backup behavior or deployment behavior was modified.
