# Plan travay etap pa etap — dokiman istorik

> **Ranplase:** [`planfinal.md`](planfinal.md) se sèl sous aktyèl pou plan 8 etap yo, estati a ak task ledger la. Pa egzekite oswa modifye plan ansyen an; konsilte li sèlman pou detay istorik.

---

**Dat:** 2026-10-03  
**Estati:** plan sèlman; okenn etap aplikasyon nan plan sa a poko kòmanse oswa pase.  
**Kontra ki gouvène li:** [`COMMERCIAL_READINESS_ROADMAP.md`](COMMERCIAL_READINESS_ROADMAP.md) ak [`COMMERCIAL_SALE_AND_PILOT_PLAN.md`](COMMERCIAL_SALE_AND_PILOT_PLAN.md).

## Objektif ak règ travay

Rezoud blokaj komèsyal yo an etap ki ka revize epi aksepte apa. Pilòt aktyèl la rete limite ak jesyon bibliyotèk sou yon òdinatè Windows; pewòl ak taks rete deyò vant/pilòt jiskaske validasyon pwofesyonèl ak yon gate apa.

Pou **chak etap** epi pou **chak travay endividyèl** anndan etap la, swiv lòd sa a:

**AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP**

- **AUDIT:** li source, tès ak dokiman ki konsène travay sa a; konfime gap la toujou egziste; idantifye risk, depandans ak ka ki deja kouvri. Pa repete fonksyonalite ki deja fèt.
- **PLAN:** ekri chanjman minimòm, fichye ki pral touche, tès ak prèv akseptasyon. Fikse nonmanifoulman sa travay la pap chanje.
- **ONE TASK:** chwazi yon sèl livrezon presi pou sik sa a. Pa melanje de fonksyon endepandan anba yon sèl travay.
- **IMPLEMENT:** fè sèlman travay sa a. Pa avanse sou pwochen travay la pandan implementasyon an.
- **TEST:** kouri tès ki espesifye pou chanjman an ansanm ak tès regresyon ki nesesè; anrejistre kòmand, sòti ak rezilta.
- **VERIFY:** enspekte diff la, sekirite, migrations/done konpatibilite, UI/operasyon ak dokiman selon sa ki touche. Tès ki pa posib rete UNVERIFIED, yo pa konte kòm PASS.
- **GATE:** bay PASS/FAIL/BLOCKED ak lyen/prèv; verifye kritè acceptance ki anba a.
- **STOP:** sispann lè gate la pa pase oswa gen desizyon legal/biznis/teknik ki poko apwouve. Pa kòmanse pwochen travay/etap. Lè li pase, make li pase epi tann revizyon/akseptasyon avan travay apre a.

Yon etap ka genyen plizyè travay, men chak travay pase tout sik la endividyèlman. Pa fè plizyè travay endepandan an menm tan. Kenbe yon task ledger ak ID inik, estati, scope, prèv ak desizyon, konsa menm travay la pa repete san rezon. Chanjman scope oswa nouvo finding antre kòm yon nouvo travay, pa ajoute an silans nan travay ki deja ap egzekite a.

## Lòd etap yo

### Etap 0 — Re-etabli baz prèv komèsyal la (C0)

**Objektif:** konnen ki build ak konpòtman aktyèl n ap amelyore.

1. Verifye restore, Release build, suite tès konplè, EF pending-model check, diagnostics, ak publish win-x64; kolekte kòd sòti ak output.
2. Si kòmand yo rete san sòti, detèmine ak dokimante blokaj la kòm yon travay apa; pa make li pase sou baz rezilta istorik.
3. Verifye artifact kandida a ouvri sou Windows x64 pwòp; ajoute build ID, hash ak release notes.
4. Mete ledger la ak inventory gap ki aktyèl pou etap sekirite, done, payroll, rapò/lang, deployment ak UI.

**Gate:** prèv aktyèl pou build/tès/model/publish ak clean-machine; tout gap komèsyal gen ID ak etap. Yon echèk oswa kòmand ki pa verifye bloke antre nan etap aplikasyon yo.

### Etap 1 — Sekirite kont ak otorizasyon

**Objektif:** kontwòl kont ki apwopriye pou done bibliyotèk ak anplwaye, san kraze service authorization ki egziste deja.

Travay endividyèl yo, nan lòd depandans apre audit chak youn:

1. **S1-A — Kontra kont:** ekri politk lockout/throttling, chanjman/rekiperasyon modpas, session expiration, wòl, pwoteksyon dènye administratè a, ak audit evènman sekirite. Desizyon biznis/operatè ki manke yo bloke implementasyon yo.
2. **S1-B — Failed-login protection:** konsepsyon, implementasyon ak tès kont failed-login throttling/lockout ak reset ki an sekirite.
3. **S1-C — Password lifecycle:** implementasyon chanjman modpas ak recovery/reset atravè workflow administratè ki verifye idantite, san log oswa ekspoze sekrè.
4. **S1-D — Persisted RBAC design/schema:** konsepsyon role-permission ki gen migration konpatib ak role mapping aktyèl; gate migration apa anvan sèvis yo sèvi ak nouvo modèl la.
5. **S1-E — RBAC admin workflow:** UI/service pou bay/retire pèmisyon, validate kont yo, epi bloke eskalasyon oswa administrator self-lockout.
6. **S1-F — Matrix verification:** teste tout pèmisyon/action boundary, dirèk service call, inactive user/employee, sensitive reads ak evènman audit.

**Gate:** tout tès pozitif/negatif ak role/action matrix pase; pa gen privilege escalation; migration pwòp ak upgrade pase; UI WPF apwouve nan sesyon entèaktif. Sinon STOP.

### Etap 2 — Konfidansyalite, backup, restore ak rekiperasyon

**Objektif:** fikse politik done ak pwouve kliyan an ka retabli sistèm nan san pèdi done.

1. **S2-A — Apwobasyon politik done:** defini done ki antre, kote DB/log/backup yo ye, aksè, owner, retention/deletion, frekans backup, RPO/RTO kliyan an dakò ak, ak responsablite. Jwenn revizyon legal/vi prive kote sa nesesè. Pa envante obligasyon legal.
2. **S2-B — Menas ak encryption/key decision:** chwazi threat boundary, pwoteksyon Windows/at-rest, si DB/backup encryption nesesè, epi key recovery/rotation/owner. Pa chwazi package ni kòmanse code anvan desizyon konsepsyon pase.
3. **S2-C — Database/data-location implementation:** aplike desizyon done yo ak migration done itilizatè yo si chemen aktyèl la deplase; teste upgrade ak prezèvasyon `library.db`.
4. **S2-D — Backup confidentiality:** aplike encryption/signing sèlman dapre desizyon apwouve a; teste kle pèdi/restore sou aparèy ki sipòte a.
5. **S2-E — Retention and backup operation:** implementasyon règleman retansyon, verifikasyon backup, ak notifikasyon erè si yo nan scope desizyon an.
6. **S2-F — Recovery drill:** egzèse backup valab, backup envalid/tampered/interrupted, pre-restore snapshot, rollback, restart apre restore, ak verifikasyon done.
7. **S2-G — Operator runbook:** ekri kote done yo ye, backup, restore, ensidan, limit, escalation, owner, ak prèv drill la.

**Gate:** politik siyen/apwouve; done ak kle yo pa ekspoze pa design lan; migrasyon/backup/restore/rollback ak restart verifye; runbook lan suivab pa yon operatè. Pa mete done kliyan anvan gate la pase.

### Etap 3 — Pewòl/taks ak validation legal (track apa; deyò pilòt aktyèl)

**Objektif:** anpeche nenpòt reklamasyon payroll/taks ki pa legalman jistifye. Etap sa a pa bloke pilòt bibliyotèk la pandan modil la rete andeyò scope, men li bloke tout vant/aktifasyon payroll.

1. **S3-A — Kontra jiridiksyon:** jwenn peyi/jiridiksyon, peryòd, kategori anplwaye, salè, dediksyon, kontribisyon, tax brackets/exemptions, dat efektif, devwa employer/employee, ak sous otorite. Chak pwen mande source referans.
2. **S3-B — Ekspè otorize:** espesyalis taks/kontab lokal revize sous ofisyèl yo, entèprete yo, epi apwouve egzanp kalkil/règ yo alekri. Si pa gen ekspè oswa sous verifyab, kenbe taks la NotConfigured/disabled epi STOP.
3. **S3-C — Rule-engine design:** konpare règ legal verifye ak kapasite motè aktyèl la; defini brackets, plafon, exemptions, kontribisyon, rounding, effective dating ak failure policy. Okenn rate pa devine.
4. **S3-D — Yon jiridiksyon/yon règ:** implementasyon nan ti travay izole; chak chanjman gen fixtures apwouve, tès deterministic, audit source/version ak regression.
5. **S3-E — Payroll integrity acceptance:** valide eligibility/recalculation, immutable finalized periods, correction/void workflow, authorization ak structured audit avèk accountant/customer.
6. **S3-F — Payroll release decision:** legal + technical sign-off pou jiridiksyon egzak la; separe feature flag/release notes/kontra. Pa jeneralize validasyon yon peyi pou lòt peyi.

**Gate:** sous ofisyèl, revizyon ekspè ekri, fixtures ak tès pase, audit trace ak failure policy verifye, epi apwobasyon jiridiksyon an anrejistre. San yo, payroll rete andeyò pwodwi ki vann lan.

### Etap 4 — Rapò komèsyal ak lokalizasyon

**Objektif:** delivre rapò itil ak lang ki nesesè pou mache sib la, san chanje kalkil done sous yo.

1. **S4-A — Kondisyon mache/rapò:** chwazi mache/lang, rapò, filtè, done ekspòte, confidentiality markings, konvansyon dat/nimewo/lajan ak zòn lè. Fikse CSV encoding/delimiter pou mache a.
2. **S4-B — Localization foundation:** mete strings nan resource files, lang switch, fallback, date/number formatting, ak pluralization dapre konsepsyon apwouve a.
3. **S4-C — Haitian Creole:** lokalize epi revize tèks UI kreyòl ak glossary konsistan.
4. **S4-D — French:** lokalize epi revize tèks UI franse.
5. **S4-E — English:** lokalize epi revize tèks UI anglè.
6. **S4-F — CSV export:** defini kolòn/filtè/encoding/data minimization; aplike export pou rapò bibliyotèk la epi teste escaping, formula injection ak dat/karaktè.
7. **S4-G — XLSX export:** teste workbook structure, encoding/types, confidentiality markings ak absans formula injection.
8. **S4-H — PDF/print:** defini layout, paginasyon, metadata, font/karaktè kreyòl/franse, preview/print workflow; teste sou Windows.
9. **S4-I — Report verification:** tès snapshot locale, currency/date, gwo volim/paginasyon, permissions ak accessibility.

**Gate:** se sèlman lang/rapò ki antre nan scope kliyan an ki dwe pase; export yo pa dwe bay plis done pase pèmisyon itilizatè a; dokiman pwodiksyon yo vizyèlman verifye. Si mache/lang poko deside, rete nan audit/plan pou travay sa a.

### Etap 5 — Packaging, lisans, vi prive ak sipò

**Objektif:** fè pwodwi a enstale, ajou, retire, epi jwenn sipò san pèdi done kliyan.

1. **S5-A — Deployment decisions:** apwouve installer/portable, data path, scope itilizatè/machin, signing owner/certificate, upgrade/rollback/uninstall, preserve/delete policy.
2. **S5-B — Version/release metadata:** aplike semantic version/build ID nan artifact, change log, third-party notices/license, ak artifact hashing/signature.
3. **S5-C — Installer:** implementasyon yon pake selon desizyon an; clean install, permissions, app launch, data directory.
4. **S5-D — Upgrade/rollback/uninstall:** tès vèsyon anvan→nouvo, migration echwe, rollback, uninstall, epi done yo rete selon règleman apwouve a.
5. **S5-E — Customer documents:** user/operator guide, license, third-party notices, privacy/retention statement, support contact/hours, diagnostics/incident steps, known limits.
6. **S5-F — Distribution rehearsal:** siyen artifact la, verifye signature/hash, teste sou clean Windows x64 san zouti dev yo, epi achive release bundle.

**Gate:** enstalasyon clean-machine, upgrade, rollback ak uninstall pase; bazdone rete oswa efase selon chwa kliyan ki dokimante; signature/hash verifye; dokiman legal/operasyon revize. Pa pibliye si youn echwe.

### Etap 6 — Validasyon vizyèl, aksesibilite ak UI regression

**Objektif:** verifye WPF workflow yo ak itilizatè/ekipman Windows aktyèl.

1. **S6-A — Test matrix:** chwazi Windows versions, DPI/scaling, rezolisyon, keyboard-only, focus order, screen reader/contrast, UI language ak senaryo workflow.
2. **S6-B — Login/shell:** teste login/logout, premye demaraj, navigasyon, session timeout/security, error/empty/loading states.
3. **S6-C — Library workflows:** teste Books, Members, Borrowing, Returns/Fines, Reservations, Dashboard, Reports, Settings ak audit.
4. **S6-D — Backup/restart:** teste backup/restore ak restart, fenèt erè/failure handling ak rezilta rekiperasyon.
5. **S6-E — Accessibility/scaling fixes:** chak defo se yon sèl task ak menm sik konplè a; pa anpile plizyè fiks nan yon sèl sik.
6. **S6-F — Regression pass:** re-egzekite matriks la sou artifact kandida a; dokimante unsupported combinations.

**Gate:** pa gen defo kritik nan workflow in-scope; klavye, focus, DPI ak UI scale pou machin sipòte yo anrejistre; aksè/kontras ak screen-reader rezilta yo make PASS oswa limit akseptab ekri. Headless startup pa ranplase tès vizyèl.

### Etap 7 — Pilòt kontwole ak desizyon vant jeneral (C1–C4)

Etap sa a rasanble gates yo nan kontra komèsyal la; li pa ranplase travay teknik etap 1–6.

1. Pase C1 data/recovery ak C2 account/interactive workflow anvan okenn done kliyan antre.
2. Siyen C3 pilot agreement: yon bibliyotèk, yon òdinatè, operatè nonmen, pa gen payroll/taks, pa gen pataj DB rezo; dire/success/stop criteria, sipò, done nan sòti.
3. Fè training, issue triage, recovery exercise ak customer acceptance; kenbe prèv.
4. Pran C4 general-sale decision apa apre etap nesesè yo fini; zewo critical findings, high risks fèmen/oswa accept pa responsab, ak prèv artifact final.

**Gate:** pilòt pa vle di vant jeneral. Vant jeneral mande apwobasyon release apa. Payroll/taks rete deyò jiskaske Etap 3 pase pou chak jiridiksyon vann lan.

## Task ledger pou kontinye travay la

Ledger sa a se pou mete estati chak travay yon sèl fwa; mete prèv/lien nan kolòn final la lè sik la fini.

| Task ID | Etap | Travay | Estati | Gate/prèv |
|---|---:|---|---|---|
| C0-01 | 0 | Verifye SDK/dependencies ak restore | PASS (project-level workaround) | SDK 10.0.302/win-x64 pase; restore pou app ak tests csproj pase exit 0. `dotnet restore GestionBibliotheque.slnx` toujou exit 1 san erè/MSB code (diagnostic log: `%TEMP%\gb-restore-diagnostic.log`), donk restore te verifye dirèk sou chak pwojè ki nan solution lan. |
| C0-02 | 0 | Release build | PASS (project-level workaround) | App ak test project build Release dirèk pase, 0 warning/0 error; test project la build app la kòm dependency. Solution build exit 1 san error/warning epi li pa t bay konklizyon sou test project; log detail poko pran. |
| C0-03 | 0 | Full automated test suite | PASS | `dotnet test tests/GestionBibliotheque.Tests/GestionBibliotheque.Tests.csproj -c Release --no-build --no-restore`: 68 passed, 0 failed, 0 skipped. |
| C0-04 | 0 | EF model check ak diagnostics | PASS (with tooling limit) | Release EF check pase: `No changes have been made to the model since the last migration.` Direct Release builds sou app/test yo rapòte 0 warnings/0 errors. Premye EF attempt san `--no-build` te kole; `--no-build --configuration Release` te fini exit 0. `dotnet format --verify-no-changes` pa t ka louvri BuildHost named pipe (UnauthorizedAccessException), kidonk format diagnostics rete UNVERIFIED. |
| C0-05 | 0 | Win-x64 publish ak clean-machine launch | BLOCKED (clean-machine evidence) | `dotnet publish ... -c Release -r win-x64 --self-contained true` pase exit 0 nan `%TEMP%\GestionBibliotheque-C0-05-Publish`; exe demare pandan 8s sou machin aktyèl la. SHA-256 `C11BA27DF56CA6F118857757D8BA21F082B6AC734863E71C968B445FDED912F7`. Pa gen Windows Sandbox/VM runtime disponib; CIM OS query resevwa Access denied. Restore/publish bay `NU1900` paske NuGet vulnerability service pa disponib. **Pwopriyetè a te mande 2026-10-03 pou kontinye C0-06 ak Etap 1 kanmenm**; C0-05 rete pa verifye/pa waive kòm prèv komèsyal, epi C0 overall/pilot/sale gate rete BLOCKED. |
| C0-06 | 0 | Mete gap inventory ak task ledger ajou | PASS | Audite `AuthenticationService`, `AuthorizationService`, `Role`, `SECURITY_AUDIT.md`, `PRODUCT_GAPS.md`, `PHASE_2_ROADMAP.md` ak desizyon D01–D14. Konfime login hash/minimum 12, active account checks, static role-name permission map, no account lockout/change/reset/MFA/session timeout, no persisted permission grants, ak incomplet authorization matrix. Sa pa double sèvis ki deja egziste; S1-A se pwochen travay la. |
| C0-07 | 0 | Fèmen blokaj test/build la si youn nan travay anwo yo pa fini | NOT STARTED (conditional) | kòmanse sèlman si gen blokaj dokimante |
| S1-A | 1 | Apwouve kontra security/account | WAITING FOR OWNER | Audit source/tests/security docs pase; draft policy, defaults, acceptance and approvals checklist nan `ACCOUNT_SECURITY_POLICY_PROPOSAL.md`. Pa gen code ki chanje. Owner decision nesesè anvan S1-B. |
| S2-A | 2 | Apwouve data/recovery/privacy contract | NOT STARTED | — |
| S3-A | 3 | Ranmase jurisdiction sources/requirement (pa aktive tax) | NOT STARTED | — |
| S4-A | 4 | Apwouve mache/rapò/lang | NOT STARTED | — |
| S5-A | 5 | Apwouve deployment/distribution decisions | NOT STARTED | — |
| S6-A | 6 | Apwouve interactive UI test matrix | NOT STARTED | — |

Pwopriyetè a te bay enstriksyon dirèk pou kontinye C0-06 ak Etap 1 pandan C0-05 rete bloke. Sa se yon eksepsyon pou pwogresyon travay sèlman; li pa chanje C0 clean-machine gate la, pa make C0 pase, epi li pa otorize pilòt oswa vant. Lè yon travay pase, anrejistre prèv li; pa kòmanse de travay an menm tan.
