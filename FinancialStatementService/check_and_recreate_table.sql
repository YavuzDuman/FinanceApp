-- FinancialStatements tablosunu kontrol et ve gerekirse yeniden oluştur
-- Veritabanı: FinancialStatementDb

-- Önce tabloyu ve index'leri sil (varsa)
DROP TABLE IF EXISTS "FinancialStatements" CASCADE;

-- Tabloyu oluştur
CREATE TABLE "FinancialStatements" (
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

-- Index'leri oluştur (performans için)
CREATE INDEX "IX_FinancialStatements_StockSymbol" ON "FinancialStatements" ("StockSymbol");
CREATE INDEX "IX_FinancialStatements_Type" ON "FinancialStatements" ("Type");
CREATE INDEX "IX_FinancialStatements_StatementDate" ON "FinancialStatements" ("StatementDate");

