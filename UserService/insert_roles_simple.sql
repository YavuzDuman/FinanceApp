-- UserDb veritabanında Roles tablosuna varsayılan rolleri ekle (BASİT VERSİYON)
-- ÖNEMLİ: pgAdmin'de sol panelden UserDb veritabanını seçin, sonra bu script'i çalıştırın!

-- Hangi veritabanında olduğunuzu kontrol edin
SELECT current_database();

-- Eğer "UserDb" değilse, sol panelden UserDb'yi seçin!

-- Mevcut rolleri sil (varsa)
DELETE FROM "Roles" WHERE "Name" IN ('Admin', 'Manager', 'User', 'Analyst');

-- Sequence'i sıfırla (basit yöntem)
ALTER SEQUENCE IF EXISTS "Roles_Id_seq" RESTART WITH 1;

-- Rolleri ekle (Id'leri manuel belirleme)
INSERT INTO "Roles" ("Id", "Name") VALUES
    (1, 'Admin'),
    (2, 'Manager'),
    (3, 'User'),
    (4, 'Analyst');

-- Sequence'i güncelle
SELECT setval('"Roles_Id_seq"', 4, true);

-- Sonuçları kontrol et
SELECT * FROM "Roles" ORDER BY "Id";

