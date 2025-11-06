-- Notes tablosu için PostgreSQL CREATE TABLE scripti
-- Veritabanı: NotesDb

CREATE TABLE IF NOT EXISTS "Notes" (
    "NoteId" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL,
    "StockSymbol" VARCHAR(50) NOT NULL,
    "Content" TEXT NOT NULL,
    "LastModifiedDate" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Index'ler (performans için)
CREATE INDEX IF NOT EXISTS "IX_Notes_UserId" ON "Notes" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_Notes_StockSymbol" ON "Notes" ("StockSymbol");
CREATE INDEX IF NOT EXISTS "IX_Notes_UserId_StockSymbol" ON "Notes" ("UserId", "StockSymbol");

