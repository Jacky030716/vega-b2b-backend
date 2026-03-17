-- Replace ShopItems.Stock with ShopItems.Theme
-- Safe for existing development data.

BEGIN;

ALTER TABLE "ShopItems"
  ADD COLUMN IF NOT EXISTS "Theme" character varying(50);

UPDATE "ShopItems"
SET "Theme" = CASE
  WHEN COALESCE(NULLIF(btrim("Description"), ''), '') IN ('Demon Slayer', 'Jujutsu Kaisen', 'Classic', 'Self')
    THEN btrim("Description")
  ELSE 'General'
END
WHERE "Theme" IS NULL OR btrim("Theme") = '';

ALTER TABLE "ShopItems"
  ALTER COLUMN "Theme" SET DEFAULT 'General';

ALTER TABLE "ShopItems"
  ALTER COLUMN "Theme" SET NOT NULL;

ALTER TABLE "ShopItems"
  DROP COLUMN IF EXISTS "Stock";

COMMIT;
