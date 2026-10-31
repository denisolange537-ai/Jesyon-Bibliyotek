# Pwopozisyon kontra sekirite kont ak wòl

**Task:** S1-A — Etap 1 (AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP)  
**Dat:** 2026-10-03  
**Estati:** apwouve pou sèvi kòm baseline Etap 2 dapre enstriksyon pwopriyetè a pou verifye blocker la epi fini Etap 2; **pa yon sètifikasyon sekirite oswa legal**.  
**Prèv:** `Services/AuthenticationService.cs`, `Services/AuthenticationSession.cs`, `Services/AuthorizationService.cs`, `Models/Role.cs`, `docs/SECURITY_AUDIT.md`, `docs/PRODUCT_GAPS.md` ak desizyon PH2.

## 1. Sa ki deja egziste (AUDIT)

- Modpas yo pase nan `PasswordHasher<User>`; plaintext pa estoke. Minimum aktyèl la se 12 karaktè.
- Username lan normalize an lowercase; login refize user oswa Employee ki inaktif.
- First-run admin limite ak bazdone ki pa gen user; logout netwaye session an.
- Sèvis yo gen plizyè `RequirePermission` checks. `Role` estoke non ak Users sèlman; lis otorizasyon an se yon dictionary estatik pa role name nan `AuthorizationService`.
- MFA poko aplike nan kòd. Desizyon ofisyèl S1-H sou metòd, obligasyon Administratè, recovery ak audit te anrejistre 2026-10-08; pwoteksyon secret/key ak implementation rete apa.
- Tès aktyèl yo kouvri hashing/login/inactive user ak kèk denial pa role; yo pa yon matriks konplè role × permission × action.

## 2. Kontra sekirite pwopoze a (PLAN)

| Sijè | Rekòmandasyon pou apwobasyon | Akseptasyon/limit |
|---|---|---|
| Failed login | Apre 5 move esè youn apre lòt sou menm kont la nan fenèt 15 minit, bloke login 15 minit; nouvo move esè apre retou ogmante reta jiska limit 60 minit. Reyajiste kontè a apre login reyisi. | Mesaj login lan pa revele si username egziste oswa si li bloke. Tès concurrency dwe montre esè paralèl pa kontoune limit la. Desizyon final sou nimewo ak fenèt la mande apwobasyon. |
| Throttling | Aplike reta/server-side/local throttle sou tout esè, enkli usernames ki pa egziste, epi kenbe repons tan ak mesaj ki pa fasil pou idantifye kont. | Aplikasyon desktop single-machine pa gen IP/device boundary serye pou rate-limit pa rezo; modèl menas la dwe valide si pita gen pataj LAN. |
| Chanjman modpas | Itilizatè bay modpas aktyèl la; nouvo modpas la respekte minimum aktyèl 12 karaktè, pa menm ak modpas aktyèl la, epi hash la re-sove. Anile lòt sessions (oswa session aktyèl la tou si sa posib san UX danjere). | Pa janm log modpas/temporary secret; re-otantifye pou chanjman ki sansib. Deside si longè 12 ase oswa si kliyan mande règ siplemantè. |
| Modpas bliye/reset | Pa gen self-service pa imèl nan pwodwi offline la. Administratè verifye moun nan atravè pwosedi kliyan an, bay yon kòd/modpas tanporè o aza yon sèl fwa, fòse chanjman nan pwochen login, epi audit aksyon an. | Pa reset modpas san idantite verifye ak otorizasyon; pa ekri temporary secret nan audit/log. Defini chemen si sèl administrator lan pèdi aksè. |
| MFA | **Desizyon S1-H (2026-10-08): TOTP se metòd MFA ofisyèl pou Administratè; MFA obligatwa pou Administratè anvan General Release.** Lè MFA aktive, pwodwi one-time recovery codes Administratè a dwe sere offline nan yon kote an sekirite; chak kòd sèvi yon sèl fwa. Lè aparèy pèdi/ranplase, recovery invalid ansyen secret la, mande re-enrollment nouvo authenticator la, epi pwodwi nouvo recovery codes. Si aparèy ak tout kòd pèdi, pa gen bypass teknik: yon administrative recovery procedure verifye idantite/otorite anvan reset MFA. Enrollment, recovery, reset/re-enrollment ak lòt aksyon MFA sansib dwe auditables san ekri secret oswa recovery code nan log. | Pa gen desizyon S1-H ki fikse obligasyon MFA pou lòt wòl. MFA poko aplike. Secret protection, key ownership/backup, ak workflow administratif egzak la rete nan implementation planning; pa kòmanse implementation anvan depandans security/key design lan pase. |
| Session | Rekòmandasyon: auto-lock apre 15 minit inaktivite; modpas obligatwa pou déverrouiller; max session 8 èdtan oswa logout lè app fèmen. Aksyon sansib mande re-authentication. | Chif yo se default pwopoze, pa politik kliyan deja apwouve. Teste timeout ak UI/thread/lifecycle WPF. |
| Permission catalog | Kenbe yon katalòg permission IDs ki defini nan code; DB kenbe Role-Permission grants, pa strings permission abitrè. Deny by default. Sèvis rete enforcement boundary. | Pa efase permission ki deja nan bazdone san migrasyon/backfill; defini map default Administrator/Librarian/Employee pou upgrade. |
| Role admin | Admin ka kreye/modifye custom roles; sistèm nan pwoteje dènye Administrator ki aktif la ak pa pèmèt yon admin retire tout dwa pa aksidan. Modifye/assign role mande audit. | Pa pèmèt efase permission ki gen assignments san tranzisyon/migrasyon. Deside si built-in roles modifyab oswa yo se templates kopi sèlman. |
| Sekirite audit | Anrejistre success/failure/lockout, reset/change, MFA enroll/reset, account enable/disable, role/permission changes, session timeout/logout ak actor/time. Pa janm anrejistre password, hash, TOTP seed oswa recovery code. | Deside retention/privacy policy ak transaction atomicity avan schema/service final la. |

## 3. Sekans implementasyon apre apwobasyon

S1-A se politik ak desizyon sèlman. Apre pwopriyetè a apwouve rekòmandasyon yo oswa ranplase yo, travay yo rete apa:

1. S1-B — kontè failed-login, lockout/throttle, security events, tès limit/concurrency.
2. S1-C — chanjman modpas ak administratè reset/temporary credential; test recovery ak sekrè nan log.
3. S1-D — design permission catalog, RolePermission persistence, default-role migration/backfill ak upgrade tests.
4. S1-E — role admin UI/service, deny-by-default, admin self-lockout prevention.
5. S1-F — konplete matriks role × permission × action, negative direct-service regression, WPF interactive verification.
6. S1-G — session idle/max lifetime ak reauthentication, ansanm ak tès WPF lifecycle.
7. S1-H-D — desizyon MFA/recovery yo apwouve epi dokimante 2026-10-08. Implementasyon TOTP, secret/key protection, enrollment/recovery UX ak tès yo se task apa apre depandans S2-B pase. Pa gen deferral/risk acceptance anrejistre.

Chak ID travay pase tout sik AUDIT → PLAN → ONE TASK → IMPLEMENT → TEST → VERIFY → GATE → STOP poukont li. Pa konbine S1-B ak S1-C, pa mete migration ak UI nan yon sèl implementasyon, epi pa ale pi lwen pase yon gate ki echwe.

## 4. Kritè TEST / VERIFY / GATE pou S1-A

- **TEST:** cross-check rekòmandasyon yo kont `AuthenticationService`, `AuthorizationService`, `Role`, tès authentication/authorization ak security findings yo; verifye plan an pa pretann gen MFA/RBAC persistence ki poko egziste.
- **VERIFY:** konfime kontra a pa mande chanje existing password hashes yo; pwoteje backward-compatible login/first-run; idantifye migrations, service boundaries, audit events ak end-to-end UI work.
- **GATE S1-A:** pwopriyetè a apwouve threshold, password-reset identity procedure, session policy, MFA scope/recovery, built-in role policy, admin self-lockout rules ak security-event retention. Jiskaske apwobasyon an anrejistre, pa kòmanse S1-B oswa lòt implementation tasks.

## 5. Desizyon pwopriyetè ki nesesè

Tanpri aksepte rekòmandasyon yo kòm ekri oswa presize chanjman pou chak nimewo sa yo:

1. Lockout: 5 esè / 15 minit; reta pwogresif jiska 60 minit.
2. Modpas: minimum 12; recovery se administrator-assisted ak verifye idantite; chanjman fòse apre modpas tanporè.
3. Session: 15 minit idle; 8 èdtan maksimòm.
4. MFA (desizyon S1-H 2026-10-08): TOTP; obligatwa pou Administrator anvan General Release; one-time recovery codes ki konsève offline; invalidasyon ansyen secret ak nouvo kòd sou lost/replaced-device recovery; pa gen bypass si aparèy ak kòd pèdi; administrative recovery dwe verifye idantite/otorite; aksyon MFA/recovery dwe auditables.
5. Role policy: built-in roles pa modifyab dirèkteman; admin kreye roles custom; deny by default; pa ka retire dènye Administrator.
6. Security event retention: dwe aliyen ak desizyon privacy/retention ki rete ouvè; pa efase otomatikman jiskaske policy sa a apwouve.

**Gate status:** ACCEPTED FOR IMPLEMENTATION as the recommended baseline on 2026-10-03, based on the owner's explicit instruction to finish Stage 2. Any customer/jurisdiction policy that differs must be reviewed before deployment. Privacy-event retention has no automatic purge until the separate data-retention policy is approved. S1-H-D decisions D1–D6 were explicitly approved and recorded 2026-10-08; MFA implementation is still pending the separate S2-B secret/key-protection decision and task authorization.
