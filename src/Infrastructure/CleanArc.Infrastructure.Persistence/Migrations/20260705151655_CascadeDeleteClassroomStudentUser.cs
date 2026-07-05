using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteClassroomStudentUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AchievementTriggers_Badges_BadgeId",
                table: "AchievementTriggers");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_adaptive_agent_decisions_Users_student_id",
                table: "adaptive_agent_decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_Users_UserId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_challenge_items_vocabulary_items_vocabulary_item_id",
                table: "challenge_items");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeProgresses_Classrooms_ClassroomId",
                table: "ChallengeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeProgresses_Users_UserId",
                table: "ChallengeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_classroom_modules_Classrooms_classroom_id",
                table: "classroom_modules");

            migrationBuilder.DropForeignKey(
                name: "FK_classroom_subjects_Classrooms_classroom_id",
                table: "classroom_subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomStudents_Classrooms_ClassroomId",
                table: "ClassroomStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomStudents_Users_UserId",
                table: "ClassroomStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyCheckIns_Users_UserId",
                table: "DailyCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_DiamondTransactions_Users_UserId",
                table: "DiamondTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_error_pattern_logs_Users_student_id",
                table: "error_pattern_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_error_pattern_logs_vocabulary_items_vocabulary_item_id",
                table: "error_pattern_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_hardcore_challenge_drafts_Users_student_id",
                table: "hardcore_challenge_drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_hardcore_challenge_drafts_spelling_tests_linked_spelling_te~",
                table: "hardcore_challenge_drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_institution_users_Users_user_id",
                table: "institution_users");



            migrationBuilder.DropForeignKey(
                name: "FK_recovery_missions_Classrooms_classroom_id",
                table: "recovery_missions");

            migrationBuilder.DropForeignKey(
                name: "FK_recovery_missions_Users_student_id",
                table: "recovery_missions");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "usr",
                table: "RoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_spelling_tests_Classrooms_classroom_id",
                table: "spelling_tests");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerGiftTransactions_Users_RecipientUserId",
                table: "StickerGiftTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerGiftTransactions_Users_SenderUserId",
                table: "StickerGiftTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerInventoryItems_Users_CreatorUserId",
                table: "StickerInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerInventoryItems_Users_OwnerUserId",
                table: "StickerInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_attempts_Users_student_id",
                table: "student_challenge_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_student_challenge_attempts_~",
                table: "student_challenge_item_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_vocabulary_items_vocabulary~",
                table: "student_challenge_item_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_skill_profiles_Users_student_id",
                table: "student_skill_profiles");



            migrationBuilder.DropForeignKey(
                name: "FK_student_spelling_test_attempts_spelling_tests_spelling_test_",
                table: "student_spelling_test_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_word_mastery_Users_student_id",
                table: "student_word_mastery");

            migrationBuilder.DropForeignKey(
                name: "FK_student_word_mastery_vocabulary_items_vocabulary_item_id",
                table: "student_word_mastery");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCredentials_Classrooms_ClassroomId",
                schema: "usr",
                table: "StudentCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCredentials_Users_UserId",
                schema: "usr",
                table: "StudentCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_user_notifications_Users_user_id",
                table: "user_notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievementEvents_Users_UserId",
                table: "UserAchievementEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadgeProgresses_Badges_BadgeId",
                table: "UserBadgeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadgeProgresses_Users_UserId",
                table: "UserBadgeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Users_UserId",
                table: "UserBadges");

            migrationBuilder.DropForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "usr",
                table: "UserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEquippedItems_Users_UserId",
                table: "UserEquippedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInventoryItems_Users_UserId",
                table: "UserInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "usr",
                table: "UserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMissionProgresses_Users_UserId",
                table: "UserMissionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMissions_Users_UserId",
                table: "UserMissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId",
                schema: "usr",
                table: "UserRefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "usr",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "usr",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserStreaks_Users_UserId",
                table: "UserStreaks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "usr",
                table: "UserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_vocabulary_syllable_infos_vocabulary_items_vocabulary_item_~",
                table: "vocabulary_syllable_infos");

            migrationBuilder.DropForeignKey(
                name: "FK_vocabulary_translations_vocabulary_items_vocabulary_item_id",
                table: "vocabulary_translations");

            migrationBuilder.DropForeignKey(
                name: "FK_word_progress_Users_student_id",
                table: "word_progress");

            migrationBuilder.DropForeignKey(
                name: "FK_word_progress_vocabulary_items_word_id",
                table: "word_progress");



            migrationBuilder.AddForeignKey(
                name: "FK_AchievementTriggers_Badges_BadgeId",
                table: "AchievementTriggers",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_adaptive_agent_decisions_Users_student_id",
                table: "adaptive_agent_decisions",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_Users_UserId",
                table: "Attempts",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_challenge_items_vocabulary_items_vocabulary_item_id",
                table: "challenge_items",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeProgresses_Classrooms_ClassroomId",
                table: "ChallengeProgresses",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeProgresses_Users_UserId",
                table: "ChallengeProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_classroom_modules_Classrooms_classroom_id",
                table: "classroom_modules",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_classroom_subjects_Classrooms_classroom_id",
                table: "classroom_subjects",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomStudents_Classrooms_ClassroomId",
                table: "ClassroomStudents",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomStudents_Users_UserId",
                table: "ClassroomStudents",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyCheckIns_Users_UserId",
                table: "DailyCheckIns",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DiamondTransactions_Users_UserId",
                table: "DiamondTransactions",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_error_pattern_logs_Users_student_id",
                table: "error_pattern_logs",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_error_pattern_logs_vocabulary_items_vocabulary_item_id",
                table: "error_pattern_logs",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_hardcore_challenge_drafts_Users_student_id",
                table: "hardcore_challenge_drafts",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_hardcore_challenge_drafts_spelling_tests_linked_spelling_te~",
                table: "hardcore_challenge_drafts",
                column: "linked_spelling_test_id",
                principalTable: "spelling_tests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_institution_users_Users_user_id",
                table: "institution_users",
                column: "user_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);



            migrationBuilder.AddForeignKey(
                name: "FK_recovery_missions_Classrooms_classroom_id",
                table: "recovery_missions",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recovery_missions_Users_student_id",
                table: "recovery_missions",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "usr",
                table: "RoleClaims",
                column: "RoleId",
                principalSchema: "usr",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_spelling_tests_Classrooms_classroom_id",
                table: "spelling_tests",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerGiftTransactions_Users_RecipientUserId",
                table: "StickerGiftTransactions",
                column: "RecipientUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerGiftTransactions_Users_SenderUserId",
                table: "StickerGiftTransactions",
                column: "SenderUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerInventoryItems_Users_CreatorUserId",
                table: "StickerInventoryItems",
                column: "CreatorUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerInventoryItems_Users_OwnerUserId",
                table: "StickerInventoryItems",
                column: "OwnerUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_attempts_Users_student_id",
                table: "student_challenge_attempts",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_student_challenge_attempts_~",
                table: "student_challenge_item_attempts",
                column: "student_challenge_attempt_id",
                principalTable: "student_challenge_attempts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_vocabulary_items_vocabulary~",
                table: "student_challenge_item_attempts",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_skill_profiles_Users_student_id",
                table: "student_skill_profiles",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);



            migrationBuilder.AddForeignKey(
                name: "FK_student_spelling_test_attempts_spelling_tests_spelling_test_",
                table: "student_spelling_test_attempts",
                column: "spelling_test_id",
                principalTable: "spelling_tests",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_word_mastery_Users_student_id",
                table: "student_word_mastery",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_student_word_mastery_vocabulary_items_vocabulary_item_id",
                table: "student_word_mastery",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCredentials_Classrooms_ClassroomId",
                schema: "usr",
                table: "StudentCredentials",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCredentials_Users_UserId",
                schema: "usr",
                table: "StudentCredentials",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_notifications_Users_user_id",
                table: "user_notifications",
                column: "user_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievementEvents_Users_UserId",
                table: "UserAchievementEvents",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadgeProgresses_Badges_BadgeId",
                table: "UserBadgeProgresses",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadgeProgresses_Users_UserId",
                table: "UserBadgeProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Users_UserId",
                table: "UserBadges",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "usr",
                table: "UserClaims",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserEquippedItems_Users_UserId",
                table: "UserEquippedItems",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInventoryItems_Users_UserId",
                table: "UserInventoryItems",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "usr",
                table: "UserLogins",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMissionProgresses_Users_UserId",
                table: "UserMissionProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMissions_Users_UserId",
                table: "UserMissions",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId",
                schema: "usr",
                table: "UserRefreshTokens",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "usr",
                table: "UserRoles",
                column: "RoleId",
                principalSchema: "usr",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "usr",
                table: "UserRoles",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserStreaks_Users_UserId",
                table: "UserStreaks",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "usr",
                table: "UserTokens",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_vocabulary_syllable_infos_vocabulary_items_vocabulary_item_~",
                table: "vocabulary_syllable_infos",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_vocabulary_translations_vocabulary_items_vocabulary_item_id",
                table: "vocabulary_translations",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_word_progress_Users_student_id",
                table: "word_progress",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_word_progress_vocabulary_items_word_id",
                table: "word_progress",
                column: "word_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AchievementTriggers_Badges_BadgeId",
                table: "AchievementTriggers");

            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_adaptive_agent_decisions_Users_student_id",
                table: "adaptive_agent_decisions");

            migrationBuilder.DropForeignKey(
                name: "FK_Attempts_Users_UserId",
                table: "Attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_challenge_items_vocabulary_items_vocabulary_item_id",
                table: "challenge_items");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeProgresses_Classrooms_ClassroomId",
                table: "ChallengeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_ChallengeProgresses_Users_UserId",
                table: "ChallengeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges");

            migrationBuilder.DropForeignKey(
                name: "FK_classroom_modules_Classrooms_classroom_id",
                table: "classroom_modules");

            migrationBuilder.DropForeignKey(
                name: "FK_classroom_subjects_Classrooms_classroom_id",
                table: "classroom_subjects");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomStudents_Classrooms_ClassroomId",
                table: "ClassroomStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassroomStudents_Users_UserId",
                table: "ClassroomStudents");

            migrationBuilder.DropForeignKey(
                name: "FK_DailyCheckIns_Users_UserId",
                table: "DailyCheckIns");

            migrationBuilder.DropForeignKey(
                name: "FK_DiamondTransactions_Users_UserId",
                table: "DiamondTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_error_pattern_logs_Users_student_id",
                table: "error_pattern_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_error_pattern_logs_vocabulary_items_vocabulary_item_id",
                table: "error_pattern_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_hardcore_challenge_drafts_Users_student_id",
                table: "hardcore_challenge_drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_hardcore_challenge_drafts_spelling_tests_linked_spelling_te~",
                table: "hardcore_challenge_drafts");

            migrationBuilder.DropForeignKey(
                name: "FK_institution_users_Users_user_id",
                table: "institution_users");



            migrationBuilder.DropForeignKey(
                name: "FK_recovery_missions_Classrooms_classroom_id",
                table: "recovery_missions");

            migrationBuilder.DropForeignKey(
                name: "FK_recovery_missions_Users_student_id",
                table: "recovery_missions");

            migrationBuilder.DropForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "usr",
                table: "RoleClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_spelling_tests_Classrooms_classroom_id",
                table: "spelling_tests");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerGiftTransactions_Users_RecipientUserId",
                table: "StickerGiftTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerGiftTransactions_Users_SenderUserId",
                table: "StickerGiftTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerInventoryItems_Users_CreatorUserId",
                table: "StickerInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_StickerInventoryItems_Users_OwnerUserId",
                table: "StickerInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_attempts_Users_student_id",
                table: "student_challenge_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_student_challenge_attempts_~",
                table: "student_challenge_item_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_challenge_item_attempts_vocabulary_items_vocabulary~",
                table: "student_challenge_item_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_skill_profiles_Users_student_id",
                table: "student_skill_profiles");



            migrationBuilder.DropForeignKey(
                name: "FK_student_spelling_test_attempts_spelling_tests_spelling_test_",
                table: "student_spelling_test_attempts");

            migrationBuilder.DropForeignKey(
                name: "FK_student_word_mastery_Users_student_id",
                table: "student_word_mastery");

            migrationBuilder.DropForeignKey(
                name: "FK_student_word_mastery_vocabulary_items_vocabulary_item_id",
                table: "student_word_mastery");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCredentials_Classrooms_ClassroomId",
                schema: "usr",
                table: "StudentCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentCredentials_Users_UserId",
                schema: "usr",
                table: "StudentCredentials");

            migrationBuilder.DropForeignKey(
                name: "FK_user_notifications_Users_user_id",
                table: "user_notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAchievementEvents_Users_UserId",
                table: "UserAchievementEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadgeProgresses_Badges_BadgeId",
                table: "UserBadgeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadgeProgresses_Users_UserId",
                table: "UserBadgeProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges");

            migrationBuilder.DropForeignKey(
                name: "FK_UserBadges_Users_UserId",
                table: "UserBadges");

            migrationBuilder.DropForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "usr",
                table: "UserClaims");

            migrationBuilder.DropForeignKey(
                name: "FK_UserEquippedItems_Users_UserId",
                table: "UserEquippedItems");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInventoryItems_Users_UserId",
                table: "UserInventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "usr",
                table: "UserLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMissionProgresses_Users_UserId",
                table: "UserMissionProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMissions_Users_UserId",
                table: "UserMissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId",
                schema: "usr",
                table: "UserRefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "usr",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "usr",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserStreaks_Users_UserId",
                table: "UserStreaks");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "usr",
                table: "UserTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_vocabulary_syllable_infos_vocabulary_items_vocabulary_item_~",
                table: "vocabulary_syllable_infos");

            migrationBuilder.DropForeignKey(
                name: "FK_vocabulary_translations_vocabulary_items_vocabulary_item_id",
                table: "vocabulary_translations");

            migrationBuilder.DropForeignKey(
                name: "FK_word_progress_Users_student_id",
                table: "word_progress");

            migrationBuilder.DropForeignKey(
                name: "FK_word_progress_vocabulary_items_word_id",
                table: "word_progress");



            migrationBuilder.AddForeignKey(
                name: "FK_AchievementTriggers_Badges_BadgeId",
                table: "AchievementTriggers",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_Users_UserId",
                table: "ActivityLogs",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_adaptive_agent_decisions_Users_student_id",
                table: "adaptive_agent_decisions",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Attempts_Users_UserId",
                table: "Attempts",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_challenge_items_vocabulary_items_vocabulary_item_id",
                table: "challenge_items",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeProgresses_Classrooms_ClassroomId",
                table: "ChallengeProgresses",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChallengeProgresses_Users_UserId",
                table: "ChallengeProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Classrooms_ClassroomId",
                table: "Challenges",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Challenges_Users_student_id",
                table: "Challenges",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_classroom_modules_Classrooms_classroom_id",
                table: "classroom_modules",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_classroom_subjects_Classrooms_classroom_id",
                table: "classroom_subjects",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomStudents_Classrooms_ClassroomId",
                table: "ClassroomStudents",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassroomStudents_Users_UserId",
                table: "ClassroomStudents",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DailyCheckIns_Users_UserId",
                table: "DailyCheckIns",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DiamondTransactions_Users_UserId",
                table: "DiamondTransactions",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_error_pattern_logs_Users_student_id",
                table: "error_pattern_logs",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_error_pattern_logs_vocabulary_items_vocabulary_item_id",
                table: "error_pattern_logs",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_hardcore_challenge_drafts_Users_student_id",
                table: "hardcore_challenge_drafts",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_hardcore_challenge_drafts_spelling_tests_linked_spelling_te~",
                table: "hardcore_challenge_drafts",
                column: "linked_spelling_test_id",
                principalTable: "spelling_tests",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_institution_users_Users_user_id",
                table: "institution_users",
                column: "user_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);



            migrationBuilder.AddForeignKey(
                name: "FK_recovery_missions_Classrooms_classroom_id",
                table: "recovery_missions",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_recovery_missions_Users_student_id",
                table: "recovery_missions",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoleClaims_Roles_RoleId",
                schema: "usr",
                table: "RoleClaims",
                column: "RoleId",
                principalSchema: "usr",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_spelling_tests_Classrooms_classroom_id",
                table: "spelling_tests",
                column: "classroom_id",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerGiftTransactions_Users_RecipientUserId",
                table: "StickerGiftTransactions",
                column: "RecipientUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerGiftTransactions_Users_SenderUserId",
                table: "StickerGiftTransactions",
                column: "SenderUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerInventoryItems_Users_CreatorUserId",
                table: "StickerInventoryItems",
                column: "CreatorUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StickerInventoryItems_Users_OwnerUserId",
                table: "StickerInventoryItems",
                column: "OwnerUserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_attempts_Users_student_id",
                table: "student_challenge_attempts",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_student_challenge_attempts_~",
                table: "student_challenge_item_attempts",
                column: "student_challenge_attempt_id",
                principalTable: "student_challenge_attempts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_challenge_item_attempts_vocabulary_items_vocabulary~",
                table: "student_challenge_item_attempts",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_student_skill_profiles_Users_student_id",
                table: "student_skill_profiles",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);



            migrationBuilder.AddForeignKey(
                name: "FK_student_spelling_test_attempts_spelling_tests_spelling_test_",
                table: "student_spelling_test_attempts",
                column: "spelling_test_id",
                principalTable: "spelling_tests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_word_mastery_Users_student_id",
                table: "student_word_mastery",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_student_word_mastery_vocabulary_items_vocabulary_item_id",
                table: "student_word_mastery",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCredentials_Classrooms_ClassroomId",
                schema: "usr",
                table: "StudentCredentials",
                column: "ClassroomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentCredentials_Users_UserId",
                schema: "usr",
                table: "StudentCredentials",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_user_notifications_Users_user_id",
                table: "user_notifications",
                column: "user_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAchievementEvents_Users_UserId",
                table: "UserAchievementEvents",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadgeProgresses_Badges_BadgeId",
                table: "UserBadgeProgresses",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadgeProgresses_Users_UserId",
                table: "UserBadgeProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Badges_BadgeId",
                table: "UserBadges",
                column: "BadgeId",
                principalTable: "Badges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBadges_Users_UserId",
                table: "UserBadges",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserClaims_Users_UserId",
                schema: "usr",
                table: "UserClaims",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserEquippedItems_Users_UserId",
                table: "UserEquippedItems",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInventoryItems_Users_UserId",
                table: "UserInventoryItems",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserLogins_Users_UserId",
                schema: "usr",
                table: "UserLogins",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMissionProgresses_Users_UserId",
                table: "UserMissionProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMissions_Users_UserId",
                table: "UserMissions",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProgresses_Users_UserId",
                table: "UserProgresses",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRefreshTokens_Users_UserId",
                schema: "usr",
                table: "UserRefreshTokens",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                schema: "usr",
                table: "UserRoles",
                column: "RoleId",
                principalSchema: "usr",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                schema: "usr",
                table: "UserRoles",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserStreaks_Users_UserId",
                table: "UserStreaks",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTokens_Users_UserId",
                schema: "usr",
                table: "UserTokens",
                column: "UserId",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vocabulary_syllable_infos_vocabulary_items_vocabulary_item_~",
                table: "vocabulary_syllable_infos",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_vocabulary_translations_vocabulary_items_vocabulary_item_id",
                table: "vocabulary_translations",
                column: "vocabulary_item_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_word_progress_Users_student_id",
                table: "word_progress",
                column: "student_id",
                principalSchema: "usr",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_word_progress_vocabulary_items_word_id",
                table: "word_progress",
                column: "word_id",
                principalTable: "vocabulary_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);


        }
    }
}
