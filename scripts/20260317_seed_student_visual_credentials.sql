-- Seed script for student visual login test data.
-- Safe to run multiple times (idempotent).
--
-- What this does:
-- 1) Ensures classroom memberships for existing student users (if any).
-- 2) Creates/updates usr."StudentCredentials" with deterministic login codes.
-- 3) Stores bcrypt hashes for visual sequence: icon_01-icon_02-icon_03

BEGIN;

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Pick existing users with student role.
WITH student_users AS (
    SELECT
        u."UserId",
        ROW_NUMBER() OVER (ORDER BY u."UserId") AS rn
    FROM usr."Users" u
    JOIN usr."UserRoles" ur ON ur."UserId" = u."UserId"
    JOIN usr."Roles" r ON r."Id" = ur."RoleId"
    WHERE LOWER(r."Name") = 'student'
),
class_targets AS (
    -- Map first 5 student users into the three seeded classrooms.
    SELECT c."Id" AS classroom_id, 1 AS slot
    FROM "Classrooms" c
    WHERE c."JoinCode" = 'E5A1'
    UNION ALL
    SELECT c."Id", 2 FROM "Classrooms" c WHERE c."JoinCode" = 'E5A1'
    UNION ALL
    SELECT c."Id", 3 FROM "Classrooms" c WHERE c."JoinCode" = 'E5A1'
    UNION ALL
    SELECT c."Id", 2 FROM "Classrooms" c WHERE c."JoinCode" = 'M5X2'
    UNION ALL
    SELECT c."Id", 3 FROM "Classrooms" c WHERE c."JoinCode" = 'M5X2'
    UNION ALL
    SELECT c."Id", 4 FROM "Classrooms" c WHERE c."JoinCode" = 'M5X2'
    UNION ALL
    SELECT c."Id", 3 FROM "Classrooms" c WHERE c."JoinCode" = 'S4T3'
    UNION ALL
    SELECT c."Id", 4 FROM "Classrooms" c WHERE c."JoinCode" = 'S4T3'
    UNION ALL
    SELECT c."Id", 5 FROM "Classrooms" c WHERE c."JoinCode" = 'S4T3'
),
membership_source AS (
    SELECT
        ct.classroom_id,
        su."UserId"
    FROM class_targets ct
    JOIN student_users su ON su.rn = ct.slot
)
INSERT INTO "ClassroomStudents" (
    "ClassroomId",
    "UserId",
    "JoinedDate",
    "CreatedTime",
    "ModifiedDate"
)
SELECT
    ms.classroom_id,
    ms."UserId",
    NOW(),
    NOW(),
    NOW()
FROM membership_source ms
ON CONFLICT ("ClassroomId", "UserId") DO NOTHING;

-- Create visual credentials for each classroom membership.
-- Login code format: STU + first 6 chars of md5(classroomId-userId), uppercased.
INSERT INTO usr."StudentCredentials" (
    "UserId",
    "ClassroomId",
    "StudentLoginCode",
    "VisualPasswordHash",
    "IsActive",
    "FailedAttempts",
    "CreatedTime",
    "ModifiedDate"
)
SELECT
    cs."UserId",
    cs."ClassroomId",
    UPPER('STU' || SUBSTRING(MD5(cs."ClassroomId"::text || '-' || cs."UserId"::text), 1, 6)) AS login_code,
    crypt('icon_01-icon_02-icon_03', gen_salt('bf', 10)) AS visual_hash,
    TRUE,
    0,
    NOW(),
    NOW()
FROM "ClassroomStudents" cs
JOIN "Classrooms" c ON c."Id" = cs."ClassroomId"
WHERE c."JoinCode" IN ('E5A1', 'M5X2', 'S4T3')
ON CONFLICT ("ClassroomId", "UserId") DO UPDATE
SET
    "StudentLoginCode" = EXCLUDED."StudentLoginCode",
    "VisualPasswordHash" = EXCLUDED."VisualPasswordHash",
    "IsActive" = TRUE,
    "FailedAttempts" = 0,
    "ModifiedDate" = NOW();

COMMIT;

-- Verification query 1: view generated student login codes by classroom
SELECT
    c."Name" AS classroom_name,
    c."JoinCode" AS class_code,
    u."UserId",
    COALESCE(NULLIF(u."Name", ''), u."UserName") AS student_name,
    sc."StudentLoginCode",
    sc."IsActive",
    sc."FailedAttempts"
FROM usr."StudentCredentials" sc
JOIN usr."Users" u ON u."UserId" = sc."UserId"
JOIN "Classrooms" c ON c."Id" = sc."ClassroomId"
ORDER BY c."JoinCode", u."UserId";

-- Verification query 2: class membership counts
SELECT
    c."JoinCode",
    c."Name",
    COUNT(cs."Id") AS student_count
FROM "Classrooms" c
LEFT JOIN "ClassroomStudents" cs ON cs."ClassroomId" = c."Id"
WHERE c."JoinCode" IN ('E5A1', 'M5X2', 'S4T3')
GROUP BY c."Id", c."JoinCode", c."Name"
ORDER BY c."JoinCode";
