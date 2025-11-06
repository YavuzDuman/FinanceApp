-- ============================================
-- NOTES TABLOSU OLUŞTURMA SCRIPTİ
-- ============================================
-- ÖNEMLİ: Bu script'i pgAdmin'de çalıştırmadan ÖNCE:
-- 1. Sol panelden "NotesDb" veritabanına tıklayın (seçili olmalı)
-- 2. Sağ üstteki tab'da "NotesDb" yazdığını kontrol edin
-- 3. Eğer "postgres" veya başka bir veritabanı görünüyorsa, sol panelden NotesDb'yi seçin
-- ============================================

-- Hangi veritabanında olduğunuzu kontrol edin (çalıştırın ve sonucu kontrol edin)
SELECT current_database();

-- Eğer "NotesDb" değilse, lütfen sol panelden NotesDb'yi seçin ve tekrar çalıştırın!

-- postgres veritabanındaki yanlış tabloyu sil (eğer varsa)
-- NOT: Bu komutu postgres veritabanında çalıştırın, sonra NotesDb'ye geçin
-- DROP TABLE IF EXISTS "Notes" CASCADE;

-- NotesDb veritabanında tabloyu oluştur
CREATE TABLE IF NOT EXISTS "Notes" (
    "NoteId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "StockSymbol" VARCHAR(50) NOT NULL,
    "Content" TEXT NOT NULL,
    "LastModifiedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index'leri oluştur
CREATE INDEX IF NOT EXISTS "IX_Notes_UserId" ON "Notes" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notes_StockSymbol" ON "Notes" ("StockSymbol");
CREATE INDEX IF NOT EXISTS "IX_Notes_UserId_StockSymbol" ON "Notes" ("UserId", "StockSymbol");

-- Kontrol: Tablo oluşturuldu mu?
SELECT 
    table_name,
    table_catalog
FROM information_schema.tables 
WHERE table_name = 'Notes' AND table_catalog = 'NotesDb';

