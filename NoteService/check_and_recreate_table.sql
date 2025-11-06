-- Notes tablosunu kontrol et ve gerekirse yeniden oluştur
-- Veritabanı: NotesDb

-- Önce tabloyu ve index'leri sil (varsa)
DROP TABLE IF EXISTS "Notes" CASCADE;

-- Tabloyu oluştur
CREATE TABLE "Notes" (
    "NoteId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "StockSymbol" VARCHAR(50) NOT NULL,
    "Content" TEXT NOT NULL,
    "LastModifiedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index'leri oluştur (performans için)
CREATE INDEX "IX_Notes_UserId" ON "Notes" ("UserId");
CREATE INDEX "IX_Notes_StockSymbol" ON "Notes" ("StockSymbol");
CREATE INDEX "IX_Notes_UserId_StockSymbol" ON "Notes" ("UserId", "StockSymbol");

