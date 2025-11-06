-- FinancialStatements tablosu için PostgreSQL CREATE TABLE scripti
-- Veritabanı: FinancialStatementDb

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

-- Index'ler (performans için)
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_StockSymbol" ON "FinancialStatements" ("StockSymbol");
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_Type" ON "FinancialStatements" ("Type");
CREATE INDEX IF NOT EXISTS "IX_FinancialStatements_StatementDate" ON "FinancialStatements" ("StatementDate");

