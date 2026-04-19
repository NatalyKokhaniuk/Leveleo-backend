-- PostgreSQL: додати ознаку адреси за замовчуванням (один раз перед деплоєм).
ALTER TABLE "UserAddresses" ADD COLUMN IF NOT EXISTS "IsDefault" boolean NOT NULL DEFAULT false;

-- Опційно: для існуючих користувачів позначити найновішу адресу як default (по одній на UserId).
-- UPDATE "UserAddresses" ua
-- SET "IsDefault" = true
-- FROM (
--   SELECT DISTINCT ON ("UserId") "Id"
--   FROM "UserAddresses"
--   ORDER BY "UserId", (SELECT "CreatedAt" FROM "Addresses" WHERE "Id" = "UserAddresses"."AddressId") DESC
-- ) x
-- WHERE ua."Id" = x."Id";
