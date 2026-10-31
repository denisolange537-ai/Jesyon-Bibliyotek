# PLAN FINAL — plan travay pou prepare Jesyon Bibliyotèk pou vann

**Dat plan an:** 2026-10-03  
**Vèsyon kandida:** `0.9.0-pilot` (pwovizwa; poko nan metadata build)  
**Estati pwodwi:** pa pare pou vann jeneral.  
**Dokiman sa a:** sous plan prensipal la pou 8 etap yo. Li mete scope, travay, depandans ak gate yo ansanm dapre nouvo kontra komèsyal la.

## 1. Kontra pwodwi ak limit yo

### Scope pilòt ki ka antre nan kontra a

Aplikasyon desktop Windows x64, bazdone SQLite lokal, yon bibliyotèk sou yon òdinatè. Workflow ki vize yo se:

- katalòg liv ak rechèch;
- jesyon manm;
- prete, dat retou, retou, reta/amann ak peman amann;
- rezèvasyon ak ranpli rezèvasyon;
- dashboard ak rapò debaz bibliyotèk;
- paramèt bibliyotèk;
- backup/restore manyèl ak audit viewer, sèlman apre gate done/rekiperasyon yo.

### Pa pwomèt nan pilòt sa a

- **Pewòl, payslip oswa konfòmite taks.** Modil sa yo pa sètifye legalman. Pa aktive oswa vann yo kòm kalkil final jiskaske yon espesyalis lokal valide sous ofisyèl yo, fixtures kalkil yo ak règ jiridiksyon an alekri, epi gate Etap 4 la pase.
- LAN/shared database, cloud, plizyè konpayi/branch oswa plizyè òdinatè ansanm.
- Ekspòtasyon oswa lokalizasyon ki poko fini ak teste; enstalatè, mizajou, siyati, SLA oswa garanti sipò ki poko verifye.
- Nenpòt reklamasyon sètifikasyon, vi prive oswa rekiperasyon ki pa gen prèv espesifik.

## 2. Règ egzekisyon ki obligatwa

Chak travay, san eksepsyon, suiv etap sa yo nan lòd:

**AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP**

1. **AUDIT:** enspekte kòd, tès, migrations, dokiman ak rezilta resan yo; konfime gap la toujou la epi pa gen lòt travay ki deja rezoud li.
2. **PLAN:** presize yon sèl rezilta, fichye ki ka touche, tès, depandans, limit ak prèv akseptasyon.
3. **ONE TASK:** chwazi yon sèl livrezon endivizib. Pa konbine travay endepandan, epi pa kòmanse yon lòt travay an paralèl.
4. **IMPLEMENT:** chanje sèlman sa plan travay sa a otorize. Desizyon ki mande pwopriyetè, legal oswa espesyalis rete an atant.
5. **TEST:** egzekite tès ki ekri nan plan an, regressions ki enpòtan, migrations/build ki nesesè; konsève kòmand, exit code ak rezilta. Kòmand ki pann oswa pa kouri pa konte kòm PASS.
6. **VERIFY:** revize diff, done/migration, otorizasyon, sekirite, UI/operasyon ak dokiman ki konsène travay la. Make tès ki pa posib kòm UNVERIFIED.
7. **GATE:** make PASS/FAIL/BLOCKED epi mete prèv la nan ledger la. Pa bay gate sou baz ansyen rezilta sèlman.
8. **STOP:** fini travay sa a. Si li echwe oswa gen desizyon ki poko pran, kanpe etap la nan limit depandans lan; pa sote travay la ni deklare li pase.

Nan fen chak etap, prezante prèv ak limit yo bay pwopriyetè a epi tann mo **“kontinye”** anvan etap apre a. “Kontinye” otorize travay ki vin apre a; li pa apwouve otomatikman yon chwa legal/biznis, li pa fè tès ki manke pase, epi li pa retire yon blokaj release. Kenbe task ledger sa a ajou pou evite doublon.

## 3. Lòd ak livrezon 8 etap yo

### ETAP 1 — Baz prèv, build ak kandida release (C0)

**Objektif:** jwenn prèv aktyèl pou source/build la ak artifact egzak la.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| C0-01 | Verifye SDK/dependencies epi restore tout pwojè yo | Anrejistre SDK/RID; restore chak project nan solution lan ak exit code. Dokimante nenpòt echèk `.slnx` epi verifye chak project dirèkteman. |
| C0-02 | Release build | Build application ak test project; 0 compiler error, warning yo klase; pa rele build ki pa fini PASS. |
| C0-03 | Suite tès konplè | Kouri tout suite la apre build aktyèl la; anrejistre total/pase/echwe/sote ak kòmand. |
| C0-04 | EF model ak static diagnostics | `has-pending-model-changes` sou Release; pa gen migration ki manke. Kouri diagnostics disponib; limit zouti make UNVERIFIED, pa kache yo. |
| C0-05 | Self-contained Win-x64 publish ak clean-machine launch | Publish artifact final; konsève build ID, dat, hash ak release notes; enstale/louvri sou clean Windows x64 san zouti dev. Revize NuGet vulnerability output epi klase avètisman yo. |
| C0-06 | Reconcile gap inventory ak task ledger | Verifye chak blocker sale kont source/dokiman aktyèl; mete owner, priyorite, dependency ak etap. Pa kreye doublon ak sèvis ki deja egziste. |
| C0-07 | Rezoud yon blocker C0 ki poko pase | Kòmanse sèlman si youn nan C0-01–06 bloke; yon root cause/fix pa task; rerun sèlman gate ki depann de fix la, apre sa gate C0 la. |

**Gate Etap 1:** restore, Release build, tout tès, EF model check, diagnostics, publish ak launch clean-machine gen prèv aktyèl; kritè sa yo pa ka ranplase ak restore/build project-only oswa launch sou menm machin dev la san yo pa make limit. Inventory a kòrèk. Pa antre pilòt si yon kondisyon release C0 bloke.

**Estati aktyèl:** C0-01 PASS sou restore project-pa-project; restore `.slnx` te sòti 1 san erè eksplisit. C0-02 PASS sou build chak project; build `.slnx` sòti 1 san erè/warning eksplisit. C0-03 PASS, 68/68. C0-04 EF Release PASS, pa gen pending model changes; `dotnet format` UNVERIFIED akòz BuildHost NamedPipe access denied. C0-05 BLOCKED: publish la demare sou òdinatè aktyèl la, men pa gen machin/VM pwòp verifyab. Gen `NU1900` paske NuGet pa t jwenn vulnerability data. C0-06 PASS. Pwopriyetè a mande eksepsyonèlman pou C0-06 ak Etap 2 kòmanse pandan C0-05 poko verifye; sa **pa** fè C0 oswa pilot gate pase. C0-07 rete kondisyonèl.

### ETAP 2 — Sekirite kont ak otorizasyon (S1)

**Objektif:** kontwòl kont serye ak role-based access ki persiste, teste sou service boundary la.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S1-A | Deside/aksepte account-security policy | Apwouve kòm baseline dapre enstriksyon pwopriyetè a 2026-10-03 pou fini Etap 2; detay ak limit yo nan `ACCOUNT_SECURITY_POLICY_PROPOSAL.md`. |
| S1-B | Failed-login state ak lockout/throttling | Persistans san race, mesaj login ki pa revele si kont egziste, reset kontè apre login reyisi; tests pou threshold, window, expiry, concurrency, disabled accounts ak audit. |
| S1-C | Chanjman modpas ak administrator-assisted reset | Verifye modpas aktyèl oswa idantite; reset sekirize; fòse chanjman apre sekrè tanporè; pa mete password/hash/temporary secret nan audit/log; tès chanjman, reset, echèk ak rollback. |
| S1-D | Persisted role-permission model ak migration | Katalòg permission stable; schema RolePermission; default-role/backfill backward-compatible; clean DB ak upgrade migration tests; deny-by-default. Gate migration anvan UI. |
| S1-E | Role administration service/UI | Kreye/modifye roles ak grants; service checks obligatwa, audit; pwoteje kont self-lockout/dènye admin; tests pou dirèk service calls ak invalid assignments. |
| S1-F | Permission/action matrix | Ranpli tout role × permission × service action pou views/reads/writes/sensitive actions; teste direct-call bypass, inactive employee/user, navigation ak audit. |
| S1-G | Session idle/max lifetime ak re-auth | Timeout dapre politik apwouve; lock/unlock; aksyon sansib mande reauth; tès lifecycle WPF, app sleep/restart/logout ak timeout. |
| S1-H | MFA/recovery policy ak implementation | D1–D6 TOTP/recovery deside epi anrejistre nan S1-H-D. Implementation TOTP, secret/key protection, enrollment, recovery, audit ak bypass tests rete nan task apa apre S2-B key decision pase. MFA Admin obligatwa anvan General Release. |

**Gate Etap 2:** S1-A apwouve; kontwòl ki nan policy a aplike; tout security/action matrix pase; pa gen privilege escalation; audit pa gen sekrè; migration pwòp/upgrade pase; UI kritikal entèaktif verifye. Pa kòmanse coding anvan S1-A gate la.

**Estati aktyèl:** S1-A baseline apwouve; S1-B jiska S1-E pase; S1-F/S1-G gen tès otomatik men entèraksyon WPF a poko verifye. Desizyon policy S1-H-D pase 2026-10-08; MFA/recovery pa aplike, epi secret/key protection ak workflow implementation tann S2-B ak yon task apa. C0-05 rete bloke separeman.

### ETAP 3 — Vi prive, depo done ak backup/recovery (S2)

**Objektif:** defini done kliyan, aksè/retansyon, chifreman ak rekiperasyon; pwouve restore.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S2-A | Apwouve data/privacy/recovery contract | Kategori done; database/log/backup locations; aksè/owner; deletion/retention; backup frequency; RPO/RTO ak recovery responsibilities. Legal review kote li nesesè; pa envante delè. |
| S2-B | Threat model ak encryption/key decision | Threat boundaries, Windows ACL/device protection, bezwen DB/backup encryption, key ownership, escrow/recovery, rotation, replacement/lost-key. Pa chwazi cryptography/package san review. |
| S2-C | Data-path/security implementation | Aplike kote done ak ACL dapre apwobasyon; migration done ki egziste yo san pèdi `library.db`; tès upgrade, failed migration, permissions ak restore. |
| S2-D | Backup encryption/signing si apwouve | Aplike sèlman metòd/key policy apwouve; tès backup tamper/confidentiality, key loss, restore sou aparèy sipòte. |
| S2-E | Retention/backup operations | Règleman otomatik oswa manyèl, pwoteksyon copy, espas/disk-full/error notification, audit efasman; teste expiration/retention. |
| S2-F | Recovery drill | Backup valab, invalid/corrupt/tampered/interrupted restore, pre-restore snapshot, staging, rollback, restart apre restore, done konplè; repete sou anviwònman pilòt. |
| S2-G | Operator runbook | Ekri kote dosye, backup/restore, failure, eskalasyon, owner, RPO/RTO, done nan fen pilòt, kontak sipò; yon lòt operatè swiv li san enstriksyon oral. |

**Gate Etap 3:** policy siyen/apwouve; protect data/key plan valide; migrations ak restore/rollback/restart passe; recovery drill ak runbook gen prèv; kliyan dakò sou done ak responsablite. Pa antre done manm reyèl anvan gate la.

### ETAP 4 — Pewòl, taks ak validasyon jiridiksyon (S3; pa nan pilòt aktyèl)

**Objektif:** pa vann ni prezante payroll/taks kòm final san otorizasyon legal espesifik pou chak jiridiksyon.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S3-A | Defini jiridiksyon ak egzijans payroll | Peyi, frekans, currency, kategori travayè, earnings/deductions, bracket/threshold/exemption, kontribisyon employer/employee, rounding, dat efektif, sous otorite. |
| S3-B | Revizyon sous pa espesyalis kalifye | Kontab/tax specialist lokal verifye sous ofisyèl ak entèpretasyon; ekri siyati/apwobasyon ak fixtures kalkil; san sa, taks rete NotConfigured/disabled epi STOP. |
| S3-C | Tax-engine contract | Verifye rule engine aktyèl kont brackets/exemptions/contributions/ceilings/rounding/effective dates; defini fail-safe; okenn rate pa devine/hard-code. |
| S3-D | Implementasyon yon jiridiksyon/yon règ nan chak task | Rule version/source link ak fixtures espesyalis apwouve; tès deterministic ak edge cases; audit trace; pa mete plizyè peyi ansanm. |
| S3-E | Payroll data integrity/lifecycle | Reconcile salary/components/tax/deduction, eligibility/recalculation, stale entries, immutable Paid/Closed, correction/void ak atomic audit; tests authorization/concurrency/state. |
| S3-F | Payroll product acceptance | Accountant/customer acceptance, payslip/report permissions, fixtures, statements ak feature release gate pou peyi sa sèlman. |

**Gate Etap 4:** sous legal ofisyèl + espesyalis siyen; rule fixtures ak kalkil teste; correction/audit/authorization verifye; release note/kontra site peyi ak peryòd egzak. Sinon payroll/tax rete deyò pilòt ak tout reklamasyon lavant.

### ETAP 5 — Rapò, ekspòtasyon ak lokalizasyon (S4)

**Objektif:** bay kliyan rapò ki itil ak lang/format mache sib la mande.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S4-A | Mache, rapò ak data-export contract | Chwazi mache/lang, rapò, filtè/kolòn, role access, PII minimization, currency/date/timezone, CSV delimiter/encoding, confidentiality markings. |
| S4-B | Localization foundation | Resource files, lang chooser/fallback, format dates/numbers/currency/time, plurals, font/diacritics; localization test harness. |
| S4-C | Haitian Creole strings | Lokalizasyon workflow ak glossary; revizyon Creole speaker/customer; resource tests. |
| S4-D | French strings | Lokalizasyon ak review native/proficient reviewer; locale snapshots/date-number tests. |
| S4-E | English strings | Lokalizasyon ak review; locale snapshots/accessibility label tests. |
| S4-F | CSV export | Yon report task pa fwa; escaping, formula-injection prevention, encoding, locale/date, permissions, large dataset. |
| S4-G | XLSX export | Cell types, formula-injection prevention, metadata/confidentiality, permissions, output tests. |
| S4-H | PDF/print | Layout/page breaks, fonts/Creole/French characters, metadata, print preview, Windows printer behavior; visual verification. |
| S4-I | Integrated report acceptance | Filtè, pagination/performance, export parity, permissions, screen reader/keyboard, privacy fields, regression. |

**Gate Etap 5:** mache ak lang ki an kontra a pase review; chak export teste ak pa ekspoze done san dwa; PDF/print vizyèlman verifye sou Windows; currency/date formats apwouve.

### ETAP 6 — Enstalasyon, mizajou, lisans, vi prive ak sipò (S5)

**Objektif:** fè distribution/install/update/uninstall kapab repete, siyen ak sipòte.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S5-A | Desizyon distribution | Portable/installer, target Windows versions, per-user/per-machine, data path, code-signing owner, update/rollback/uninstall, DB retention, support window. |
| S5-B | Version/build/release metadata | Product version, build ID, artifact hash/signature, release notes, reproducible evidence. `0.9.0-pilot` vin ofisyèl sèlman apre li mete nan metadata epi apwouve. |
| S5-C | License/third-party notices | Lisans pwodwi, packages/fonts/third-party notices, legal review; pa distribye san attribution nesesè. |
| S5-D | Installer clean install | Pake dapre desizyon, permissions, shortcuts, first start/first admin, data/log paths, uninstall entry. |
| S5-E | Upgrade/rollback/uninstall | Vèsyon anvan→nouvo, migration fail, rollback, data preservation/deletion dapre policy, backup/restore apre upgrade. |
| S5-F | Privacy/support/operations docs | Privacy notice, retention/deletion, data ownership, support contact/hours, diagnostics, incident/recovery guide, known limitations; review pou mache sib. |
| S5-G | Signed distribution rehearsal | Clean machine, verify signature/hash, install/update/rollback/uninstall, launch ak DB; archive final bundle ak prèv. |

**Gate Etap 6:** clean-machine lifecycle pase; data path ak preservation conforme; artifact siyen/hash verifye; lisans/privacy/support dokiman revize; release notes egzak. Pa pwomèt auto-update oswa SLA si li pa pase.

### ETAP 7 — UI, aksè ak verifikasyon vizyèl Windows (S6)

**Objektif:** konfime workflow yo vizyèlman ak entèaktif nan konfigirasyon Windows kliyan yo.

| ID | Yon sèl travay | Rezilta ak tès/verifikasyon |
|---|---|---|
| S6-A | UI test matrix | Windows versions, resolution/DPI scaling, language, keyboard-only, focus order, screen reader/contrast, state/error cases, user roles. |
| S6-B | Login/shell workflows | First launch, login/logout, lock/re-auth/session expiry, sidebar/navigation, errors/loading/empty states. |
| S6-C | Core library workflows | Books, Members, Borrow/Return/Fine, Reservations, dashboard, reports, settings, audit; record actual clicks/results. |
| S6-D | Backup/restore UI | Confirmation, wrong/corrupt files, recovery, restart after restore, error message quality and data check. |
| S6-E | One accessibility/scaling defect per task | Each finding receives own AUDIT→PLAN→ONE TASK… cycle and regression test; no batch of unrelated UI fixes. |
| S6-F | Candidate UI regression | Repeat matrix on exact signed/release artifact; screenshots/checklist, supported/unsupported DPI and accessibility findings. |

**Gate Etap 7:** zero open critical UI/data-loss/accessibility issue for the contracted workflow; all high residuals accepted; interactive artifacts and environment documented. Headless app startup alone is not UI verification.

### ETAP 8 — Pilòt siyen, acceptance ak desizyon vant (C1–C4)

**Objektif:** verifye scope la ak yon kliyan kontwole, epi pran yon desizyon apa pou vant jeneral.

1. C1: pase done/privacy/recovery gate yo; operatè kapab swiv runbook; customer/product owner siyen responsablite done/recovery. Pa gen done manm reyèl anvan sa.
2. C2: pase security/account ak interactive workflow review; users/roles nome; lockout/reset/MFA gaps dokimante epi kliyan akseptelimit yo.
3. C3: akò pilòt siyen — yon bibliyotèk, yon òdinatè, operatè nonmen, pa gen payroll/tax, pa gen DB pataje an rezo; dire, workflow, success/stop criteria, sipò, incident reporting, done exit/delete, training ak sign-off.
4. Kouri pilòt la; issue log, training, recovery exercise, customer acceptance; kritikal = 0 anvan fini.
5. C4 general-sale decision apa: security/privacy, legal, localization/exports pou mache a, packaging, accessibility, recovery, performance, support ak artifact current evidence tout pase; owner apwouve alekri.

**Gate Etap 8:** pilot success ak dokiman siyen pa egal general sale. Vant jeneral mande C4 apa, zewo critical finding, high issues fèmen oswa aksepte alekri, epi release-specific proof. Payroll/tax separe jiskaske Etap 4 pase pou chak jiridiksyon.

## 4. Task ledger — estati aktyèl ak handoff

| ID | Etap | Estati | Prèv / pwochen kondisyon |
|---|---:|---|---|
| C0-01 | 1 | PASS (workaround) | SDK 10.0.302/win-x64; restore chak `.csproj` exit 0; solution restore exit 1 san erè eksplisit. |
| C0-02 | 1 | PASS (workaround) | App ak tests Release build dirèk, 0 warning/0 error; solution build exit 1 san diagnostics. |
| C0-03 | 1 | PASS | Full tests 68/68 pass, 0 fail/0 skip sou test project dirèk. |
| C0-04 | 1 | PARTIAL/PASS WITH LIMIT | EF Release: no pending model changes; build diagnostics 0 warning/error. `dotnet format` pa t konekte BuildHost NamedPipe (`UnauthorizedAccessException`). |
| C0-05 | 1 | BLOCKED | Self-contained Win-x64 publish ak temp-folder launch sou menm machin nan pase; clean-machine pa verifye. Hash: `C11BA27DF56CA6F118857757D8BA21F082B6AC734863E71C968B445FDED912F7`. Nan verification 2026-10-03, `WindowsSandbox`, Docker, Podman, VM tools pa t disponib nan PATH; machin Windows 10 Pro a pa t bay sandbox/VM runtime ki ka lanse san elevation. `NU1900`: NuGet vulnerability endpoint pa t disponib. Prèv clean install/launch sou VM pwòp la toujou manke. |
| C0-06 | 1 | PASS | Gap inventory verifye kont code/security docs; pa gen doublon ak authentication/authorization sa ki deja nan source. Pwopriyetè a te mande kontinye malgre C0-05; C0 pa make pase. |
| S1-A | 2 | PASS | Pwopriyetè a mande pou fini Etap 2; rekòmandasyon ki nan `ACCOUNT_SECURITY_POLICY_PROPOSAL.md` sèvi kòm implementation baseline. |
| S1-B | 2 | PASS | Account failed-login lockout, progressive delay, audit events, migration `20261003181727_AddAccountLoginProtection`; enkli nan regression suite 82/82. `NU1900` rete package vulnerability verification warning. |
| S1-C | 2 | PASS | Own password change, admin-assisted temporary reset, forced password change, protected routes, no secret in audit; migration `20261003183257_AddPasswordLifecycle`; authz/service tès pase. |
| S1-D | 2 | PASS | Persisted `RolePermission`, migration `20261003184406_PersistRolePermissions` (kreye kòm migration annapre; verifye li pa repete security columns deja migre), default grants Administrator/Librarian ak deny-by-default custom roles; SQLite migrations ak login tests pase; EF Release reports no pending model changes. |
| S1-E | 2 | PASS | Role administration service + WPF page; create custom roles, grant permissions from closed catalog, audit changes; built-in Administrator ak Librarian role/grants pwoteje. Direct service tests cover persistence, audit, unknown grants ak built-in protections. |
| S1-F | 2 | PARTIAL — rete sou gate | Re-audit 2026-10-08: `AuthorizationServiceTests.PersistedDefaultAndCustomRolesMatchTheCompletePermissionMatrix` verifye tout `Permissions.Catalog` pou Administratè (tout), Bibliyotekè (grant migration yo), ak wòl koutim vid (deny-by-default); li verifye tou aksè pozitif/negatif pou chak permission. `InactiveCurrentEmployeeCannotAuthorize` teste refi anplwaye inaktif la. `RestrictedRoleCannotBypassServiceAuthorizationByCallingServicesDirectly` kounye a teste 62 direct service calls: aksyon san grant yo dwe refize, pandan `Books.View` pèmèt query liv la. Lòt tès yo verifye deny sou sèvis ak workflows Payroll, Company, Employee, TaxRule, Backup/Audit. Suite Release apre chanjman an: 84/84 pase, 0 echèk/sote. Sa bay kouvèti automated sou permission matrix ak service boundaries; li pa ranplase WPF/keyboard/menu entèaktif. Aplikasyon an pa t bay okenn fenèt targetable nan Computer Use, menm apre lansman; API native-app la pa disponib nan runtime sa a. S1-F rete pasyèl jiskaske workflow WPF entèaktif la verifye sou Windows. |
| S1-G | 2 | PARTIAL — sesyon/reauth aplike; WPF lifecycle poko verifye | 2026-10-08: baseline apwouve a presize 15 min idle, 8h max, lock/unlock ak modpas pou kont lan. Timeout mete sesyon an an lock; `LoginWindow` mande menm kont aktif la, epi app la fèmen si lock dialog la fèmen san unlock. Aksyon sansib ki deja idantifye nan `SECURITY_AUDIT.md` a se apwobasyon payroll: UI mande modpas, sèvis la verifye li ak hash aktyèl la, ekri `ReauthenticationSucceeded/Failed` ak `Purpose=PayrollApprove` san modpas, epi service boundary la konsome grant reauth la yon sèl fwa anvan apwobasyon. Tès verifye modpas kòrèk/enkòrèk, apwobasyon san grant, grant yon sèl itilizasyon, epi absans modpas nan audit. Full Release suite pase 88/88. WPF entèaktif, app sleep/resume/restart, ak tès fen-a-fen ekran reauth la poko verifye. Policy a pa idantifye lòt aksyon sansib pou mande reauth; pa envante lòt mapping. Gate S1-G rete pasyèl. |
| S1-H-D | 2 | PASS — desizyon ak dokimantasyon sèlman | 2026-10-08: D1 TOTP ofisyèl; D2 MFA Administratè obligatwa anvan General Release; D3 one-time recovery codes pwodwi lè MFA aktive epi konsève offline an sekirite; D4 lost/replaced device invalid ansyen secret, mande re-enrollment nouvo authenticator, epi pwodwi nouvo recovery codes; D5 si aparèy ak tout recovery codes pèdi, pa gen bypass teknik, administrative recovery dwe verifye idantite/otorite; D6 enrollment/recovery/reset/re-enrollment ak lòt aksyon MFA sansib dwe auditables san sekrè nan log. Kòd, schema, UI, dependency ak tès pa chanje. Implementation rete separe epi depann de S2-B pou secret/key protection ak ownership. |

**Kote travay la kanpe (verifye 2026-10-08):** Etap 2 pa fin gate. Pwopriyetè a te bay enstriksyon eksplisit pou kontinye S1-G pandan S1-F rete pasyèl; sa pa fè S1-F pase ni waive verifikasyon li. Full suite Release pase 88/88 (rezilta anrejistre anvan S1-H-D; suite pa relanse nan task sa a); EF pa gen chanjman modèl san migration (dènye prèv istorik nan ledger C0-04). S1-F automated matrix la verifye tout grants pou twa profil wòl ak 62 dirèk aksyon sèvis; li rete pasyèl paske WPF/keyboard/menu workflow pa t ka verifye entèaktif. S1-G aplike idle/max lock, menm-kont unlock, ak reauth modpas pou apwobasyon payroll (yon grant yon sèl itilizasyon); unit tests pase. UI entèaktif, sleep/resume/restart ak fen-a-fen ekran reauth la pa verifye paske Computer Use pa ekspoze aplikasyon/fenèt Windows nan sesyon sa a. S1-H-D desizyon D1–D6 pase; MFA implementation rete tann S2-B key/secret protection. Pou C0-05, executable sandbox/VM tools yo pa enstale; Windows feature query mande elevation epi elevation pa t disponib, kidonk clean-machine proof la pa ka fèt nan anviwònman sa a. Pa deklare C0 oswa Etap 2 gate pase, epi pa kòmanse pilòt/vant jeneral sou baz rezilta sa yo sèlman.

## 5. Apwobasyon ak depandans ki pa dwe envante

- S1-A account-security policy apwouve pou baseline: 5 move esè/15 minit, progressive lockout jiska 60 minit; admin-assisted reset ak idantite verifye; 15-min idle/8-hour maximum; MFA obligatwa pou admin anvan general sale; custom roles ak deny-by-default, pa ka retire dènye admin; pa gen auto-purge security events jiskaske privacy policy defini retention.
- Data storage/encryption/retention/RPO/RTO, installer/signing, recovery owner ak privacy obligations bezwen desizyon/apwobasyon ki koresponn ak dosye PH2 yo. Pa fabrike chif legal/operasyonèl.
- Tax Haiti oswa lòt peyi mande sous legal otorite + espesyalis lokal. San yo, kite taks unconfigured/disabled.
- Clean-machine, WPF interactive, accessibility/DPI ak release signature mande Windows environment/prèv fizik; yon terminal headless pa konfimasyon.

## 6. Relasyon ak lòt dokiman

`planfinal.md` se kote aktyèl la pou lòd ak scope 8 etap yo. `COMMERCIAL_SALE_AND_PILOT_PLAN.md` rete deklarasyon scope lavant/pilòt la; `COMMERCIAL_READINESS_ROADMAP.md` se orijin gate C0–C4 yo; `ACCOUNT_SECURITY_POLICY_PROPOSAL.md` se detay draft S1-A. Si enfòmasyon estati oswa kondisyon aksepte chanje, mete **planfinal.md** ak task ledger la ajou menm jou a.
