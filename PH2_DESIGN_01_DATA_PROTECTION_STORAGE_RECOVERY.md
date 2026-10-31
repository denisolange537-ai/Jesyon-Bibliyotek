# PH2-DESIGN-01 — Data Protection, Storage and Recovery Decision

Date: 2026-09-05  
Status: Design complete; no implementation authorized  
Scope: Read-only audit, architecture design and policy boundary only

## 1. Executive Summary

The current application is a Windows WPF desktop application using EF Core 10 and SQLite. The runtime database is currently placed at `AppContext.BaseDirectory/library.db`, which is beside the executable. Startup applies EF migrations with `Database.MigrateAsync()`.

The current backup implementation is materially safer than the historical baseline: it uses SQLite backup APIs, temporary files, SQLite integrity validation, required-schema validation, migration-history validation, staged restore, a pre-restore safety backup, authorization and audit events. It does not provide encryption, signing, retention, automated scheduling, a recovery drill or a durable disaster-recovery copy.

This document does not select legal retention periods, tax rules, encryption algorithms, cloud providers, installer technology or RPO/RTO targets. Those require explicit business, privacy, security, legal or operational decisions.

## 2. Current Architecture Evidence

Verified repository evidence:

- `GestionBibliotheque.csproj` targets `net10.0-windows`, enables WPF, nullable reference types and implicit usings.
- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.0 and `SQLitePCLRaw.lib.e_sqlite3` 2.1.13 are referenced.
- `ServiceCollectionExtensions` forces the configured SQLite data source to `Path.Combine(AppContext.BaseDirectory, "library.db")` and registers `IDbContextFactory<LibraryDbContext>`.
- `DatabaseInitializer` calls `Database.MigrateAsync()` during application startup.
- `LibraryDbContextFactory` uses SQLite for design-time operations.
- `BackupService` uses `SqliteConnection.BackupDatabase` with pooling disabled.
- Backup/restore requires `SettingsManage` at the service boundary.
- Backup and restore write `BackupCreated` and `DatabaseRestored` audit events containing only the filename.
- Backup tests use isolated temporary SQLite databases; no production database was used by this design task.

## 3. Current Database Storage

### Verified state

- Provider: SQLite.
- Runtime filename: `library.db`.
- Runtime location: application base directory, normally beside the executable.
- Configured connection-string path is overwritten to that base-directory location.
- Database initialization: EF migrations at startup.
- Database path is not exposed as a user setting.
- No WAL mode or journal mode is explicitly configured in application code.
- The code does not explicitly manage `-wal` or `-shm` sidecar files.
- SQLite backup API is used rather than a raw byte-for-byte file copy.
- Tests verify backup integrity and invalid restore rejection, but do not establish a full production storage/deployment contract.

### Current implications

The executable-directory path is portable and simple, but it can cause problems with Windows ACLs, installation directories, upgrades, uninstall behavior, write permissions and data separation. It also makes it easier for a file copy or application folder backup to include sensitive data unintentionally.

## 4. Database Protection Analysis

| Protection area | Current state | Design implication |
|---|---|---|
| SQLite file encryption | Not implemented/Not verified | A copied database can expose stored data if filesystem access is obtained |
| Windows ACL protection | Not configured by application | Deployment must define owner/user permissions |
| EF migrations | Applied automatically at startup | Upgrade failure handling and pre-upgrade backup need operational design |
| Foreign keys/indexes | Present in EF model | Integrity is partly enforced by schema |
| Transactions | Present in critical payroll and backup paths | Atomicity is stronger than historical baseline |
| Concurrency | `PayrollPeriod.Version` token exists | Payroll period stale writes are detectable |
| WAL/journal policy | Not explicitly configured | Backup/storage design must test supported SQLite modes |
| Database path | Beside executable | Requires a deployment/data-directory decision |

## 5. Encryption Options

### Option A — Unencrypted SQLite plus Windows filesystem protection

- Lowest implementation complexity.
- Works with current provider and migrations.
- Depends on Windows user/profile ACLs, disk encryption and operational discipline.
- Does not protect a copied database or backup opened elsewhere.
- Not sufficient alone for high-sensitivity portable backup handling.

### Option B — Encrypted filesystem/volume

- Protects the database and backups when the volume is locked.
- Minimal application impact.
- Depends on Windows/device management and recovery keys.
- Does not protect an authorized user or a copied file after extraction.

### Option C — SQLCipher or equivalent encrypted SQLite provider

- Provides database-level encryption for the database file and potentially backups.
- Requires provider/package compatibility, migration testing, key lifecycle and licensing review.
- Can affect restore, tooling, diagnostics, performance and support.
- Must not be selected only because it is easy to add.

### Option D — Application-level field encryption

- Can protect selected fields such as tax identifiers or sensitive payroll values.
- Does not protect schema, indexes, all database content or backup metadata by itself.
- Adds key-management and query limitations.
- Not a replacement for database/backup protection.

### Option E — Windows DPAPI/ProtectedData for secrets or local key wrapping

- Useful for protecting a locally stored key against other Windows users.
- Bound to a Windows user or machine scope depending on mode.
- Recovery after profile/machine replacement requires a documented recovery key or administrative process.
- Does not by itself encrypt the whole SQLite database.

### Technical recommendation

Use a dedicated application-data directory with Windows ACLs and device-level encryption as the baseline. Before implementing database-level encryption, complete the threat model and decide whether copied database/backup protection is a release requirement. If it is required, evaluate an encrypted SQLite provider or an encrypted backup container with compatibility, key recovery and licensing evidence.

No encryption library or algorithm is selected by this document.

## 6. Key Management Analysis

### What would need a key

- Database-level encryption, if selected.
- Backup encryption, if selected.
- Backup signing/integrity authentication, if selected.
- Optional field-level protection for particularly sensitive values.

### Where a key could live

Options require a decision:

- Windows DPAPI-protected local key material.
- Windows Certificate Store with ACLs.
- Enterprise key-management service for managed deployments.
- Operator-managed recovery key stored outside the application machine.

No key should be stored plaintext in `appsettings.json`, source code, the SQLite database or an ordinary backup folder.

### Required key decisions

- Who owns the key: end user, organization administrator, vendor or managed IT service?
- Can the application operate offline?
- Is a recovery key mandatory?
- What happens if the Windows profile or machine is replaced?
- Is key rotation required?
- Can old backups still be restored after rotation?
- Is key escrow permitted by the customer policy?

### Loss scenario

If an encryption key is lost and no recovery copy exists, encrypted database/backups may be permanently unreadable. This must be an explicit acceptance decision before encryption implementation.

## 7. Backup Current State

### Verified behavior

1. Service authorization requires `SettingsManage`.
2. Destination/source paths are normalized.
3. Active database path cannot be supplied as the backup or restore path.
4. Backup uses a temporary SQLite destination.
5. SQLite `BackupDatabase` creates a consistent provider-level copy.
6. Temporary output is checked for existence and non-zero length.
7. `PRAGMA integrity_check` is executed.
8. Required application tables are checked.
9. Applied migration history is checked against migrations available to the current context.
10. Validated temporary backup replaces an existing destination using `File.Replace` or moves into place.
11. Successful backup is audit logged.
12. Restore validates the source before touching the active database.
13. Restore creates a safety copy of the current database.
14. Restore stages and validates the source copy before writing to the active database.
15. Post-restore integrity validation is executed.
16. Restore attempts to recover the safety copy on failure.
17. Successful restore is audit logged.

### Not currently implemented or not fully verified

- Encryption/signing of backup files.
- Retention and automatic deletion policy.
- Automatic scheduled backup.
- Durable off-machine recovery copy.
- Recovery drill and documented operator runbook.
- Full handling policy for newer-than-current backups; current validation ensures required migrations exist but does not establish a complete downgrade/upgrade compatibility policy.
- Explicit WAL/SHM operational policy.
- Durable recovery artifact if the process or disk fails during the restore itself.

## 8. Backup Protection Design

### Backup classes

Separate these concepts:

- **Working backup:** a recent local copy used for operational restore.
- **Recovery copy:** a copy on a different failure domain, such as an external drive or approved network location.
- **Archive copy:** a policy-controlled historical copy kept for an approved period.

A second file on the same disk is not a disaster-recovery copy.

### Recommended future controls

- Validate backup before publication.
- Use protected backup directories rather than arbitrary untrusted locations by default, subject to product decision.
- Add metadata: creation time, application version, schema/migration identity, source database identity and integrity result.
- Encrypt/sign backups if the threat model requires copied-file protection.
- Keep at least one recovery copy outside the primary machine failure domain, subject to business policy.
- Test restore regularly in an isolated database.
- Keep failed backup artifacts out of the published backup set.

## 9. Backup Retention Policy Boundary

### Technical recommendation

The implementation should support a retention mechanism that can express:

- manual retention;
- count-based retention;
- age-based retention;
- protected/pinned backups;
- metadata and deletion audit;
- failure when deletion would remove the last recoverable copy.

### Business policy required

The business owner must decide:

- how many copies to retain;
- whether backups are daily/weekly/monthly or event-based;
- which copies are protected from automatic deletion;
- whether customers can override retention;
- who can delete a backup;
- whether payroll/audit backups require separate handling.

### Legal/accounting dependency

No retention period is specified here. Legal or accounting requirements must come from an authoritative source for the target jurisdiction and customer type.

## 10. Automated Backup Analysis

Current behavior: manual backup through Settings UI and service call. Automatic scheduling is not implemented.

| Trigger | Assessment |
|---|---|
| Application start | Can protect against some incidents but may slow startup and repeat unnecessarily |
| Scheduled background backup | Stronger operational coverage but requires scheduler, failure notification, retention and permissions |
| Shutdown backup | Unreliable if the process crashes or power is lost |
| Pre-restore backup | Strongly recommended and already represented by current restore safety behavior |
| Pre-migration backup | Requires upgrade/migration operational policy before implementation |
| Manual backup | Exists and is tested |

Recommendation: automatic backup is **recommended**, not implementable until destination, retention, failure notification and recovery policy are approved. Pre-restore protection is **required for future implementation** and is already partially implemented.

## 11. Restore Safety Design

Future restore requirements:

1. Authorize the operation at the service boundary.
2. Normalize and validate source path.
3. Open source as SQLite read-only.
4. Run integrity validation.
5. Validate required schema and migration compatibility.
6. Create and validate a safety backup of the current database.
7. Stage the source restore.
8. Replace or apply the staged database using a provider-safe atomic strategy.
9. Reopen and validate the resulting database.
10. Recover from the safety copy on failure where technically possible.
11. Do not report success before post-restore validation.
12. Audit success/failure without secrets.
13. Require application restart or a proven context-reload strategy before stale in-memory views are trusted.

Current implementation covers many of these requirements. The remaining design work concerns encryption, operational recovery, durable safety-copy handling and compatibility policy.

## 12. Disaster Recovery Design

| Scenario | Detection | Recovery source | Expected data loss | Operator requirement | Dependency |
|---|---|---|---|---|---|
| Database corruption | Startup/integrity failure or service error | Latest validated recovery copy | Undefined until RPO chosen | Stop writes, restore isolated copy, validate | RPO/RTO policy |
| Accidental deletion | Missing database file | Latest backup/recovery copy | Since last backup | Restore and restart application | Retention/backup policy |
| Application crash | Startup failure/logs | Existing database plus backup | Usually none to last committed transaction | Diagnose, validate DB, restore only if needed | Operations runbook |
| Failed migration | Startup migration exception | Pre-migration backup | Since pre-migration backup | Do not retry blindly; restore/review migration | Upgrade policy |
| Invalid backup | Integrity/schema check fails | Current database or another backup | None if current DB intact | Reject source and preserve current DB | Existing validation |
| Corrupted backup | Integrity check fails | Another recovery copy | Depends on copy age | Mark unusable and investigate | Retention policy |
| Disk failure | OS/storage failure | Off-machine recovery copy | Since last off-machine copy | Replace storage/machine, restore and validate | Recovery-copy policy |
| Lost backup | Backup inventory gap | Remaining copies | Undefined | Inventory review and incident response | Retention/operations |
| Ransomware/malware | Endpoint/security detection | Offline/immutable recovery copy | Since isolated copy | Isolate machine, security response, restore clean environment | Security operations |
| Accidental overwrite | Unexpected data audit/state | Safety or historical backup | Since selected copy | Stop writes, identify correct copy, restore isolated first | Operator procedure |
| Machine replacement | Hardware failure/replacement | Off-machine recovery copy and application package | Since last recovery copy | Install supported package, restore data, validate | Installer/key policy |
| Application reinstall | Missing application directory | Data directory/recovery copy | Depends on data separation | Reinstall without deleting data, rebind/restore | Storage-path decision |
| Windows profile replacement | DPAPI/user-profile key loss if used | Recovery key and backup | Potentially all encrypted data without recovery key | Recover key/profile or restore from supported escrow | Key policy |

## 13. RPO/RTO Decision Boundary

No RPO or RTO target is defined in the repository or approved project material.

Do not invent a number.

Business/operations must decide:

- maximum acceptable data loss for library activity;
- maximum acceptable payroll/employee data loss;
- maximum acceptable recovery downtime;
- whether different data classes have different targets;
- support hours and recovery ownership.

Technical implementation must be derived from approved targets, not the reverse.

## 14. Privacy/Data Protection Analysis

### Data categories verified from models

| Category | Evidence | Storage/backup exposure | Protection consideration |
|---|---|---|---|
| Authentication data | `User.Username`, `PasswordHash`, role/employee links | SQLite and backups | Hashing is present; copied-file protection and access controls remain decisions |
| Employee personal data | name, phone, email, address, hire/status fields | SQLite and backups | Least privilege, encryption/access control, retention policy |
| Member data | name, member number, phone, email, address | SQLite and backups | Data minimization, access control and retention policy |
| Payroll data | salary history, payroll entries/components, pay data | SQLite and backups | High sensitivity; encryption, access review, recovery and retention decisions |
| Tax configuration/data | tax rules, source references, rates/thresholds | SQLite and backups | Legal source control, change audit and access control |
| Audit data | actor, action, entity, details, timestamp | SQLite and backups | Audit retention, tamper resistance and privacy minimization |
| Company/organization data | company, departments, positions | SQLite and backups | Access control and customer data handling |

No legal privacy conclusion is made here. Jurisdiction-specific privacy and employment-data obligations require external review.

## 15. Logging/Audit Security

Verified:

- Host logging and startup failure logging exist.
- Audit records include action, entity, user, details and UTC time.
- Payroll, tax-rule and backup/restore success events are represented.
- Passwords and hashes are not intentionally placed in backup audit details.

Not verified or incomplete:

- Comprehensive login success/failure security-event audit.
- Old/new value capture for all sensitive changes.
- Tamper-evident audit storage.
- Retention and privacy-minimization policy.
- Full redaction test for every exception/log path.

Future logging design must avoid passwords, hashes, encryption keys, tokens and database credentials.

## 16. Database Path Recommendation

### Options

| Option | Advantages | Risks/limits |
|---|---|---|
| Beside executable | Portable and simple | Write ACLs, upgrade/uninstall coupling, accidental data deletion |
| `%LocalAppData%` | Standard per-user writable location | User-profile dependency, profile replacement/recovery concerns |
| Dedicated application-data directory | Clear separation, ACL and backup control | Requires installer/storage migration design |
| User-configurable directory | Supports operational needs | Path validation, permissions, network/disconnect and support complexity |

### Technical recommendation

Use a dedicated application-data directory separate from the executable, with an explicit migration/upgrade path and protected ACLs. A per-machine or per-user choice must be made based on the deployment model; the current code does not establish that choice.

### Business/product decision

The product owner must decide whether the application is single-user, per-Windows-user, shared workstation or controlled LAN deployment. That decision determines the final directory, ACL and backup strategy.

## 17. Deployment Implications

A future deployment design must define:

- executable/install directory;
- data directory;
- log directory;
- backup/recovery directory;
- permissions and service account/user identity;
- upgrade behavior without deleting data;
- uninstall behavior that preserves or explicitly exports data;
- database migration failure handling;
- code signing;
- installer and update mechanism;
- clean-machine validation.

No installer technology is selected here.

## 18. Multi-Company Impact

Current evidence shows a single active Company profile and no tenant identifier on core records. Current storage/backup assumptions are therefore **single-company only**.

The design is not multi-tenant ready. Multi-company or multi-tenant support is a business/product and architecture decision, not an automatic consequence of this document. Future implementation would need isolation boundaries for database, backup, encryption keys, authorization and recovery.

## 19. Threat Model

| Threat | Likelihood | Impact | Current mitigation | Remaining gap | Recommendation |
|---|---|---|---|---|---|
| Stolen PC | Medium | High | Windows/SQLite deployment, authentication | Database file not encrypted by application | Decide encryption and device/ACL baseline |
| Unauthorized local Windows user | Medium | High | Login and service authorization | File access may bypass application | Protected data directory and OS ACLs |
| Copied database | Medium | High | Password hashing only | Personal/payroll data readable if opened | Database/backup encryption decision |
| Stolen backup | Medium | High | Integrity validation | No confidentiality encryption | Encrypt/sign backups after key decision |
| Ransomware | Medium | High | Manual backup and staging | No immutable/offline policy | Off-machine/offline recovery copy |
| Malicious administrator | Low/Medium | High | Role checks and audit | Administrator can perform permitted sensitive actions; audit not tamper-evident | Governance, separation of duties and audit policy |
| Accidental deletion | Medium | High | Backup/restore service | Retention/automation undefined | Approved retention and recovery procedure |
| Corruption | Medium | High | Integrity checks and transactions | Recovery drill absent | Scheduled isolated recovery test |
| Failed restore | Low/Medium | High | Safety backup and rollback attempt | Durable recovery artifact/operational drill absent | Recovery runbook and failure injection tests |
| Lost encryption key | Low/Medium | High | No application encryption currently | Future encryption could make data unrecoverable | Recovery key/escrow policy before implementation |
| Unauthorized backup access | Medium | High | `SettingsManage` service authorization | File-system destination may be user-selected | Protected destination and OS permissions |

## 20. Decision Matrix

| Decision | Current state | Options | Recommended design direction | Dependency | Later implementation |
|---|---|---|---|---|---|
| Database path | Beside executable | Keep, LocalAppData, dedicated data directory, configurable | Dedicated application-data directory | Deployment/product decision | Yes |
| Database encryption | Not implemented | OS protection, encrypted SQLite, field encryption | Decide from threat model; do not add blindly | Security/privacy decision | Maybe |
| Key management | No application key | DPAPI, certificate store, enterprise KMS, operator key | Protected key plus recovery key if encryption selected | Operations/security | Yes |
| Backup encryption | Not implemented | None, OS-protected, encrypted container, encrypted SQLite | Encrypt/sign if copied backups are in threat scope | Key policy | Yes |
| Backup location | User-selected destination | Same disk, external, network, managed storage | Separate recovery copy outside primary failure domain | Operations policy | Yes |
| Retention | Undefined | Manual/count/age/pinned | Support policy-driven retention, no invented period | Business/legal | Yes |
| Automation | Manual backup exists | Start, schedule, shutdown, pre-restore | Recommended after retention/failure policy | Operations | Yes |
| RPO | Undefined | Business target | Set by owner per data class | Business | Yes |
| RTO | Undefined | Business target | Set by owner per deployment | Business | Yes |
| Restore | Validated/staged/safety copy | Improve current, external orchestration | Preserve current controls; add durable recovery/drill | Operations | Yes |
| Disaster recovery | Tests exist, drill absent | Local restore, off-machine, offline/immutable | Define recovery-copy and drill policy | Operations/security | Yes |
| Logging/privacy | Basic audit/logging | Expand audit, minimize, tamper-evident | Define event/retention/redaction contract | Privacy/security | Yes |
| Multi-company | Single-company | Stay single, multi-company, tenant-aware | Stay single-company until product decision | Product | Deferred |

## 21. Technical Decisions

These are design recommendations, not implementation authorization:

1. Separate executable files from application data.
2. Treat database and backups as sensitive data assets.
3. Preserve provider-level SQLite backup and validation rather than raw file copying.
4. Keep validation before publication and before restore.
5. Keep a pre-restore safety copy and validate post-restore state.
6. Add durable recovery copies outside the primary disk failure domain after policy approval.
7. Define key recovery before enabling encryption.
8. Keep secrets and encryption keys out of source, ordinary configuration and database rows.
9. Test recovery in isolated databases, never against the customer database.
10. Do not claim legal/privacy compliance from technical controls alone.

## 22. Business Decisions Required

- Single-user, shared-workstation or LAN deployment model.
- Approved database/data directory behavior.
- Backup ownership and destination rules.
- Retention and deletion authority.
- Automatic backup requirement.
- Recovery ownership and support process.
- RPO and RTO targets.
- Customer responsibility for external/offline copies.
- Whether customers require encrypted database and backups.
- Installer, signing, upgrade and uninstall strategy.
- Whether multi-company support is a product requirement.

## 23. Legal/Accounting Dependencies

- Jurisdiction-specific privacy and employment-data requirements.
- Payroll and tax record retention rules.
- Accounting/audit retention rules.
- Legal requirements for deletion, export or access requests.
- Requirements affecting encrypted backups and key custody.
- No legal period, tax rule or privacy claim is invented here.

## 24. Environment Dependencies

- Real Windows interactive session for ACL, DPI, accessibility and restart validation.
- Clean-machine environment for installation, upgrade and uninstall testing.
- Representative disk-full, permission-denied, disconnected-network and interrupted-operation environments.
- Offline or separate failure-domain storage for recovery-copy drills.
- Key recovery environment if encryption is selected.

## 25. Future Implementation Dependency Chain

```text
Business/privacy/security decisions
  -> storage/encryption/retention/RPO/RTO design approval
  -> deployment/data-path architecture
  -> package/key-management selection if needed
  -> implementation
  -> focused unit/integration tests
  -> migration compatibility review if applicable
  -> backup/restore and failure-injection tests
  -> isolated recovery drill
  -> full regression and Release build
  -> clean-machine deployment validation
  -> production gate
```

## 26. Future Test Strategy

### Database/storage

- Path resolution and data-directory creation.
- ACL/permission denied behavior.
- Existing-data migration from old path.
- Concurrent access and lock behavior.
- Corruption detection.
- Disk-full and interrupted operation behavior.

### Backup

- Valid consistent backup.
- Integrity/schema/migration validation.
- Invalid/non-SQLite/corrupt backup rejection.
- Destination overwrite safety.
- Insufficient disk and interrupted backup.
- Encrypted backup open/restore if selected.
- Key rotation and recovery-key behavior if selected.

### Restore

- Valid restore.
- Failed restore rollback.
- Pre-restore safety copy.
- Schema/migration mismatch.
- Newer/incompatible source policy.
- Post-restore integrity and service usability.
- Restart/reload behavior.

### Recovery

- Machine replacement.
- Database loss.
- Backup loss.
- Encryption-key loss.
- Corrupted recovery copy.
- RPO/RTO exercise with recorded evidence.

### Security/privacy

- Unauthorized backup/restore.
- Backup destination access.
- Sensitive logging redaction.
- Key-file/DPAPI access boundaries.
- Audit event and deletion/retention authorization.

No tests are added by this design task.

## 27. Risk Register

| Risk | Severity | Probability | Impact | Mitigation | Dependency |
|---|---|---|---|---|---|
| Key loss after encryption | High | Low/Medium | Permanent unreadable data | Recovery key/escrow and restore drill | Key policy |
| Backup corruption | High | Medium | Recovery failure | Integrity checks, multiple copies, drills | Recovery policy |
| Retention deletes needed copy | High | Medium | Irrecoverable history loss | Pinned copies, deletion authorization, legal review | Retention policy |
| Database relocation breaks upgrade | High | Medium | Data appears missing/lost | Migration tool and pre-upgrade backup | Deployment design |
| Restore failure | High | Low/Medium | Downtime/data loss | Staging, safety backup, failure injection | Operations |
| User lacks backup permissions | Medium | Medium | Backup cannot run | Explicit roles and operational procedure | RBAC policy |
| Backup copied to unsafe location | High | Medium | Data disclosure | Protected destination and encryption | Privacy/security |
| Disk failure | High | Medium | Primary data loss | Off-machine/offline recovery copy | Operations |
| WAL/SHM operational mismatch | Medium | Low/Medium | Incomplete/manual copy | Provider backup API and mode-specific tests | SQLite deployment |
| Unclear multi-company boundary | High | Low/Medium | Cross-organization data design rework | Keep single-company and document decision | Product strategy |

## 28. Recommended Implementation Sequence

1. Approve PH2-DESIGN-01 decisions: data path, threat model, encryption, key recovery, retention, automation, RPO/RTO and recovery ownership.
2. Implement database/data-directory strategy with migration from the current executable-directory path.
3. Implement approved encryption/key-management controls, if required.
4. Implement backup encryption/signing, retention and automation according to approved policy.
5. Run isolated recovery and failure-injection drills.
6. Define and implement account-lifecycle security controls.
7. Define and implement persisted RBAC/permission matrix.
8. Approve payroll correction policy, then design and implement correction workflow.
9. Obtain tax/legal sources and define jurisdiction rule contract.
10. Implement export/localization according to accepted product requirements.
11. Implement installer/signing/update/uninstall.
12. Run performance, accessibility, clean-machine and final commercial gates.

No item in this sequence is started by this document.

## 29. Explicitly Deferred Items

- All production code changes.
- EF model/migrations.
- Database relocation.
- Encryption and key creation.
- Backup retention/automation.
- Cloud/network backup integration.
- Payroll correction/reversal.
- Tax/legal rule implementation.
- Authentication lifecycle redesign.
- Configurable RBAC.
- Installer/signing/update mechanism.
- Export/localization/UI/accessibility implementation.
- Multi-company/multi-tenant architecture.

## 30. Design Acceptance Gate

The design is accepted only when the project owner/security/privacy/operations stakeholders explicitly decide:

- deployment model and data path;
- encryption requirement and key ownership/recovery;
- backup destinations and recovery-copy strategy;
- retention/deletion policy;
- automation requirement;
- RPO/RTO targets;
- recovery owner and drill frequency;
- single-company versus future multi-company scope.

Until these decisions are recorded, implementation tasks depending on them remain blocked or deferred.

No production code, tests, schema, migration, package, database or runtime behavior was modified during PH2-DESIGN-01.
