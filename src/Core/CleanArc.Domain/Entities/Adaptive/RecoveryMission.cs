using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.AI;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Adaptive;

public class RecoveryMission : BaseEntity<int>
{
    public int StudentId { get; set; }
    public User.User Student { get; set; } = null!;

    public int ClassroomId { get; set; }
    public CleanArc.Domain.Entities.Classroom.Classroom Classroom { get; set; } = null!;

    public int? ModuleId { get; set; }
    public SyllabusModule? Module { get; set; }

    public string SourceType { get; set; } = RecoveryMissionSourceTypes.PredefinedModuleRecovery;
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RecommendedGameType { get; set; } = "SPELL_CATCHER";
    public int DifficultyLevel { get; set; } = 1;
    public string TargetWordsJson { get; set; } = "[]";
    public string ConfigJson { get; set; } = "{}";
    public string RewardJson { get; set; } = "{\"xp\":50,\"diamonds\":2}";
    public string Status { get; set; } = RecoveryMissionStatuses.Pending;
    public string GeneratedBy { get; set; } = RecoveryMissionGeneratedBy.System;
    public int? ApprovedByTeacherId { get; set; }
    public User.User? ApprovedByTeacher { get; set; }
    public int? AiAuditLogId { get; set; }
    public AiAuditLog? AiAuditLog { get; set; }
    public DateTime? AvailableUntil { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ArchiveAt { get; set; }
    public int? LinkedChallengeId { get; set; }
    public Challenge? LinkedChallenge { get; set; }
    public string WeakSkill { get; set; } = "MIXED";
    public string TriggerSnapshotJson { get; set; } = "{}";
    public bool RewardClaimed { get; set; }
}

public static class RecoveryMissionSourceTypes
{
    public const string PredefinedModuleRecovery = "PREDEFINED_MODULE_RECOVERY";
    public const string CustomSkillRecovery = "CUSTOM_SKILL_RECOVERY";
}

public static class RecoveryMissionStatuses
{
    public const string Pending = "PENDING";
    public const string Active = "ACTIVE";
    public const string Completed = "COMPLETED";
    public const string Expired = "EXPIRED";
    public const string Archived = "ARCHIVED";
}

public static class RecoveryMissionGeneratedBy
{
    public const string Ai = "AI";
    public const string Teacher = "TEACHER";
    public const string System = "SYSTEM";
}
