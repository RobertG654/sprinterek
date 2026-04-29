# Hotcakes Rendelések — WinForms Desktop App

> **Célközönség:** Olyan tanulók, akik Hotcakes Commerce API integrációt, WinForms-ot és PDF generálást szeretnének tanulni  
> **Keretrendszer:** .NET 8 · Windows Forms · C#  
> **Ez egy demó alkalmazás** — nem hivatalos számlázó rendszer

---

## Gyors áttekintés

| Funkció | Részletek |
|---|---|
| API kapcsolat | Hotcakes Commerce REST API v1, API kulcs query string paraméterként |
| Rendelések listája | DataGridView, valós idejű frissítéssel (`/orders`) |
| Rendelés tételek | Automatikusan betöltve kiválasztáskor (`/orders/{bvin}/items`) |
| Számla PDF | QuestPDF, A4, `Dokumentumok\GeneratedFiles\Invoices\` |
| Cimke PDF | QuestPDF, A6, `Dokumentumok\GeneratedFiles\Labels\` |
| Mock mód | Beépített mintaadatokkal működő felhasználói felület, ha az API nem elérhető |
| DPI / felbontás | Per-Monitor V2 magas DPI mód — minden monitoron azonos megjelenés |

---

## API kérések menete (két lépés)

Az alkalmazás két különálló API hívást végez:

### 1. lépés — Rendelések listája
```
GET /orders?key={ApiKey}
```
Visszaad egy rendelés-összesítő listát. Minden rendelés tartalmaz:
- `Id` — egész szám azonosító (numerikus, nem elég az items betöltéséhez)
- `bvin` — GUID string azonosító (**ez szükséges a tételek betöltéséhez**)
- vevő adatok, összegek, állapot, szállítási/számlázási cím

### 2. lépés — Rendelési tételek
```
GET /orders/{bvin}/items?key={ApiKey}
```
Egy konkrét rendelés teljes részleteit adja vissza. A válasz szerkezete:

```json
{
  "Content": {
    "Items": [ { "ProductName": "...", "ProductSku": "...", ... } ],
    "Coupons": [],
    "Notes": [],
    "Packages": [],
    "TotalGrand": 149900.0
  },
  "Errors": []
}
```

**Fontos:** `Content` itt egyetlen objektum (nem tömb!), és a tételsorok a `Content.Items` tömbben vannak.  
Ez eltér az `/orders` végponttól, ahol `Content` maga a tömb.

**Miért `bvin` és nem `Id`?**  
A Hotcakes API az items végponton a `bvin` (GUID) alapú útvonalat várja, nem a numerikus `Id`-t.  
Például:
```
http://4.231.236.217/DesktopModules/Hotcakes/API/rest/v1/orders/84f5d6cc-1017-4022-96d3-3be7ea02c39a/items?key=YOUR_API_KEY
```

---

## Konfiguráció — appsettings.json

```json
{
  "Hotcakes": {
    "BaseUrl": "http://4.231.236.217/",
    "ApiBasePath": "DesktopModules/Hotcakes/API/rest/v1/",
    "ApiKey": "PASTE_YOUR_API_KEY_HERE"
  },
  "DemoLogin": {
    "Username": "admin",
    "Password": "admin123"
  },
  "UseMockData": false
}
```

### Az API kulcs beállítása

Az alkalmazás első indításakor (ha még nincs beállítva API kulcs) automatikusan megjelenik az **API kapcsolat beállítása** ablak:

1. Adja meg az **Alap URL** értékét (pl. `http://4.231.236.217/`)
2. Adja meg az **API elérési út** értékét (pl. `DesktopModules/Hotcakes/API/rest/v1/`)
3. Illessze be az **API kulcsot**
4. Kattintson a **Kapcsolat tesztelése** gombra — a rendszer meghívja `/orders`-t és kiírja a választ
5. Mentse a beállításokat a **Mentés és folytatás** gombbal

A beállítások a felhasználó **roaming profiljába** kerülnek — a projekt mappán **kívülre**:
```
%APPDATA%\HotcakesWinFormsApp\apisettings.json
```
(pl. `C:\Users\Alice\AppData\Roaming\HotcakesWinFormsApp\apisettings.json`)

> 🔒 **Miért nem a projekt mappájában?** Az API kulcs titkos érték. Azzal, hogy `%APPDATA%`-ban tároljuk, a kulcs sosem kerül a klónozott / fordított projekt fa alá, így egy `git add .` a projekt mappából véletlenül sem tudja kommitolni. Egy fejlesztő nyugodtan kitolhatja a repót egy publikus GitHub-ra anélkül, hogy a kulcsát megosztaná. A `.gitignore` is tartalmaz egy biztonsági bejegyzést az `apisettings.json`-re arra az esetre, ha egy jövőbeli refaktorálás véletlenül a projekt mappába dobná.

Egy korábbi verzió a `bin\Debug\net8.0-windows\apisettings.json` útvonalat használta. Az alkalmazás indításkor automatikusan átmásolja a régi fájlt az új helyre és törli az eredetit (lásd `ApiSettingsStore.MigrateLegacyFileIfNeeded`).

Az API kulcs a következő DNN admin oldalon hozható létre:
```
http://[az-on-oldala]/DesktopModules/Hotcakes/Core/Admin/configuration/Api.aspx
```

> **Fontos:** Az API kulcs **query string paraméterként** kerül átadásra (`?key=...`),
> **nem** HTTP fejlécben.

Az `apisettings.json` (a `%APPDATA%`-ban) értékei felülírják az `appsettings.json`-ban megadott értékeket — így a felhasználó a telepítés után is módosíthatja a kapcsolati adatokat a telepített fájlok módosítása nélkül.

### Friss klón egy új gépen

Egy új fejlesztő számára, aki frissen klónozza a repót:

1. Az `appsettings.json` üres `ApiKey`-jel érkezik (csak placeholder).
2. A `%APPDATA%\HotcakesWinFormsApp\apisettings.json` még nem létezik.
3. Indításkor automatikusan megnyílik az **API kapcsolat beállítása** ablak, és a fejlesztő beírja a saját kulcsát.
4. A kulcs a saját `%APPDATA%`-jába kerül — sosem a projekt fa alá.

### Mi van, ha mégis kommitoltam egy kulcsot korábban?

A jelen repó történetében **már szerepel** egy korábbi API kulcs (az `appsettings.json` és a `README.md` is tartalmazta egy korai kommitban). A jelenlegi munkamásolat tisztázása után is **a `git log -p` vissza tudja keresni** ezt a kulcsot. Két lehetőség:

1. **Forgassa le a kulcsot a Hotcakes admin oldalán** (DNN > Hotcakes > Configuration > API → új kulcs generálása, régi visszavonása). Ez a legegyszerűbb és kötelező lépés, mielőtt publikus repót csinál belőle.
2. (Opcionális) Ha a teljes történetből is ki akarja törölni: használjon `git filter-repo`-t vagy a [BFG Repo-Cleaner](https://rtyley.github.io/bfg-repo-cleaner/)-t, majd `git push --force`. Ez átírja a távoli történetet — vegye figyelembe, hogy a már klónozott repóknak rebase-elniük kell.

### Beállítások módosítása futás közben

A főablak jobb felső sarkában található **„⚙ API beállítások"** gombbal bármikor újranyitható az **API kapcsolat beállítása** ablak:

1. Megjelenik a jelenleg mentett URL és kulcs.
2. A „Kapcsolat tesztelése" gomb a friss mezőkkel (még mentés előtt) próbál ki egy `/orders` hívást.
3. „Mentés és folytatás" után az alkalmazás automatikusan újraépíti az API-klienseket az új beállításokkal és frissíti a rendelés-listát — **alkalmazás-újraindítás nem szükséges**.

### Végső kérés URL-je

```
{BaseUrl.TrimEnd('/')}/{ApiBasePath.Trim('/')}/{endpoint}?key={ApiKey}
```

Példák:
```
http://4.231.236.217/.../orders?key=...
http://4.231.236.217/.../orders/84f5d6cc-.../items?key=...
```

### Mock mód bekapcsolása

Ha az API nem elérhető, 3 mintarendeléssel is bemutatható az alkalmazás:
```json
"UseMockData": true
```
A mock mód aktív állapotát narancssárga jelvény jelzi a főablakon.

---

## Az alkalmazás működése

### Indítási folyamat
1. Induláskor betöltődik az `appsettings.json`, majd felülírják az `apisettings.json` értékei (ha léteznek).
2. Ha nincs érvényes API kulcs (és nincs mock mód), az **API kapcsolat beállítása** ablak jelenik meg (lásd fentebb).
3. Érvényes beállítás esetén rögtön a főablak nyílik meg.

### Rendelések betöltése
1. Induláskor automatikusan meghívja `/orders?key=...`
2. A lista tartalmazza: rendelésszám, vevő neve, email, végösszeg, fizetési állapot, szállítás, dátum, állapot
3. A „Rendelések frissítése" gombbal manuálisan is frissíthető
4. **Keresés:** a rendelések fölötti szövegmezőbe írva valós időben szűrhet — a keresés a rendelésszámra, vevő nevére és email címre fut (kis- és nagybetű érzéketlenül). Nem történik új API hívás.
5. **Lapozás:** a rendelések 20-as oldalakon jelennek meg. Az oldal alján található „◀ Előző" / „Oldal X / Y" / „Következő ▶" vezérlőkkel navigálhat. A lapozás a szűrt találatokon működik.

### Rendelési tétel betöltése (kiválasztáskor)
1. Rendelés kiválasztásakor az alkalmazás automatikusan meghívja:  
   `/orders/{bvin}/items?key=...`
2. A tételek megjelennek az alsó panelen
3. Ha betöltés közben hiba van, a panel hibaüzenetet mutat

### PDF generálás
- **Számla:** tartalmazza a rendelés fejléc-adatait + a betöltött tételsorokat  
  (ha nincs tétel, összesítő számla generálódik tájékoztató üzenettel)
  - A számla „ELADÓ" blokkjának adatai a `companysettings.json`-ból töltődnek be (lásd [Cégadatok a számlához](#cégadatok-a-számlához)).
  - Sikeres számla generálás után az alkalmazás **automatikusan „Complete" állapotra állítja a rendelést** a Hotcakes API-n keresztül (lásd [Automatikus állapot frissítés](#automatikus-állapot-frissítés-számla-után)).
- **Cimke:** szállítási/számlázási cím alapján generálódik, tételek nem szükségesek
  - A cimkén a rendelésszám alapján **valódi CODE_128 vonalkód** jelenik meg (ZXing.Net-tel generálva). Ha nincs `OrderNumber`, a `bvin`-ből készül vonalkód.
- Piszkozat (`IsPlaced = false`) rendelésekhez PDF generálás **le van tiltva**

---

## Cégadatok a számlához

A számla „ELADÓ" (eladó cég) blokkjának **minden adata** és a számla testreszabható szövegei egy helyi JSON fájlban találhatók:

```
HotcakesWinFormsApp\bin\Debug\net8.0-windows\companysettings.json
```

(Release build esetén `bin\Release\net8.0-windows\companysettings.json`.)

A fájlt szándékosan a futtatható mellett tartjuk, hogy egy **nem programozó** (pl. cégtulajdonos, irodai munkatárs) is bármikor át tudja írni — mindössze egy szövegszerkesztő (Notepad, VS Code, stb.) szükséges, semmilyen forráskód módosítás nem kell.

### Első futtatás

Ha a fájl nem létezik, az alkalmazás induláskor automatikusan létrehozza alapértelmezett (placeholder) értékekkel:

```json
{
  "CompanyName": "Demo Webshop Kft.",
  "TaxNumber": "12345678-2-41",
  "RegistrationNumber": "",
  "Address": "Minta utca 1., 1000 Budapest",
  "Email": "info@demowebshop.hu",
  "Phone": "+36 1 234 5678",
  "Website": "",
  "BankName": "",
  "BankAccount": "",
  "InvoiceTitle": "SZÁMLA",
  "InvoiceSubtitle": "Demo bizonylat — nem hivatalos számla",
  "InvoiceIdPrefix": "DEMO",
  "InvoiceFooterNote": "Ez egy DEMO számla. Nem érvényes pénzügyi bizonylat. Jogi megfelelőséghez hitelesített számlázó program szükséges."
}
```

### Mezők

#### Cégadatok

| Mező | Magyarázat |
|---|---|
| `CompanyName` | A számlán „ELADÓ"-ként megjelenő cégnév. |
| `TaxNumber` | Adószám (pl. `12345678-2-41`). |
| `RegistrationNumber` | Cégjegyzékszám. **Opcionális** — ha üres, nem jelenik meg. |
| `Address` | Postacím egy sorban (pl. székhely, telephely). |
| `Email` | Kapcsolattartási email cím. |
| `Phone` | Kapcsolattartási telefonszám. |
| `Website` | Cég weboldala (pl. `www.pelda.hu`). **Opcionális** — ha üres, nem jelenik meg. |
| `BankName` | A számlavezető bank neve (pl. „OTP Bank"). **Opcionális** — ha üres, csak a számlaszám jelenik meg. |
| `BankAccount` | Bankszámlaszám. **Opcionális** — ha üres, a teljes bank-sor kimarad. |

#### A számla testreszabható szövegei

| Mező | Magyarázat |
|---|---|
| `InvoiceTitle` | A számla bal felső sarkában megjelenő nagy cím. Alapértelmezés: `SZÁMLA`. |
| `InvoiceSubtitle` | Kis piros figyelmeztető szöveg a cím alatt. Üres karakterlánc esetén kimarad — éles használathoz érdemes üresre állítani. |
| `InvoiceIdPrefix` | A jobb felső azonosítónál használt előtag (pl. `INV`, `SZ`, `2026-`). Ezzel saját számlasorszám-mintát lehet kialakítani. |
| `InvoiceFooterNote` | A lap alján megjelenő figyelmeztető szöveg / megjegyzés. Üresre állítva nincs lábjegyzet. |

### A fájl módosítása

1. Állítsa le az alkalmazást.
2. Nyissa meg a `companysettings.json`-t bármilyen szövegszerkesztővel (pl. Notepad, VS Code).
3. Írja át a kívánt mezőket. **Bármelyik mező** változhat egy cég életében — átköltöznek (Address), bankot váltanak (BankName, BankAccount), megváltozik az adószám vagy a cégnév (CompanyName, TaxNumber, RegistrationNumber), új weboldal lesz (Website), új email cím (Email), stb.
4. Mentse a fájlt, majd indítsa újra az alkalmazást.
5. A következő generált számlán már az új értékek jelennek meg.

> 🔄 **Visszafelé kompatibilitás:** Ha egy korábbi (rövidebb) `companysettings.json`-t talál, az alkalmazás induláskor automatikusan kibővíti a fájlt az új mezőkkel — alapértelmezett értékekkel — hogy szerkeszthető legyen. A meglévő értékek nem vesznek el.

A JSON fájlt a `CompanySettings` osztály olvassa be (`Configuration/CompanySettings.cs`), az `InvoiceTemplateService` pedig ebből tölti a „ELADÓ" blokkot, a fejlécet és a lábjegyzetet — **nincsenek a kódba égetett (hardcoded) cégadatok vagy szövegek**.

### Fájl mentési helyek

| Típus | Útvonal |
|---|---|
| Számlák | `%USERPROFILE%\Documents\GeneratedFiles\Invoices\` |
| Szállítási cimkék | `%USERPROFILE%\Documents\GeneratedFiles\Labels\` |

---

## Automatikus állapot frissítés (számla után)

Amikor a felhasználó a „Számla generálása" gombbal sikeresen készít egy PDF-et, az alkalmazás **automatikusan átállítja a kiválasztott rendelés állapotát „Complete"-re** a Hotcakes API-n keresztül.

### Folyamat: GET → mutáció → POST → verifikáció

A frissítés **egy stratégiát** használ — a teljes rendelést olvassa be, módosítja az állapot-mezőket, majd visszaküldi az **egész** objektumot. Ez a kerülőút azért szükséges, mert a Hotcakes végpont a parciális POST-ot felülírásként kezeli (a hiányzó mezőket alapértékre állítja vissza), nem összevonásként.

```
1. GET  {BaseUrl}/{ApiBasePath}/orders/{bvin}?key={ApiKey}
        → teljes OrderDTO snapshot
2. (kliens) StatusCode + StatusName + Instructions átírása a snapshoton
3. POST {BaseUrl}/{ApiBasePath}/orders/{bvin}?key={ApiKey}&recalculateOrder=false
        Content-Type: application/json
        Body: a teljes módosított OrderDTO
4. ~500 ms várakozás
5. GET  {BaseUrl}/{ApiBasePath}/orders/{bvin}?key={ApiKey}
        → StatusCode összehasonlítása a célállapottal
```

**Végpont részletek:**
- A frissítéshez **POST /orders/{bvin}** kell (a `bvin` az URL-ben van) — ez a frissítő (UPDATE) útvonal. A POST /orders (bvin nélkül) az új-rekord-létrehozó (CREATE) útvonal, ami HTTP 500-at ad. A PUT szintén HTTP 500-at ad.
- A `recalculateOrder=false` query paraméter elkerüli az árazás újrafuttatását.
- A body minden `DateTime` mezője **ISO 8601** formátumban megy ki (pl. `2026-04-26T14:32:11.0000000+02:00`). A Hotcakes a GET-eken `/Date(ms)/` formában adja vissza a dátumokat, de a POST bemeneti modell-binder kizárólag ISO 8601-et fogad el — ha `/Date(...)`-t küldünk vissza, hibát ad: `{"Code":"EXCEPTION","Description":"/Date(...) is not a valid value for DateTime."}`. Ezt a `DotNetJsonDateConverter.Write` kezeli (lásd a fájlban lévő figyelmeztető kommentet).

### Miért nem minimális JSON?

Eredetileg egy minimális kérés-törzzsel (body-val) próbáltunk frissíteni:

```json
{ "Bvin": "...", "StatusCode": "...", "StatusName": "Complete", "Instructions": "..." }
```

A szerver erre **HTTP 200 OK**-t adott, de a verifikációs GET a régi állapotot mutatta — a hiányzó mezőket (`Items`, `BillingAddress`, `TotalGrand`, stb.) ugyanis alapértékre állította, és az egész rendelés alapértelmezett snapshot-té esett volna szét. Ezért a kód mostantól **mindig a teljes snapshot-et küldi vissza**, és nincs külön „minimális + visszaeső" stratégia.

### Verifikáció

Azért szükséges a POST utáni GET, mert a Hotcakes — még a teljes-objektum POST esetén is — időnként **HTTP 200 OK-t ad anélkül, hogy a változás perzisztálódna**. A vak „200 = siker" feltevés csendes hibát okozna: a számla létrejön, de a rendelés állapota nem változik. A verifikációs GET garantálja, hogy a felhasználói felület csak akkor mutat „sikeres" üzenetet, ha a szerver oldalon is megvan a `Complete` állapot.

### UI viselkedés

| Eset | UI reakció |
|---|---|
| Sikeres frissítés (verifikálva) | **Nincs külön ablak** — az alsó státusz sávban megjelenik a sikerüzenet, és a rendelés-lista automatikusan frissül. |
| A rendelés már „Complete" volt | Sikeres üzenet a státusz sávban, POST nem indul |
| 4xx/5xx hiba a GET-en vagy POST-on | Egyetlen figyelmeztető ablak a hibakóddal |
| 200 OK, de a verifikáció nem mutatja a változást | Egyetlen figyelmeztető ablak (nincs további újrapróbálkozás) |
| Hálózati hiba / időtúllépés | Magyar nyelvű hibaüzenet az `ApiExceptionMapper`-ből |
| Mock mód aktív | Az állapot frissítés kihagyva, üzenet a státusz csíkban |

> 💡 **Egyszerűsített folyamat:** A korábbi „Megjeleníti a részletes naplót?" kérdés és a „Részletek" gomb el lett távolítva — egy számla generálás → állapot frissítés folyamatot **már nem szakít meg további párbeszédablak**. A teljes kérés / válasz napló továbbra is a Visual Studio Debug Output ablakában olvasható `[StatusUpdate]` előtaggal — fejlesztői célokra (a végfelhasználó szempontjából láthatatlan).

### Kód helye

- `Services/OrderStatusUpdateService.cs` — GET + mutáció + teljes POST + verifikációs GET
- `Services/StatusUpdateResult.cs` — visszatérési érték (Success / Error / DebugLog / VerifiedOrder)
- `Models/HotcakesOrderStatus.cs` — Complete + Received állapot (StatusCode + StatusName) konstansok
- `Helpers/DotNetJsonDateConverter.cs` — `/Date(ms)/` olvasás + ISO 8601 írás
- `Forms/StatusUpdateLogForm.cs` — debug napló megjelenítő ablak
- `Forms/MainForm.cs` — `UpdateStatusAfterInvoiceAsync` (a `GenerateInvoiceAsync` hívja)

---

## Manuális visszaállítás „Received"-re (kész rendeléseknél)

A főablak alsó panelének **jobb alsó sarkában** található a **„Visszaállítás 'Received'-re"** gomb. Ezzel egy, már `Complete` állapotba került rendelést lehet visszaállítani a kezdeti `Received` állapotba — például ha tévedésből generáltunk rá számlát, vagy újra kell csomagolni a rendelést.

### Mikor használható

- A gomb **csak akkor aktív**, ha a kiválasztott rendelés `StatusCode`-ja megegyezik a `Complete` GUID-dal (`09D7305D-…`).
- Bármely más állapotnál (Received, ReadyForPayment, OnHold, Cancelled stb.) a gomb **letiltott** marad.
- Mock módban a gomb mindig letiltott (nincs élő API).

### Folyamat

1. **Nincs megerősítő párbeszédablak** — a gomb kattintás magában a felhasználói gesztus.
2. Ugyanaz a `OrderStatusUpdateService.UpdateOrderStatusAsync(bvin, HotcakesOrderStatus.Received)` hívás fut, mint a számla utáni Complete frissítésnél (GET → mutáció → teljes POST → verifikációs GET).
3. Sikeres visszaállítás után az alsó státusz sávban jelenik meg a megerősítés, és a rendelés-lista automatikusan frissül — semmilyen extra ablak nem nyílik.
4. Sikertelenség esetén egy darab figyelmeztető ablak jelenik meg a hibaüzenettel (napló-ablak nincs).

### Kód helye

- `Forms/MainForm.cs` — `_btnRevertToReceived` gomb (jobb-szélhez horgonyozva) + `RevertOrderToReceivedAsync` + `UpdateRevertButtonState`
- `Models/HotcakesOrderStatus.cs` — `Received` állapot-konstans (`058B09EE-…`)
- `Services/OrderStatusUpdateService.UpdateOrderStatusAsync(string bvin, OrderStatus targetStatus)` — explicit célállapotot fogadó túlterhelés

---

## API válasz formátumok

### Rendelések lista (`/orders`)

```json
{
  "Errors": [],
  "Content": [
    {
      "Id": 1,
      "bvin": "84f5d6cc-1017-4022-96d3-3be7ea02c39a",
      "OrderNumber": "1001",
      "UserEmail": "valaki@pelda.hu",
      "IsPlaced": true,
      "PaymentStatus": 3,
      "ShippingStatus": 1,
      "TotalGrand": 15990.00,
      "TimeOfOrderUtc": "/Date(1772190902103)/",
      "BillingAddress": { ... },
      "ShippingAddress": { ... }
    }
  ]
}
```

### Rendelési tételek (`/orders/{bvin}/items`)

```json
{
  "Errors": [],
  "Content": {
    "Items": [
      {
        "ProductName": "Pamut póló",
        "ProductSku": "TSH-WHT-M",
        "Quantity": 2,
        "BasePricePerItem": 4995.00,
        "AdjustedPricePerItem": 4995.00,
        "LineTotal": 9990.00,
        "ProductShortDescription": "<ul class=\"lineitemoptions\"><li>Méret: M</li></ul>",
        "SelectionData": ""
      }
    ],
    "Coupons": [],
    "Notes": [],
    "Packages": [],
    "TotalGrand": 9990.00
  }
}
```

**Fontos — a két végpont Content típusa ELTÉRŐ:**

| Végpont | `Content` típusa |
|---|---|
| `/orders` | **tömb** — `Content: [ {...}, {...} ]` |
| `/orders/{bvin}/items` | **egyetlen objektum** — `Content: { "Items": [...], ... }` |

Egyéb pontosítások:
- SKU mező neve: `ProductSku` (nem `Sku`)
- `StoreId` az API-ban egész szám (nem karakterlánc) — korábbi verziókban ezt tévesen karakterláncként kezeltük
- Opció / variáns szöveg a `ProductShortDescription` HTML mezőben van:  
  pl. `<ul class="lineitemoptions"><li>Szín: XL</li></ul>` → az alkalmazás kinyeri: `Szín: XL`
- A `SelectionData` **nem karakterlánc** — belső GUID-párokat tartalmazó tömb:  
  `[{ "OptionBvin": "cc248...", "SelectionData": "a0293..." }]`  
  Ezek nem jeleníthetők meg közvetlenül; az olvasható szöveg a `ProductShortDescription`-ben van
- Dátumok régi .NET formátumban: `/Date(milliszekundum)/` — automatikusan kezelve
- `bvin` a GUID azonosító; `Id` csak numerikus sorrend
- Egységár: `AdjustedPricePerItem` (ha van) különben `BasePricePerItem`

---

## Piszkozat rendelések

Az `IsPlaced = false` rendelések piszkozatok. Ezekre:
- „Piszkozat" állapot látszik a listában
- `OrderNumber`, email és összeg lehet üres
- PDF generálás **blokkolva** van

---

## Projekt struktúra

```
HotcakesWinFormsApp/
├── appsettings.json             ← Alapértelmezett konfiguráció (verziókezelt)
├── apisettings.json             ← API kapcsolat (felhasználó által beállítva, bin-ben)
├── companysettings.json         ← Cégadatok a számlához (bin-ben, auto-gen)
├── Program.cs                   ← Belépési pont (API kulcs ellenőrzés → MainForm)
│
├── Configuration/
│   ├── AppSettings.cs           ← appsettings.json modellje
│   ├── ApiSettingsStore.cs      ← apisettings.json betöltő/mentő (új)
│   └── CompanySettings.cs       ← companysettings.json betöltő/mentő (új)
│
├── Models/
│   ├── AddressInfo.cs           ← Cím modell
│   ├── OrderSummary.cs          ← /orders válasz modellje (bvin is itt van)
│   ├── OrderDetail.cs           ← OrderSummary + Items lista
│   ├── OrderLine.cs             ← /orders/{bvin}/items egy tétele
│   ├── HotcakesApiResponse.cs   ← {"Errors":[],"Content":[...]} wrapper
│   └── HotcakesOrderStatus.cs   ← Rendelés-állapot konstansok (Complete) (új)
│
├── ViewModels/
│   ├── OrderViewModel.cs        ← grid-projekció a rendelésekhez
│   └── OrderItemViewModel.cs    ← grid-projekció a tételsorokhoz
│
├── Services/
│   ├── HotcakesApiService.cs    ← GetOrdersAsync + GetOrderItemsAsync
│   ├── MockDataService.cs       ← Offline demo adatok
│   ├── PdfService.cs            ← CompanySettings-et átadja az InvoiceTemplate-nek
│   ├── InvoiceTemplateService.cs ← CompanySettings → ELADÓ blokk
│   ├── LabelTemplateService.cs  ← ZXing.Net CODE_128 vonalkód
│   ├── OrderStatusUpdateService.cs ← POST /orders + verifikációs GET (új)
│   └── StatusUpdateResult.cs    ← Update eredmény + debug napló (új)
│
├── Helpers/
│   ├── ApiExceptionMapper.cs    ← Kivételek → Magyar hibaüzenetek
│   ├── DotNetJsonDateConverter.cs ← /Date(ms)/ kezelő
│   ├── FilePathHelper.cs
│   ├── MessageHelper.cs
│   └── BarcodeGenerator.cs      ← ZXing-alapú CODE_128 PNG generátor (új)
│
└── Forms/
    ├── ApiSettingsForm.cs       ← API kulcs beállító ablak (UJ, LoginForm helyett)
    ├── StatusUpdateLogForm.cs   ← Debug napló megjelenítő ablak (új)
    └── MainForm.cs              ← Keresés + lapozás + rendelések + auto-Complete
```

---

## Magas DPI / monitor-konzisztens megjelenés

Az alkalmazás **Per-Monitor V2** magas DPI módban fut (lásd `HotcakesWinFormsApp.csproj` → `<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>`), és minden Form `AutoScaleMode = Dpi`-ra van állítva 96 DPI-s kiindulási mérettel. Ennek a kombinációnak a célja:

- A főablak és a párbeszédablakok **fizikailag azonos méretben** jelennek meg minden monitoron, függetlenül a felbontástól (1080p, 1440p, 4K) és a Windows DPI-skálázás (100% / 125% / 150% / 200%) beállítástól.
- Ha a felhasználó **több monitor** között húzza át az alkalmazást, és azoknak eltérő a skálázása, a tartalom automatikusan újraskálázódik az új monitor DPI-jére.
- A vezérlők elrendezése (gombok, gridek, kártyák) arányosan nyúlik / zsugorodik a felhasználói skálázással.

> 🔧 **Ha valami mégis rosszul jelenne meg:** ellenőrizze, hogy a Windows **Megjelenítés beállításai** → **Méretezés és elrendezés** értéke nincs-e egy szokatlan, nem szabványos értéken (pl. 175%). Ilyen esetekben az automatikus skálázás néha kerekítési hibákat mutat — a 100% / 125% / 150% / 200% beállítások a legmegbízhatóbbak.

---

## Hibakeresés

### Érvénytelen vagy hiányzó API kulcs (HTTP 401/403)
**Tünet:** `Az API kulcs hiányzik vagy érvénytelen` üzenet.  
**Megoldás:** Ellenőrizze az `ApiKey` értékét az `appsettings.json`-ban. Tesztelje böngészőből.

### Helytelen API végpont (HTTP 404)
**Tünet:** `Az API végpont nem található (HTTP 404)`.  
**Megoldás:** Ellenőrizze az `ApiBasePath`-t. Próbálja böngészőből: `.../orders?key=...`

### Tételek betöltése sikertelen
**Tünet:** Az alsó panel hibaüzenetet mutat `bvin` vagy hálózati hibáról.  
**Megoldás:**
- Ellenőrizze, hogy a kiválasztott rendelés `bvin`-je nem üres
- Tesztelje a URL-t böngészőből: `.../orders/{bvin}/items?key=...`
- A válasz `Content` egyetlen objektum legyen (nem tömb) — tételek a `Content.Items`-ben
- Ellenőrizze a Debug Output ablakot (`[HotcakesAPI]` előtag)

### Időtúllépés
**Tünet:** `Az API nem válaszol időben` — ~30 mp várakozás után jelenik meg.  
**Megoldás:** Ellenőrizze az internetkapcsolatot és a szerver elérhetőségét. Mock módot is bekapcsolhat.

### JSON feldolgozási hiba
**Tünet:** `A szerver válasza beérkezett, de az adatok feldolgozása nem sikerült.`  
**Hibakeresés:** Debug Output → `[HotcakesAPI]` sorok tartalmazzák a nyers JSON-t (első 800 karakter).

Ismert korábbi hibák:
- A `StoreId` a DTO-ban `string`-ként volt tipizálva, holott az API egész számot küld → javítva az `OrderSummary`, `AddressInfo`, `OrderLine`-ban
- A `SelectionData` a DTO-ban `string`-ként volt tipizálva, holott az API GUID-párokat tartalmazó tömböt küld → javítva: `List<SelectionDataEntry>`

### Nem jelenik meg tétel
**Tünet:** A tétel-panel `Ehhez a rendeléshez nem találhatók tételek` üzenetet mutat.  
Ez normális lehet:
- A rendelésnek valóban nincs tétele
- A rendelés piszkozat állapotban van

---

## Implementált funkciók

| Funkció | Állapot |
|---|---|
| Rendelések listázása (`/orders`) | ✅ |
| Tételek betöltése (`/orders/{bvin}/items`) | ✅ |
| `bvin` alapú tétel-végpont | ✅ |
| Magyar hibaüzenetek (időtúllépés, 404, 401, hálózat, JSON) | ✅ |
| `/Date(ms)/` dátum formátum kezelése | ✅ |
| Mock mód | ✅ |
| Számla PDF (tételekkel vagy összesítőként) | ✅ |
| Szállítási cimke PDF | ✅ |
| Piszkozat rendelések blokkolása | ✅ |
| Lapozás UI (kliensoldali, 20 / oldal) | ✅ |
| Rendelés szűrés (OrderNumber / CustomerName / UserEmail) | ✅ |
| Valódi vonalkód (ZXing.Net CODE_128) | ✅ |
| Cégadatok JSON fájlból (`companysettings.json`) | ✅ |
| API beállító ablak (`ApiSettingsForm`) + kapcsolat tesztelés | ✅ |
| Automatikus „Complete" állapot frissítés számla után + verifikáció | ✅ |
| Teljes-objektum POST stratégia (GET → mutáció → POST → verifikációs GET) | ✅ |
| Dátum konverter: `/Date(ms)/` olvasás, ISO 8601 írás (Hotcakes input formátum) | ✅ |
| Manuális „Visszaállítás 'Received'-re" gomb (csak Complete rendeléseknél aktív) | ✅ |
| Részletes napló dialógus eltávolítva (számla / státusz frissítés / visszaállítás) | ✅ |
| Visszaállítás megerősítő dialógus eltávolítva (egy kattintás → kész) | ✅ |
| Per-Monitor V2 magas DPI mód — különböző felbontású monitorokon konzisztens megjelenés | ✅ |
| Bővített `companysettings.json` (Website, RegistrationNumber, BankName, számla cím / előtag / lábjegyzet) | ✅ |

---

## NuGet csomagok

| Csomag | Verzió |
|---|---|
| `Microsoft.Extensions.Configuration` | 8.0.0 |
| `Microsoft.Extensions.Configuration.Json` | 8.0.0 |
| `Microsoft.Extensions.Configuration.Binder` | 8.0.0 |
| `QuestPDF` | 2024.3.4 |
| `ZXing.Net` | 0.16.9 |

**QuestPDF licenc:** Közösségi (Community) licenc — ingyenes, nem kereskedelmi használatra.  
**ZXing.Net licenc:** Apache 2.0.

---

## Ismert korlátok

1. **Nem hivatalos számla** — nem felel meg a magyar számlázási jogszabályoknak
2. **Korlátozott írás** — az alkalmazás csak rendelés-állapotot ír (számla generáláskor → „Complete", vagy manuálisan vissza → „Received"); egyéb mezőket (vevő, tételek, árak) nem módosít.
3. **Kliensoldali lapozás** — a `/orders` végpont továbbra is max. 50 rendelést ad vissza egy hívásban; a 20-as oldalméret ezen a kliensoldali listán fut.
4. **HTTP (nem HTTPS)** — éles használathoz HTTPS javasolt
5. **Kódba égetett állapot-GUID-ok** — a `Complete` (`09D7305D-…`) és `Received` (`058B09EE-…`) GUID-ok a `Models/HotcakesOrderStatus.cs`-ben vannak rögzítve a Hotcakes Commerce alapértelmezésekkel. Ha a telepítés egyedi GUID-okat használ, ott kell módosítani.
6. **Csak két állapot** — az `OrderStatusUpdateService` `Complete`-re és `Received`-re tud váltani. További állapotokhoz bővíteni kell a `HotcakesOrderStatus` osztályt és a hívási helyet.
