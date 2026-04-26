-- =============================================================================
-- Webshop méretválasztási funkció - MySQL séma példa
-- "Webshopos méretválasztási funkció" - Oktatási célú adatbázis példa
--
-- Ez a fájl bemutatja, hogyan lehet méret/variáns opciókat kezelni
-- egy MySQL alapú webshop adatbázisban.
--
-- MEGJEGYZÉS: Ez csak oktatási célú példa, nem csatlakozik a valódi
-- Hotcakes Commerce adatbázishoz. A Hotcakes saját sémarendszert használ.
-- =============================================================================

-- =============================================================================
-- 1. ALAPTÁBLÁK
-- =============================================================================

-- Termék tábla (products)
CREATE TABLE products (
    id            INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name          VARCHAR(200) NOT NULL COMMENT 'Termék neve',
    sku_base      VARCHAR(50)  NOT NULL COMMENT 'Alap SKU kód (variáns nélkül)',
    description   TEXT,
    base_price    DECIMAL(10,2) NOT NULL DEFAULT 0.00 COMMENT 'Alapár (Ft)',
    active        TINYINT(1)   NOT NULL DEFAULT 1,
    created_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_sku_base (sku_base)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Termékek';


-- Méretkategória tábla — pl. "Ruhaméret (EU)", "Cipőméret (EU)", "Általános"
CREATE TABLE size_categories (
    id          INT         NOT NULL AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(100) NOT NULL COMMENT 'Kategória neve (pl. Ruhaméret EU)',
    description VARCHAR(255)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Méretkategóriák';


-- Méret értékek tábla — az egyes méretek
CREATE TABLE size_values (
    id               INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    size_category_id INT          NOT NULL,
    label            VARCHAR(30)  NOT NULL COMMENT 'Megjelenített méret felirat (pl. S, M, L, XL, 42)',
    sort_order       INT          NOT NULL DEFAULT 0 COMMENT 'Sorrend a legördülőben',
    FOREIGN KEY (size_category_id) REFERENCES size_categories(id) ON DELETE CASCADE,
    INDEX idx_category (size_category_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Méret értékek';


-- Termék-méret kapcsolótábla — egy termékhez tartozó elérhető méretek
-- + variáns-specifikus SKU és készlet
CREATE TABLE product_sizes (
    id             INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    product_id     INT          NOT NULL,
    size_value_id  INT          NOT NULL,
    sku_variant    VARCHAR(80)  NOT NULL COMMENT 'Variáns-specifikus SKU (pl. TSH-WHT-M)',
    stock_qty      INT          NOT NULL DEFAULT 0 COMMENT 'Aktuális készlet',
    price_modifier DECIMAL(8,2) NOT NULL DEFAULT 0.00 COMMENT 'Ár különbség az alapártól (+/-)',
    active         TINYINT(1)   NOT NULL DEFAULT 1,
    FOREIGN KEY (product_id)    REFERENCES products(id)    ON DELETE CASCADE,
    FOREIGN KEY (size_value_id) REFERENCES size_values(id) ON DELETE RESTRICT,
    UNIQUE KEY uq_product_size (product_id, size_value_id),
    INDEX idx_product (product_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Termék méretvariánsok';


-- =============================================================================
-- 2. RENDELÉSI TÁBLÁK
-- =============================================================================

CREATE TABLE orders (
    id              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    order_number    VARCHAR(50)  NOT NULL UNIQUE COMMENT 'Megjelenített rendelésszám',
    customer_name   VARCHAR(200) NOT NULL,
    customer_email  VARCHAR(200) NOT NULL,
    total_amount    DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    status          ENUM('new','processing','shipped','completed','cancelled') NOT NULL DEFAULT 'new',
    created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_customer_email (customer_email),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Rendelések';


-- Rendelési tételek — tartalmaz méret-hivatkozást
CREATE TABLE order_items (
    id              INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    order_id        INT          NOT NULL,
    product_id      INT          NOT NULL,
    product_size_id INT          NULL     COMMENT 'NULL ha nincs méretválasztás',
    product_name    VARCHAR(200) NOT NULL COMMENT 'Rendeléskor aktuális terméknév',
    sku             VARCHAR(80)  NOT NULL COMMENT 'Rendeléskor aktuális SKU',
    size_label      VARCHAR(30)  NULL     COMMENT 'Rendeléskor aktuális méret felirat',
    quantity        INT          NOT NULL DEFAULT 1,
    unit_price      DECIMAL(10,2) NOT NULL COMMENT 'Rendeléskor aktuális egységár',
    line_total      DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (order_id)        REFERENCES orders(id)       ON DELETE CASCADE,
    FOREIGN KEY (product_id)      REFERENCES products(id)     ON DELETE RESTRICT,
    FOREIGN KEY (product_size_id) REFERENCES product_sizes(id) ON DELETE SET NULL,
    INDEX idx_order (order_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='Rendelési tételek';


-- =============================================================================
-- 3. PÉLDA ADATOK
-- =============================================================================

-- Méretkategóriák
INSERT INTO size_categories (name, description) VALUES
    ('Ruhaméret (EU)', 'Európai ruházati méretrendszer: XS, S, M, L, XL, XXL'),
    ('Cipőméret (EU)', 'Európai cipőméret: 36-46'),
    ('Általános',      'Egy méret / One-size termékekhez');

-- Ruhazati méretek
INSERT INTO size_values (size_category_id, label, sort_order) VALUES
    (1, 'XS',  10),
    (1, 'S',   20),
    (1, 'M',   30),
    (1, 'L',   40),
    (1, 'XL',  50),
    (1, 'XXL', 60);

-- Cipőméretek
INSERT INTO size_values (size_category_id, label, sort_order) VALUES
    (2, '36', 10), (2, '37', 20), (2, '38', 30),
    (2, '39', 40), (2, '40', 50), (2, '41', 60),
    (2, '42', 70), (2, '43', 80), (2, '44', 90);

-- Általános méret
INSERT INTO size_values (size_category_id, label, sort_order) VALUES
    (3, 'Egy méret', 10);

-- Termékek
INSERT INTO products (name, sku_base, base_price) VALUES
    ('Pamut póló - Fehér',  'TSH-WHT',   4995.00),
    ('Fleece kabát - Kék',  'JKT-BLU',  12990.00),
    ('Futócipő - Szürke',   'SHO-RUN',   9990.00);

-- Póló méretvariánsok (S, M, L, XL)
INSERT INTO product_sizes (product_id, size_value_id, sku_variant, stock_qty) VALUES
    (1, (SELECT id FROM size_values WHERE label='S'  AND size_category_id=1), 'TSH-WHT-S',  15),
    (1, (SELECT id FROM size_values WHERE label='M'  AND size_category_id=1), 'TSH-WHT-M',  22),
    (1, (SELECT id FROM size_values WHERE label='L'  AND size_category_id=1), 'TSH-WHT-L',  18),
    (1, (SELECT id FROM size_values WHERE label='XL' AND size_category_id=1), 'TSH-WHT-XL',  8);

-- Kabát méretvariánsok (M, L, XL)
INSERT INTO product_sizes (product_id, size_value_id, sku_variant, stock_qty, price_modifier) VALUES
    (2, (SELECT id FROM size_values WHERE label='M'   AND size_category_id=1), 'JKT-BLU-M',   10, 0.00),
    (2, (SELECT id FROM size_values WHERE label='L'   AND size_category_id=1), 'JKT-BLU-L',   12, 0.00),
    (2, (SELECT id FROM size_values WHERE label='XL'  AND size_category_id=1), 'JKT-BLU-XL',   6, 500.00),
    (2, (SELECT id FROM size_values WHERE label='XXL' AND size_category_id=1), 'JKT-BLU-XXL',  3, 500.00);

-- Cipő méretvariánsok (40-43)
INSERT INTO product_sizes (product_id, size_value_id, sku_variant, stock_qty) VALUES
    (3, (SELECT id FROM size_values WHERE label='40' AND size_category_id=2), 'SHO-RUN-GRY-40',  5),
    (3, (SELECT id FROM size_values WHERE label='41' AND size_category_id=2), 'SHO-RUN-GRY-41',  8),
    (3, (SELECT id FROM size_values WHERE label='42' AND size_category_id=2), 'SHO-RUN-GRY-42',  6),
    (3, (SELECT id FROM size_values WHERE label='43' AND size_category_id=2), 'SHO-RUN-GRY-43',  4);


-- =============================================================================
-- 4. HASZNOS LEKÉRDEZÉSEK
-- =============================================================================

-- 4.1 Termék összes méretének lekérése (készlettel és árral)
SELECT
    p.name          AS termek_neve,
    sv.label        AS meret,
    ps.sku_variant  AS sku,
    ps.stock_qty    AS keszlet,
    (p.base_price + ps.price_modifier) AS ar_ft
FROM products p
JOIN product_sizes   ps ON ps.product_id    = p.id
JOIN size_values     sv ON sv.id            = ps.size_value_id
WHERE p.id = 1          -- Cserélje le a termék ID-ra
  AND ps.active = 1
ORDER BY sv.sort_order;


-- 4.2 Csak raktáron lévő méretek lekérése egy adott termékhez
SELECT sv.label AS elerheto_meret, ps.stock_qty
FROM products p
JOIN product_sizes ps ON ps.product_id    = p.id
JOIN size_values   sv ON sv.id            = ps.size_value_id
WHERE p.sku_base = 'TSH-WHT'
  AND ps.active = 1
  AND ps.stock_qty > 0
ORDER BY sv.sort_order;


-- 4.3 Rendelési tétel felvétele méretvariánssal
-- (Ezt általában tranzakcióban érdemes futtatni)
START TRANSACTION;

    -- Ellenőrzés: van-e elegendő készlet?
    SELECT stock_qty INTO @keszlet
    FROM product_sizes
    WHERE id = 5 FOR UPDATE;  -- 5 = a kiválasztott product_size.id

    -- Ha van készlet (keszlet >= rendelt_db), then:
    INSERT INTO order_items
        (order_id, product_id, product_size_id, product_name, sku, size_label, quantity, unit_price, line_total)
    SELECT
        1,                        -- order_id
        p.id,
        ps.id,
        p.name,
        ps.sku_variant,
        sv.label,
        2,                        -- rendelt mennyiség
        (p.base_price + ps.price_modifier),
        2 * (p.base_price + ps.price_modifier)
    FROM product_sizes ps
    JOIN products p    ON p.id    = ps.product_id
    JOIN size_values sv ON sv.id  = ps.size_value_id
    WHERE ps.id = 5;

    -- Készlet csökkentése
    UPDATE product_sizes SET stock_qty = stock_qty - 2 WHERE id = 5;

COMMIT;


-- 4.4 Rendelés tételeinek lekérése (méret információval együtt)
SELECT
    oi.id,
    oi.product_name,
    oi.sku,
    oi.size_label   AS meret,
    oi.quantity     AS db,
    oi.unit_price   AS egysegar_ft,
    oi.line_total   AS osszeg_ft
FROM order_items oi
WHERE oi.order_id = 1
ORDER BY oi.id;


-- 4.5 Kifogyóban lévő méretek riportja (készlet <= 3)
SELECT
    p.name         AS termek,
    sv.label       AS meret,
    ps.sku_variant AS sku,
    ps.stock_qty   AS keszlet
FROM product_sizes ps
JOIN products    p  ON p.id  = ps.product_id
JOIN size_values sv ON sv.id = ps.size_value_id
WHERE ps.stock_qty <= 3
  AND ps.active = 1
ORDER BY ps.stock_qty ASC, p.name;
