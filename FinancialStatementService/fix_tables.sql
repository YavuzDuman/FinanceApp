-- ============================================
-- FINANCIALSTATEMENTS TABLOSU OLUŞTURMA SCRIPTİ
-- ============================================
-- ÖNEMLİ: Bu script'i pgAdmin'de çalıştırmadan ÖNCE:
-- 1. Sol panelden "FinancialStatementDb" veritabanına tıklayın (seçili olmalı)
-- 2. Sağ üstteki tab'da "FinancialStatementDb" yazdığını kontrol edin
-- 3. Eğer "postgres" veya başka bir veritabanı görünüyorsa, sol panelden FinancialStatementDb'yi seçin
-- ============================================

-- Hangi veritabanında olduğunuzu kontrol edin (çalıştırın ve sonucu kontrol edin)
SELECT current_database();

-- Eğer "FinancialStatementDb" değilse, lütfen sol panelden FinancialStatementDb'yi seçin ve tekrar çalıştırın!

-- postgres veritabanındaki yanlış tabloyu sil (eğer varsa)
-- NOT: Bu komutu postgres veritabanında çalıştırın, sonra FinancialStatementDb'ye geçin
-- DROP TABLE IF EXISTS "FinancialStatements" CASCADE;

-- FinancialStatementDb veritabanında tabloyu oluştur
CREATE TABLE IF NOT EXISTS "FinancialStatements" (
    "Id" SERIAL PRIMARY KEY,
    "StockSymbol" VARCHAR(50) NOT NULL,
    "CompanyName" VARCHAR(255) NOT NULL,
    "StatementDate" TIMESTAMP WITH TIME ZONE NOT NULL,
    "Type" VARCHAR(50) NOT NULL,
    "Data" TEXT NOT NULL,
    "AnnouncementDate" TIMESTAMP WITH TIME ZONE NULL,
    "NetProfitChangeRate" DECIMAL(18, 4) NULL,
    "UpdatedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index'leri oluştur
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_StockSymbol" ON "FinancialStatements" ("StockSymbol");
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_Type" ON "FinancialStatements" ("Type");
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_StatementDate" ON "FinancialStatements" ("StatementDate");

-- Kontrol: Tablo oluşturuldu mu?
SELECT 
    table_name,
    table_catalog
FROM information_schema.tables 
WHERE table_name = 'FinancialStatements' AND table_catalog = 'FinancialStatementDb';

