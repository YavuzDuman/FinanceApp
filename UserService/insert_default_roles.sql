-- UserDb veritabanında Roles tablosuna varsayılan rolleri ekle
-- ÖNEMLİ: Bu script'i UserDb veritabanında çalıştırın!

-- Hangi veritabanında olduğunuzu kontrol edin
SELECT current_database();

-- Eğer "UserDb" değilse, lütfen sol panelden UserDb'yi seçin ve tekrar çalıştırın!

-- Önce mevcut rolleri kontrol et ve sil (varsa)
DELETE FROM "Roles" WHERE "Name" IN ('Admin', 'Manager', 'User', 'Analyst');

-- Sequence adını bul ve sıfırla
DO $$
DECLARE
    seq_name text;
BEGIN
    SELECT pg_get_serial_sequence('"Roles"', '"Id"') INTO seq_name;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('ALTER SEQUENCE %s RESTART WITH 1', seq_name);
    END IF;
END $$;

-- Rolleri ekle (Id'leri manuel olarak belirleyerek)
INSERT INTO "Roles" ("Id", "Name") VALUES
    (1, 'Admin'),
    (2, 'Manager'),
    (3, 'User'),
    (4, 'Analyst');

-- Sequence'i tekrar ayarla (sonraki eklemeler için 5'ten başlaması için)
DO $$
DECLARE
    seq_name text;
BEGIN
    SELECT pg_get_serial_sequence('"Roles"', '"Id"') INTO seq_name;
    IF seq_name IS NOT NULL THEN
        EXECUTE format('SELECT setval(%L, 4, true)', seq_name);
    END IF;
END $$;

-- Kontrol: Rollerin eklendiğini doğrula
SELECT * FROM "Roles" ORDER BY "Id";

