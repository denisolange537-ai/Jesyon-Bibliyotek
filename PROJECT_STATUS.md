# Eta Pwojè a

## Kontra devlopman komèsyal — mizajou 2026-10-08

Dokiman kontra ki gouvène pwochen travay la se [`planfinal.md`](planfinal.md), ak limit pwodwi/akseptasyon nan [`COMMERCIAL_SALE_AND_PILOT_PLAN.md`](COMMERCIAL_SALE_AND_PILOT_PLAN.md). M15 anba a se eta istorik scope aplikasyon an; li pa vle di pwodwi a pare pou vann.

Plan travay etap pa etap la ak ledger aktyèl la nan [`planfinal.md`](planfinal.md). `COMMERCIAL_WORK_PLAN.md` se achiv istorik. Swiv sik AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP; mande “kontinye” nan fen chak etap.

- **Etikèt build kandida:** `0.9.0-pilot` (pwovizwa; poko antre nan metadata build).
- **Scope pilòt:** Jesyon Bibliyotèk debaz sèlman, Windows x64, yon òdinatè/bibliyotèk; pewòl/taks deyò scope la.
- **Desizyon komèsyal:** pa pare pou vant jeneral; pa kòmanse pilòt anvan C0, C1 ak C2 pase; C3 mande akò pilòt siyen.
- **C0 prèv aktyèl:** restore/build dirèk chak pwojè pase; `.slnx` restore/build bay exit 1 san erè eksplisit. Suite Release pase 88/88 (2026-10-08). C0-04 EF Release pase; compiler 0 warning/erè; `dotnet format` UNVERIFIED (BuildHost NamedPipe `UnauthorizedAccessException`). C0-05 publish self-contained win-x64 ak launch nan temp pase. Clean-machine gate poko pase; `NU1900` montre NuGet vulnerability data pa t ka jwenn. C0-06 inventory pase sou demann pwopriyetè a. C0/C1 release gate rete BLOCKED; detay yo nan `planfinal.md`.
- **Etap aktyèl (2/8):** S1-A rive S1-E pase. S1-F automated permission matrix la verifye tout katalòg la pou Administratè/Bibliyotekè/wòl koutim, ak 62 apèl dirèk nan service boundary pou wòl restrenn; tès anplwaye inaktif pase. UI WPF/keyboard/menu entèaktif la poko verifye, kidonk S1-F rete pasyèl. Sou enstriksyon pwopriyetè a, travay la avanse sou S1-G san li pa make S1-F pase. S1-G aplike lock 15 min idle/8h max, reauth menm kont lan pou unlock, fèmen app la si unlock pa fèt, epi pwoteje apwobasyon payroll ak modpas ak yon grant yon sèl itilizasyon. Rete verifikasyon WPF entèaktif, sleep/resume/restart ak fen-a-fen ekran an; Computer Use pa montre okenn aplikasyon Windows (`apps: []`) nan sesyon sa a. Suite Release ki te kouri anvan S1-H-D: 88 tès, 0 echèk, 0 sote; pa relanse nan task documentation-only sa a. S1-G rete PARTIAL. S1-H-D D1–D6 (TOTP, mandatory Admin MFA, one-time codes, lost-device/reset flow, no bypass, auditability) dokimante/apwouve 2026-10-08; MFA implementation tann S2-B secret/key-protection decision ak task apa. Gade `planfinal.md` pou task ledger konplè a.
- **Kondisyon egzak:** wè seksyon C0–C4 nan `COMMERCIAL_READINESS_ROADMAP.md`.

## Milestone ki te deja livre nan scope orijinal la

M15 — Release (konplete)

## Konplete

- M0 — Enspeksyon depo a
- M1 — Fondasyon: aplikasyon WPF, DI, konfigirasyon, logging, ak pwojè tès
- M2 — Bazdone: SQLite, EF Core, `LibraryDbContext`, migration inisyal, ak initializer
- M3 — Otantifikasyon: hash modpas, first-run admin, login, session, ak logout
- M4 — Shell/UI: sidebar, header, navigasyon, tèm, itilizatè, ak logout
- M5 — Books: SQLite, CRUD, rechèch, validation, deactivate, audit log, ak tès
- M6 — Manm: SQLite, CRUD, rechèch, validation, deactivate, audit log, ak tès
- M7 — Borrowing: règ prete, due date, transaction, audit, UI, ak tès workflow
- M8 — Retou + Amann: retou, kalkil reta, fine, peman, audit, UI, ak tès
- M9 — Rezèvasyon: kreye/anile, fulfillment pandan retou, audit, UI, ak tès
- M10 — Dashboard: estatistik Books/Members/Borrowing/Fines ki soti nan SQLite
- M11 — Rapò: endikatè bibliyotèk, prete anreta, jou anreta, amann ak rezèvasyon ki soti nan SQLite; View, navigation ak tès entegrasyon
- M12 — Paramèt: non bibliyotèk, jou prete, amann pa jou ak tèm ki pèsistan nan SQLite; règ prete/amann yo sèvi ak paramèt yo
- M13 — Backup + Restore + Audit: backup/restore SQLite ak konfimasyon, audit viewer ak tès entegrasyon
- M14 — Polish: rakousi klavye, Enter sou login, non aksesib pou kontwòl yo, ak loading settings ki rezistan
- M15 — Release: clean, restore, build Release, self-contained win-x64 artifact; current regression evidence is 68/68 tests PASS

## An pwogrè

- Pa gen etap an pwogrè.

## Poko kòmanse

- Pa gen lòt milestone nan kontra orijinal M0–M15 la. Kontra komèsyal aktyèl C0–C4 la antre an vigè ak mizajou 2026-10-03 anlè a.

## Pwoblèm yo konnen

- Verifikasyon fenèt la fèt atravè pwosesis demaraj la; pa gen sesyon entèaktif pou enspekte UI a manyèlman nan anviwònman sa a.
- Warning `NU1903` la rezoud lè pwojè yo pin `SQLitePCLRaw.lib.e_sqlite3` sou 2.1.13.
- Verifikasyon UI entèaktif pa disponib nan anviwònman tès sa a.
- Verifikasyon restart UI a rete pou yon sesyon Windows entèaktif; tès otomatik yo itilize SQLite reyèl.
- Tès UI entèaktif la toujou mande yon sesyon Windows ki gen fenèt.
- Retou apre restart ak klike UI pa teste nan sesyon san fenèt la; sèvis ak persistence SQLite teste otomatikman.
- Login ak fenèt Rapò yo bezwen konfimasyon manyèl nan sesyon sa a.
- Restore mande rekòmanse aplikasyon an pou shell ki deja chaje yo itilize done restore yo.

## Dènye build ki reyisi

- `dotnet build GestionBibliotheque.slnx -c Release --no-restore`: 2026-09-05, pase san warning rapòte.

## Dènye tès ki reyisi

- `dotnet test GestionBibliotheque.slnx -c Release --no-build`: 2026-09-05, 68 tès pase, ladan yo payroll, security, settings, backup/restore ak audit sou SQLite reyèl.

## Release artifact

- `src/GestionBibliotheque/bin/Release/net10.0-windows/win-x64/publish/GestionBibliotheque.exe`
- Publish self-contained Windows x64: 2026-08-22, pase san warning NuGet.

## Phase 1 — Employees and authorization foundation

Phase 1 foundation complete on 2026-08-26. Employee identity, User-to-Employee linkage, first-admin compatibility, active-state authentication checks, role permission mapping, service-level authorization for existing operational writes, Employee CRUD UI, account/role assignment UI, dashboard identity, EF migration, and tests were added. Phase 1.5 deep validation also protected read/sensitive boundaries, shell navigation, backup/restore and inactive current sessions.

Phase 2 Company Management Foundation is implemented on 2026-08-26: single Company profile, normalized Department and Position entities, Employee foreign-key integration with legacy fields preserved, service-level authorization/audit, shell navigation, migration and tests. The older note that Payroll had not started is historical; payroll foundations were added later, but payroll/tax remains outside the current pilot/sale contract until jurisdiction-specific expert validation and separate acceptance.

Payroll foundations and Phase 1 hardening tasks are implemented and tested. Salary history, PayrollPeriod lifecycle, PayrollEntry/Component persistence, authoritative payroll calculation, PayrollPeriod concurrency token, versioned configurable TaxRule structure, finalized-only payslips, payroll summary reports, authorization, audit, backup safety and security regressions are implemented. Verified Haitian tax rules, payroll export, advanced reports, installer/distribution and full commercial sign-off are not complete.

Known follow-up: persisted Role/Permission management, lockout/MFA/password lifecycle, export/localization, privacy/encryption policy, installer/distribution, controlled payroll correction and legal tax verification.
