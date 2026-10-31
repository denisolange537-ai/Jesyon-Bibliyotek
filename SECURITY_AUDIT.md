# Security Audit

Date: 2026-09-05
Scope: authentication, authorization, sensitive data, backup/restore, audit logging and security tests. TASK 2A added security regression tests only; no production code or schema was changed.

## Decision

**NOT SECURE FOR COMMERCIAL PRODUCTION** until the High findings below are addressed and independently tested. The application has useful foundations, but passing functional tests is not equivalent to a completed security assessment.

## Strengths Verified

- ASP.NET Identity `PasswordHasher<User>` is used; plaintext passwords are not stored.
- Minimum password length is 12 characters.
- Usernames are normalized with invariant lower casing.
- Initial administrator creation is limited to an empty user store.
- Authentication requires active user and active employee.
- Session logout clears the current user.
- Service methods commonly call `RequirePermission`.
- Sensitive settings/backup/audit paths require authorization.
- Audit records include action, entity, user, details and UTC timestamp.

## Findings

### SEC-001 | HIGH | Incomplete authentication lifecycle

**Evidence:** `Services/AuthenticationService.cs`, `Services/AuthenticationSession.cs`, `tests/GestionBibliotheque.Tests/AuthenticationServiceTests.cs`.

**Update 2026-10-08:** Session idle/max timeout and same-user unlock are implemented; payroll approval now requires password reauthentication, enforced in the service boundary with a one-use grant and audited without recording the password. Automated coverage exists for those paths. WPF interactive and sleep/resume/restart verification is still outstanding.

Other lifecycle controls remain incomplete: no general throttling policy, MFA implementation, or complete security-event/retention policy was found. The owner approved and recorded the MFA policy decisions D1–D6 on 2026-10-08; TOTP enrollment, protected secret/key ownership, one-time recovery-code storage/use, and recovery workflow still require a separate implementation task after S2-B. Password change/reset workflows have since been added and must be assessed against their own implementation and recovery tests.

**Impact:** password guessing and stale sessions are not controlled to commercial expectations.

**Recommendation:** finish WPF lifecycle verification; implement the approved MFA/recovery policy after S2-B key/secret design; assess remaining throttling and security-event/retention requirements; keep abuse and recovery tests current.

### SEC-002 | HIGH | RBAC is hard-coded, not administrable

**Evidence:** `Services/AuthorizationService.cs`, `Services/Permissions.cs`.

Permissions are a static dictionary selected by role name. The persisted `Role` entity does not represent permission assignments, and no role/permission management workflow exists.

**Impact:** customers cannot safely configure least privilege; role names and code releases control authorization. Future changes risk privilege drift.

**Recommendation:** persist permissions and role-permission assignments, protect administrator self-lockout, enforce authorization in every service, and add a complete role/action matrix test.

### SEC-003 | HIGH | Backup/restore still lacks confidentiality and operational recovery controls

**Evidence:** `Services/BackupService.cs`, `SettingsView.xaml.cs`, `SettingsBackupAuditTests.cs`.

Backup/restore now validates SQLite integrity/schema/migration history, uses staging, creates a pre-restore safety backup and has invalid-file/authorization tests. Encryption, signing, retention, automatic backup and a documented recovery drill remain absent.

**Impact:** backup disclosure/tampering and incomplete operational disaster recovery.

**Recommendation:** define protected backup locations, encrypt and sign backups, validate with SQLite integrity/schema checks, create a pre-restore snapshot, restore to staging, verify, then swap atomically; document recovery point/time objectives.

### SEC-004 | HIGH | Sensitive local data protection is unspecified

Employee personal data, tax identifiers, salary and payroll are stored in SQLite. No application encryption-at-rest, key management, access-control deployment requirement or backup confidentiality policy was found.

**Recommendation:** document threat model and Windows ACL requirements at minimum; decide whether database/backup encryption is required for the target market and implement key handling without storing secrets in config.

### SEC-005 | HIGH | Payroll authorization and integrity need defense in depth

Service checks exist and `SaveCalculatedAsync` now validates caller values against effective salary, configured tax and components. Authorization tests are improved but are not a complete matrix for every read/write/sensitive payroll action.

**Recommendation:** derive all monetary values server-side, reauthorize every operation, separate preparation/review/approval/pay actions, and add negative tests for every role and state.

### SEC-006 | MEDIUM | Audit log is not tamper-evident or complete

**Evidence:** `Models/AuditLog.cs`.

Audit has no old/new value payload, correlation/request ID, reason, approval evidence, hash chain, append-only storage policy or retention policy. Some state changes and audit writes are not in one explicit transaction.

**Recommendation:** make critical state change plus audit atomic, capture safe diffs and actor/context, restrict audit deletion, and define archival/retention.

### SEC-007 | MEDIUM | Error messages may expose technical details

**Evidence:** `MainWindow.xaml.cs`, `SettingsView.xaml.cs`.

Some dialogs include `exception.Message`, which can contain database paths, provider details or filesystem information.

**Recommendation:** show stable user messages, log technical details with event IDs, and test that secrets/paths are not exposed.

## Security Test Gaps

- Brute-force/lockout/throttling tests.
- Password change/reset and session expiry tests.
- Full role x permission x operation matrix.
- Direct service calls for every sensitive read/write.
- Unauthorized payroll salary/tax access tests.
- Restore of invalid, malicious, incompatible and corrupted files.
- Backup confidentiality, retention and pre-restore rollback tests.
- Audit atomicity and tamper-resistance tests.
- Concurrent approval/close and stale-session tests.

## Security Gate

Do not declare secure or production ready until SEC-001 through SEC-005 have implemented controls and passing negative/regression tests. No legal, tax or security certification is implied by this report.

## TASK 2A Verification

- Authentication tests: 4/4 passed.
- Authorization/RBAC-focused tests: 18/18 passed.
- Combined authentication/RBAC-focused tests: 22/22 passed.
- TASK 5 combined security regression: 44/44 passed.
- Full regression: 68/68 passed.
- Restore: passed with exit code 0.
- Release build: passed with exit code 0.
- No production code or database schema changed.
- No EF model verification was required because TASK 2A introduced no model change.

Additional verified behavior includes inactive-user rejection, no-session denial, inactive-session denial, logout state clearing, password hashing/verification, and Librarian denial of Administrator-only permissions.

TASK 5 also verifies service-boundary denial for Librarian payroll calculation and calculated-entry save, payroll lifecycle transitions, tax-rule add/deactivate, employee/company operations, and backup/restore operations.

Remaining gaps are policy/product hardening items: lockout/throttling, password change/reset, MFA, session timeout, persisted configurable RBAC, login security-event auditing and complete permission-matrix coverage.
