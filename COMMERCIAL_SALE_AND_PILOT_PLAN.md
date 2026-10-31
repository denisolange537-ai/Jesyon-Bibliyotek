# Plan komèsyal ak kondisyon pilòt — Jesyon Bibliyotèk

**Dat revizyon:** 2026-10-03  
**Baz prèv:** source, tès ak dokiman ki nan depo a nan dat sa a.  
**Desizyon:** **PA PARE POU VANT JENERAL**. Prepare yon pilòt limite sèlman apre kondisyon obligatwa ki anba yo pase.

## 1. Vèsyon pwodwi a

**Non pwodwi:** Jesyon Bibliyotèk  
**Etikèt pwovizwa:** `0.9.0-pilot`  
**Estati etikèt la:** pwopozisyon pou build pilòt la, se pa yon vèsyon ofisyèl ki deja antre nan project/version metadata. Depo a pa defini yon nimewo vèsyon pwodwi ki verifye. Anvan distribisyon pilòt, mete nimewo sa a nan metadata/build, make artifact la ak dat/build ID, epi konsève hashes ak nòt chanjman yo. Pa prezante etikèt pwovizwa sa a kòm yon release final.

**Platfòm ak fason pou itilize li ki prèv aktyèl yo sipòte:** aplikasyon desktop Windows WPF, SQLite lokal, yon òdinatè/yon bazdone bibliyotèk, achitekti single-company. Artifact self-contained Windows x64 egziste nan prèv istorik pwojè a, men yon enstalasyon pwòp, mizajou ak dezenstalasyon pa verifye nan revizyon sa a. Pa pwomèt itilizasyon pataje an LAN, hosting/cloud, plizyè branch oswa plizyè konpayi.

## 2. Sijè vant ki kouvri

Yon òf limite pou jere operasyon bibliyotèk debaz sou yon sèl òdinatè Windows:

- katalòg liv: anrejistre, chèche, mete ajou ak dezaktive liv;
- manm: anrejistre, chèche, mete ajou ak dezaktive manm;
- prete, dat retou, retou, reta/amann ak peman amann;
- rezèvasyon ak ranpli rezèvasyon lè liv retounen;
- dashboard ak rapò debaz bibliyotèk;
- paramèt bibliyotèk tankou non, dire prete, amann pa jou ak tèm;
- backup/restore manyèl ak audit log, anba kondisyon pilot sa yo.

Fonksyon sa yo egziste nan source ak dokiman/tès pwojè a. Sa pa vle di tout ekran yo, fasilite itilizasyon oswa enstalasyon yo te pase yon verifikasyon entèaktif resan.

### Pa mete nan òf la

- **Pewòl, kalkil taks, payslip oswa reklamasyon konfòmite payroll/taks.** Modil HR/pewòl la prezan, men okenn pwofesyonèl taks/ kontab pa valide règ jiridiksyon yo. Kenbe fonksyon sa yo dezaktive oswa klèman deyò kontra pilòt la. Pa mete okenn to Ayiti oswa lòt peyi nan pwodwi a kòm règ legal valide san sous ak siyati apwobasyon espesyalis la.
- Enstalasyon, mizajou otomatik, siyati kòd, sipò/SLA oswa garanti rekiperasyon ki poko pwouve.
- Itilizasyon plizyè òdinatè an menm tan, LAN, cloud, plizyè branch/tenant, migrasyon done kliyan, oswa enpòtasyon mas.
- Ekspòtasyon PDF/XLSX/CSV/enprime; lokalizasyon kreyòl/franse/anglè konplè; entegrasyon ekstèn.

## 3. Kondisyon obligatwa anvan pilòt kliyan

Pilòt pa kòmanse jiskaske gen prèv ekri pou chak baryè sa yo:

| Baryè | Sa pou fèt/pwouve | Prèv pou kenbe |
|---|---|---|
| Vèsyon/build | Make `0.9.0-pilot` (oswa vèsyon apwouve), fè Release build/publish sou Windows x64, epi verifye aplikasyon an ouvri sou yon machin pwòp. | Build ID, hash artifact, rezilta build/publish ak fich verifikasyon clean-machine. |
| Backup/restore | Fè yon egzèsis ak done tès: backup, restore, verifye done yo, epi teste fichye backup envalid/entèwonp. Bay enstriksyon operatè a ak limit yo alekri. | Rezilta egzèsis restore, lis done verifye, backup pre-restore, gid rekiperasyon ak pwopriyetè rekiperasyon kliyan an. |
| Done prive | Jwenn apwobasyon kliyan an pou ki done li mete ladan; defini aksè sou òdinatè a, kote bazdone/backup yo ye, retansyon ak efasman. Pa mete done pèsonèl reyèl anvan aranjman sa yo ak pwoteksyon machin nan valide. | Fèy limit done ak responsablite pilòt la siyen; pa envante yon delè legal retansyon. |
| Aksè kont | Revize lis itilizatè/otorizasyon ak aksè administratè; itilize modpas inik, limite aksè fizik Windows, epi gen pwosedi pou dezaktive kont lè operatè a kite. Dokimante ke lockout/reset/MFA pa disponib. | Lis itilizatè apwouve, responsab kont yo, prèv tès aksyon kritik ak akseptasyon limit sekirite yo. |
| UI ak workflow | Nan yon sesyon Windows entèaktif, valide login, liv/manm, prete/retou, amann, rezèvasyon, rapò, backup/restore, restart apre restore ak echèl ekran ki sipòte a. | Senaryo pase/echwe ak defo, screenshots oswa nòt obsèvasyon, siyati reprezantan kliyan an. |
| Sipò/limit | Nonmen yon moun kontak, bay gid enstalasyon/demaraj/backup/rapò ensidan, epi fikse lè sipò ak fason pou rapòte pwoblèm. Pa pwomèt SLA anvan li dakò alekri. | Gid pilòt, kontak, lè repons ak limit sipò dakò. |
| Akseptasyon pilòt | Ekri kantite operatè/òdinatè, dire, workflow ki ladan, kritè siksè, kritè sispann, fason rapòte ensidan ak fason efase/retounen done nan fen pilòt la. | Akò pilòt ak responsab kliyan/pwopriyetè a siyen. |

Pou premye pilòt la, limite li a **yon bibliyotèk, yon òdinatè Windows, yon responsab kliyan ak operatè yo nonmen**, san pewòl/taks, san done payroll, epi san pataj bazdone an rezo. Sèvi ak done demonstrasyon pandan verifikasyon; itilize done manm reyèl sèlman apre baryè done prive a pase.

## 4. Kondisyon pou vann jeneral

Pilòt ki reyisi pa egal yon lage komèsyal jeneral. Anvan kontra jeneral oswa pwomosyon kòm pare pou pwodiksyon, fèmen oswa jwenn akseptasyon ekri pou risk ki enpòtan yo:

1. Mete an plas pwoteksyon kont tantativ login repete, chanjman/rekiperasyon modpas, jesyon wòl/otorizasyon ki konplè, ak revizyon aksè.
2. Deside epi aplike pwoteksyon bazdone/backup, kote done yo estoke, retansyon, efasman, ak responsablite vi prive; fè recovery drill repete.
3. Fikse workflow ak kontwòl pou done finansye, koreksyon payroll, taks ak audit. Fè espesyalis legal/pewòl valide chak peyi anvan yo pèmèt oswa vann fonksyon payroll/taks nan jiridiksyon sa a.
4. Fè yon enstalè/mizajou/dezenstalasyon ak prezèvasyon bazdone ki verifye; deside siyati kòd, lisans/notis twazyèm pati, dokiman konfidansyalite ak sipò.
5. Deside si rapò/export ak lokalizasyon nesesè pou mache sib la; si wi, aplike epi teste PDF/XLSX/CSV/enprime ak lang/currency/dates ki nesesè.
6. Pase tès WPF entèaktif, aksè, echèl ekran, restart, echèk backup/restore, ansanm ak tès regresyon/build sou artifact egzak yo pral distribye.
7. Mete dokiman istorik yo ajou pou retire chif tès oswa estati ki pa koresponn, epi konsève yon sèl sous verite pou release ak prèv aktyèl yo.

## 5. Nòt verifikasyon sa a

Dokiman pwojè yo rapòte 68/68 tès pase ak Release build/publish istorik. Nan sesyon revizyon 2026-10-03 la, tantativ `dotnet test` ak tantativ `--no-build --no-restore` pa t bay sòti pandan plizyè minit; yo te kanpe. Kidonk, kantite tès ak publish sa yo rete **prèv istorik**, pa yon rezilta nou rekouri nan revizyon sa a. UI entèaktif, enstalasyon clean-machine, ekspòtasyon, ak konfimasyon legal pa pase nan prèv aktyèl ki disponib isit la.

Plan sa a limite sa ki ka anonse ak vann dapre prèv ki nan depo a. Li pa sètifye pwodwi a, pa apwouve taks/pewòl, epi pa konstitye konsèy legal.
