-- One-time data fix: convert usr."Users"."AvatarId" legacy values (URL/name/bear)
-- into numeric avatar ShopItem IDs stored as text.
--
-- Target model:
--   - AvatarId stores a numeric ShopItems.Id (as text)
--   - "0" means default avatar (bear / no equipped shop avatar)
--
-- Safe to run multiple times (idempotent): rows already normalized stay unchanged.

BEGIN;

WITH avatar_shop AS (
    SELECT
        s."Id"::text AS avatar_item_id,
        lower(regexp_replace(coalesce(s."Name", ''), '[^a-z0-9]+', '', 'g')) AS shop_name_norm,
        lower(
            regexp_replace(
                coalesce(
                    nullif(split_part(split_part(s."ImageUrl", '%2F', 2), '?', 1), ''),
                    split_part(
                        split_part(s."ImageUrl", '/', array_length(regexp_split_to_array(s."ImageUrl", '/'), 1)),
                        '?',
                        1
                    )
                ),
                '\.[a-z0-9]+$',
                ''
            )
        ) AS image_key_norm,
        s."ImageUrl"
    FROM "ShopItems" s
    WHERE lower(s."Category") = 'avatar'
),
source_norm AS (
    SELECT
        u."UserId",
        u."AvatarId",
        CASE
            WHEN u."AvatarId" IS NULL OR btrim(u."AvatarId") = '' OR lower(btrim(u."AvatarId")) = 'bear' THEN '0'
            WHEN btrim(u."AvatarId") = '0' THEN '0'
            WHEN u."AvatarId" ~ '^\d+$' THEN btrim(u."AvatarId")
            ELSE NULL
        END AS direct_avatar_id,
        lower(
            regexp_replace(
                coalesce(
                    nullif(split_part(split_part(u."AvatarId", '%2F', 2), '?', 1), ''),
                    split_part(
                        split_part(u."AvatarId", '/', array_length(regexp_split_to_array(u."AvatarId", '/'), 1)),
                        '?',
                        1
                    ),
                    u."AvatarId"
                ),
                '\.[a-z0-9]+$',
                ''
            )
        ) AS avatar_token_norm,
        lower(regexp_replace(coalesce(u."AvatarId", ''), '[^a-z0-9]+', '', 'g')) AS avatar_text_norm
    FROM usr."Users" u
),
resolved AS (
    SELECT
        sn."UserId",
        coalesce(
            sn.direct_avatar_id,
            (
                SELECT a.avatar_item_id
                FROM avatar_shop a
                WHERE lower(a."ImageUrl") = lower(sn."AvatarId")
                LIMIT 1
            ),
            (
                SELECT a.avatar_item_id
                FROM avatar_shop a
                WHERE a.image_key_norm = sn.avatar_token_norm
                LIMIT 1
            ),
            (
                SELECT a.avatar_item_id
                FROM avatar_shop a
                WHERE a.shop_name_norm = sn.avatar_text_norm
                LIMIT 1
            ),
            '0'
        ) AS resolved_avatar_id
    FROM source_norm sn
)
UPDATE usr."Users" u
SET "AvatarId" = r.resolved_avatar_id
FROM resolved r
WHERE u."UserId" = r."UserId"
  AND coalesce(u."AvatarId", '') <> coalesce(r.resolved_avatar_id, '');

COMMIT;

-- Optional verification queries:
-- SELECT "UserId", "AvatarId" FROM usr."Users" ORDER BY "UserId";
-- SELECT "Id", "Name", "Category", "ImageUrl" FROM "ShopItems" WHERE lower("Category") = 'avatar' ORDER BY "Id";
