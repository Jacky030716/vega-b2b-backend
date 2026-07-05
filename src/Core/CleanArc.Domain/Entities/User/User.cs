using CleanArc.Domain.Common;

using CleanArc.Domain.Entities.Institution;
using Microsoft.AspNetCore.Identity;

namespace CleanArc.Domain.Entities.User;

public class User : IdentityUser<int>, IEntity
{
    public User()
    {
        this.ExternalUuid = Guid.NewGuid();
        this.GeneratedCode = Guid.NewGuid().ToString().Substring(0, 8);
        this.Level = 1;
        this.Experience = 0;
        this.Diamonds = 0;
        this.AvatarId = "0"; // default avatar sentinel (no equipped shop avatar)
    }

    public string Name { get; set; }
    public string FamilyName { get; set; }
    public Guid ExternalUuid { get; set; }
    public string GeneratedCode { get; set; }

    // Institution Link
    public int? InstitutionId { get; set; }
    public Institution.Institution Institution { get; set; }

    // User Profile Data
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Diamonds { get; set; } = 0;
    public int DreamTokensCount { get; set; } = 0;
    public DateTime? LastStickerGeneratedAtUtc { get; set; }
    public string AvatarId { get; set; } = "0";
    public string AvatarUrl { get; set; }
    public bool WeeklyAiInsightsEmail { get; set; } = true;
    public bool InactiveStudentAlerts { get; set; } = true;
    public bool InAppNotificationsEnabled { get; set; } = StudentNotificationPreferenceDefaults.InAppNotificationsEnabled;
    public bool PracticeRemindersEnabled { get; set; } = StudentNotificationPreferenceDefaults.PracticeRemindersEnabled;
    public bool StreakRemindersEnabled { get; set; } = StudentNotificationPreferenceDefaults.StreakRemindersEnabled;
    public bool AchievementAlertsEnabled { get; set; } = StudentNotificationPreferenceDefaults.AchievementAlertsEnabled;
    public bool WeeklyReportsEnabled { get; set; } = StudentNotificationPreferenceDefaults.WeeklyReportsEnabled;
    public string ReminderTimeLocal { get; set; } = StudentNotificationPreferenceDefaults.ReminderTimeLocal;
    public string QuietHoursStartLocal { get; set; } = StudentNotificationPreferenceDefaults.QuietHoursStartLocal;
    public string QuietHoursEndLocal { get; set; } = StudentNotificationPreferenceDefaults.QuietHoursEndLocal;
    public string NotificationTimezone { get; set; } = StudentNotificationPreferenceDefaults.NotificationTimezone;
    public string? ExpoPushToken { get; set; }
    public DateTime? LastSrsNotificationSentAt { get; set; }

    // Password reset tracking (store hashed token only)
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public string PasswordResetTokenHash { get; set; }
    public bool PasswordResetTokenUsed { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<UserLogin> Logins { get; set; }
    public ICollection<UserClaim> Claims { get; set; }
    public ICollection<UserToken> Tokens { get; set; }
    public ICollection<UserRefreshToken> UserRefreshTokens { get; set; }

    #region Navigation Properties


    public ICollection<InstitutionUser> InstitutionMemberships { get; set; } = new List<InstitutionUser>();

    #endregion

}
