-- Mock Badge Data Script - Multi-type Progress Tracking
-- Inserts 4 test badges with different progress aggregation types
-- to demonstrate challenge counts, streaks, diamonds earned/spent
-- 
-- Run directly: psql -U postgres -d your_db -f insert_mock_badge_data.sql
-- Or in psql client: \i insert_mock_badge_data.sql

BEGIN;

-- ============================================================================
-- 1. INSERT MOCK BADGES (4 different progress types)
-- ============================================================================

-- Badge 1: Complete 3 Challenges (count aggregation)
INSERT INTO "Badges" (
  "Name",
  "Description",
  "ImageRef",
  "Category",
  "Rarity",
  "Requirement",
  "RuleJson",
  "IsSecret",
  "CreatedTime"
) VALUES (
  'Challenge Master',
  'Complete 3 mini-games successfully',
  'badges/challenge_master.png',
  'quiz',
  'silver',
  'Win 3 challenge attempts',
  '{"aggregation": "count", "threshold": 3}',
  false,
  NOW()
) ON CONFLICT DO NOTHING;

-- Badge 2: 7-Day Consecutive Check-in (streak aggregation)
INSERT INTO "Badges" (
  "Name",
  "Description",
  "ImageRef",
  "Category",
  "Rarity",
  "Requirement",
  "RuleJson",
  "IsSecret",
  "CreatedTime"
) VALUES (
  'Streak Champion',
  'Check in for 7 consecutive days',
  'badges/streak_champion.png',
  'streak',
  'gold',
  'Maintain a 7-day check-in streak',
  '{"aggregation": "current_streak", "threshold": 7, "sourceField": "currentStreak"}',
  false,
  NOW()
) ON CONFLICT DO NOTHING;

-- Badge 3: Earn 5000 Diamonds (sum aggregation)
INSERT INTO "Badges" (
  "Name",
  "Description",
  "ImageRef",
  "Category",
  "Rarity",
  "Requirement",
  "RuleJson",
  "IsSecret",
  "CreatedTime"
) VALUES (
  'Diamond Collector',
  'Accumulate 5000 diamonds from rewards',
  'badges/diamond_collector.png',
  'milestone',
  'crystal',
  'Earn 5000 diamonds from gameplay',
  '{"aggregation": "sum", "sourceField": "diamond_earned", "threshold": 5000}',
  false,
  NOW()
) ON CONFLICT DO NOTHING;

-- Badge 4: Spend 1000 Diamonds (sum aggregation)
INSERT INTO "Badges" (
  "Name",
  "Description",
  "ImageRef",
  "Category",
  "Rarity",
  "Requirement",
  "RuleJson",
  "IsSecret",
  "CreatedTime"
) VALUES (
  'Spender Extraordinaire',
  'Spend 1000 diamonds in the shop',
  'badges/spender.png',
  'shop',
  'gold',
  'Purchase items totaling 1000 diamonds',
  '{"aggregation": "sum", "sourceField": "diamond_spent", "threshold": 1000}',
  false,
  NOW()
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- 2. INSERT ACHIEVEMENT TRIGGERS (1 per badge)
-- ============================================================================

-- Trigger 1: Challenge attempts count
INSERT INTO "AchievementTriggers" (
  "BadgeId",
  "EventType",
  "Description",
  "AggregationType",
  "AggregationSourceField",
  "Threshold",
  "FilterConditionsJson",
  "IsRepeatable",
  "SupportedContexts",
  "IsActive",
  "EvaluationOrder",
  "CreatedTime"
) SELECT
  b."Id",
  'attempt_completed',
  'Count successful game attempts',
  'count',
  NULL,
  3,
  '[]',
  false,
  'quiz,game',
  true,
  1,
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Challenge Master'
  AND NOT EXISTS (
    SELECT 1 FROM "AchievementTriggers" t 
    WHERE t."BadgeId" = b."Id" AND t."EventType" = 'attempt_completed'
  );

-- Trigger 2: Consecutive day check-in streak
INSERT INTO "AchievementTriggers" (
  "BadgeId",
  "EventType",
  "Description",
  "AggregationType",
  "AggregationSourceField",
  "Threshold",
  "FilterConditionsJson",
  "IsRepeatable",
  "SupportedContexts",
  "IsActive",
  "EvaluationOrder",
  "CreatedTime"
) SELECT
  b."Id",
  'daily_check_in',
  'Track daily check-in streak',
  'current_streak',
  'currentStreak',
  7,
  '[]',
  false,
  'checkin',
  true,
  1,
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Streak Champion'
  AND NOT EXISTS (
    SELECT 1 FROM "AchievementTriggers" t 
    WHERE t."BadgeId" = b."Id" AND t."EventType" = 'daily_check_in'
  );

-- Trigger 3: Total diamonds earned
INSERT INTO "AchievementTriggers" (
  "BadgeId",
  "EventType",
  "Description",
  "AggregationType",
  "AggregationSourceField",
  "Threshold",
  "FilterConditionsJson",
  "IsRepeatable",
  "SupportedContexts",
  "IsActive",
  "EvaluationOrder",
  "CreatedTime"
) SELECT
  b."Id",
  'diamond_earned',
  'Accumulate diamonds from rewards',
  'sum',
  'diamond_earned',
  5000,
  '[]',
  false,
  'reward,checkin,game',
  true,
  1,
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Diamond Collector'
  AND NOT EXISTS (
    SELECT 1 FROM "AchievementTriggers" t 
    WHERE t."BadgeId" = b."Id" AND t."EventType" = 'diamond_earned'
  );

-- Trigger 4: Total diamonds spent
INSERT INTO "AchievementTriggers" (
  "BadgeId",
  "EventType",
  "Description",
  "AggregationType",
  "AggregationSourceField",
  "Threshold",
  "FilterConditionsJson",
  "IsRepeatable",
  "SupportedContexts",
  "IsActive",
  "EvaluationOrder",
  "CreatedTime"
) SELECT
  b."Id",
  'diamond_spent',
  'Track total diamonds spent',
  'sum',
  'diamond_spent',
  1000,
  '[]',
  false,
  'shop',
  true,
  1,
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Spender Extraordinaire'
  AND NOT EXISTS (
    SELECT 1 FROM "AchievementTriggers" t 
    WHERE t."BadgeId" = b."Id" AND t."EventType" = 'diamond_spent'
  );

-- ============================================================================
-- 3. INSERT MOCK USER PROGRESS (Optional - demonstrates in-progress badges)
-- ============================================================================
-- NOTE: Replace user_id 1 with an actual user ID from your usr."Users" table

-- Progress 1: Challenge attempts in progress (2 of 3)
INSERT INTO "UserBadgeProgresses" (
  "UserId",
  "BadgeId",
  "ProgressValue",
  "LastEvaluatedAt",
  "CreatedTime"
) SELECT
  1,  -- Replace with actual user ID
  b."Id",
  2,  -- 2 attempts completed
  NOW(),
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Challenge Master'
  AND NOT EXISTS (
    SELECT 1 FROM "UserBadgeProgresses" p 
    WHERE p."BadgeId" = b."Id" AND p."UserId" = 1
  )
ON CONFLICT DO NOTHING;

-- Progress 2: Streak in progress (5 of 7 days)
INSERT INTO "UserBadgeProgresses" (
  "UserId",
  "BadgeId",
  "ProgressValue",
  "LastEvaluatedAt",
  "CreatedTime"
) SELECT
  1,  -- Replace with actual user ID
  b."Id",
  5,  -- 5 consecutive days
  NOW(),
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Streak Champion'
  AND NOT EXISTS (
    SELECT 1 FROM "UserBadgeProgresses" p 
    WHERE p."BadgeId" = b."Id" AND p."UserId" = 1
  )
ON CONFLICT DO NOTHING;

-- Progress 3: Diamonds earned in progress (3200 of 5000)
INSERT INTO "UserBadgeProgresses" (
  "UserId",
  "BadgeId",
  "ProgressValue",
  "LastEvaluatedAt",
  "CreatedTime"
) SELECT
  1,  -- Replace with actual user ID
  b."Id",
  3200,  -- 3200 diamonds accumulated
  NOW(),
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Diamond Collector'
  AND NOT EXISTS (
    SELECT 1 FROM "UserBadgeProgresses" p 
    WHERE p."BadgeId" = b."Id" AND p."UserId" = 1
  )
ON CONFLICT DO NOTHING;

-- Progress 4: Diamonds spent in progress (650 of 1000)
INSERT INTO "UserBadgeProgresses" (
  "UserId",
  "BadgeId",
  "ProgressValue",
  "LastEvaluatedAt",
  "CreatedTime"
) SELECT
  1,  -- Replace with actual user ID
  b."Id",
  650,  -- 650 diamonds spent
  NOW(),
  NOW()
FROM "Badges" b 
WHERE b."Name" = 'Spender Extraordinaire'
  AND NOT EXISTS (
    SELECT 1 FROM "UserBadgeProgresses" p 
    WHERE p."BadgeId" = b."Id" AND p."UserId" = 1
  )
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 4. VERIFICATION QUERIES
-- ============================================================================

-- Show inserted badges
SELECT '--- INSERTED BADGES ---' AS info;
SELECT 
  "Id",
  "Name",
  "Category",
  "Rarity",
  "Requirement"
FROM "Badges"
WHERE "Name" IN ('Challenge Master', 'Streak Champion', 'Diamond Collector', 'Spender Extraordinaire')
ORDER BY "Id";

-- Show inserted triggers with their configurations
SELECT '--- INSERTED TRIGGERS ---' AS info;
SELECT 
  t."Id",
  b."Name" AS "BadgeName",
  t."EventType",
  t."AggregationType",
  t."AggregationSourceField",
  t."Threshold",
  t."Description"
FROM "AchievementTriggers" t
JOIN "Badges" b ON t."BadgeId" = b."Id"
WHERE b."Name" IN ('Challenge Master', 'Streak Champion', 'Diamond Collector', 'Spender Extraordinaire')
ORDER BY b."Name";

-- Show user progress if any exists
SELECT '--- USER BADGE PROGRESS (User ID = 1) ---' AS info;
SELECT 
  p."UserId",
  b."Name" AS "BadgeName",
  p."ProgressValue" AS "CurrentProgress",
  t."Threshold" AS "TargetProgress",
  ROUND((p."ProgressValue" / t."Threshold" * 100)::numeric, 1) || '%' AS "ProgressPercent"
FROM "UserBadgeProgresses" p
JOIN "Badges" b ON p."BadgeId" = b."Id"
JOIN "AchievementTriggers" t ON b."Id" = t."BadgeId"
WHERE p."UserId" = 1
ORDER BY b."Name";

COMMIT;

-- ============================================================================
-- USAGE NOTES:
-- ============================================================================
-- 1. Before running this script, ensure you have at least one user in usr."Users"
--    Replace user_id = 1 with an actual user ID if needed
--
-- 2. To find a valid user ID:
--    SELECT "UserId" FROM usr."Users" LIMIT 1;
--
-- 3. The badges are configured to track 4 different progress types:
--    - Challenge Master: count aggregation (3 attempts)
--    - Streak Champion: current_streak aggregation (7 consecutive days)
--    - Diamond Collector: sum aggregation for earned diamonds (5000 total)
--    - Spender Extraordinaire: sum aggregation for spent diamonds (1000 total)
--
-- 4. To test the progress endpoints after insertion:
--    curl -H "Authorization: Bearer <token>" \
--         http://localhost:5002/api/v1.1/Badges/user/progress
--
-- 5. You can modify the progress values in the INSERT statements above
--    to test different completion percentages
-- ============================================================================
