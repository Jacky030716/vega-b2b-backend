ALTER TABLE usr."Users"
    ADD COLUMN IF NOT EXISTS "AchievementAlertsEnabled" boolean NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "InAppNotificationsEnabled" boolean NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "NotificationTimezone" character varying(100) NOT NULL DEFAULT 'UTC',
    ADD COLUMN IF NOT EXISTS "PracticeRemindersEnabled" boolean NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "QuietHoursEndLocal" character varying(5) NOT NULL DEFAULT '08:00',
    ADD COLUMN IF NOT EXISTS "QuietHoursStartLocal" character varying(5) NOT NULL DEFAULT '22:00',
    ADD COLUMN IF NOT EXISTS "ReminderTimeLocal" character varying(5) NOT NULL DEFAULT '18:00',
    ADD COLUMN IF NOT EXISTS "StreakRemindersEnabled" boolean NOT NULL DEFAULT TRUE,
    ADD COLUMN IF NOT EXISTS "WeeklyReportsEnabled" boolean NOT NULL DEFAULT TRUE;
