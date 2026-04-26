# Webshop méretválasztási funkció — SQL oktatási példa

> **Célközönség:** Webshop fejlesztést tanuló fejlesztők  
> **Technológia:** MySQL 8.x  
> **Kapcsolat az alkalmazással:** Önálló oktatási anyag — az asztali alkalmazás nem csatlakozik közvetlenül ehhez az adatbázishoz.

---

## Miről szól ez a példa?

A webshopokban a termékek gyakran több variánsban elérhetők: különböző méretekben (S/M/L/XL, 38/39/40...), színekben stb. Ezt a funkciót **méret/variáns kezelésnek** nevezzük.

A `size_selection_example.sql` fájl bemutatja:
1. Hogyan tároljuk el a méretkategóriákat és az egyes méreteket
2. Hogyan kapcsoljuk a méreteket a termékekhez (készlettel és áreltéréssel)
3. Hogyan rögzítünk rendelési tételt a kiválasztott mérettel együtt
4. Hasznos lekérdezések a napi webshop-üzemeltetéshez

---

## Adatbázis séma áttekintése

```
products              — Termékek (alaptábla)
    │
    ├── product_sizes  — Termék × Méret kapcsolótábla
    │       │             + SKU variáns, készlet, áreltérés
    │       └── size_values   — Konkrét méretek (S, M, L, 42...)
    │               └── size_categories — Méretrendszer (Ruhaméret EU, Cipőméret EU...)
    │
orders                — Rendelések
    └── order_items   — Rendelési tételek
             └── (product_sizes-re hivatkozik, ha volt méretválasztás)
```

### Tábla leírások

| Tábla | Szerepe |
|---|---|
| `size_categories` | Méretrendszer-csoportok (pl. "Ruhaméret EU", "Cipőméret EU") |
| `size_values` | Az egyes konkrét méretek (pl. S, M, 42) egy kategórián belül |
| `product_sizes` | Melyik termékhez melyik méret elérhető (+ SKU, készlet, áreltérés) |
| `products` | Alaptermékek |
| `orders` | Vásárlói rendelések |
| `order_items` | Rendelési tételek méretvariáns-hivatkozással |

---

## Hogyan kapcsolódik ez a Hotcakes Commerce-hoz?

A Hotcakes Commerce saját variáns/opció rendszert használ:
- **ProductOptions** (termék opciók): pl. "Méret", "Szín"
- **ProductVariants**: az opció-kombinációkból generált variánsok
- **InventoryItem**: variáns-szintű készletkezelés
- A REST API-ban a `SelectionData` mező tartalmazza a kiválasztott opciókat (pl. `"Méret: M"`)

A mi oktatási sémánk egyszerűbb, de az alapelv ugyanaz:
- Kategória = ProductOption (pl. "Méret")
- size_value = OptionItem (pl. "M")
- product_sizes = ProductVariant (SKU + készlet + áreltérés)

---

## Fontos tervezési döntések

### Miért mentjük el a product_name és size_label értékeket az order_items táblába?

Azért, mert a rendelés leadásakor érvényes adatokat kell megőrizni. Ha utólag megváltoztatják a termék nevét vagy törlik a méretopciót, a régi rendeléseknek még az eredeti adatokat kell mutatniuk. Ez a **snapshotting pattern** — alapvető webshop-tervezési elvárás.

### Miért NULL a product_size_id az order_items-ben?

Nem minden terméknek van méretválasztása (pl. könyvek, digitális termékek). A `NULL` érték jelzi, hogy az adott tételnél nem volt méretopció.

### Miért van price_modifier a product_sizes táblában?

Mert bizonyos méretekhez (pl. XXL, OverSize) felár járhat. Ez lehetővé teszi, hogy az alap egységárat ne kelljen minden variánsnál ismételni — csak az eltérést tároljuk.

---

## Futtatás

```bash
# MySQL parancssorból:
mysql -u root -p webshop_db < size_selection_example.sql

# Vagy importálja phpMyAdmin / MySQL Workbench segítségével.
```

> Hozzon létre először egy `webshop_db` nevű adatbázist, vagy módosítsa a fájlban a táblaneveket a meglévő sémájának megfelelően.

---

## Bővítési lehetőségek

- **Szín opció hozzáadása:** Hozzon létre egy `color_values` táblát és egy `product_colors` kapcsolótáblát — vagy általánosítsa a sémát egy `option_types` + `option_values` megközelítéssel
- **Kombináció-alapú variáns:** Ha egy terméknek egyszerre van méret- és szín-opciója, szükség lehet egy `product_variants` táblára, amely több opció kombinációját fogja össze
- **Foglalás kezelés:** A rendelés leadásakor a készletet érdemes ideiglenesen foglalni (reservation), és csak a fizetés megerősítésekor végleges levonással kezelni
