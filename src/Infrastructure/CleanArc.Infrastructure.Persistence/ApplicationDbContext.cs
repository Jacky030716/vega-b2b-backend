using System.Reflection;
using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Adaptive;
using CleanArc.Domain.Entities.Achievement;
using CleanArc.Domain.Entities.Activity;
using CleanArc.Domain.Entities.Classroom;
using CleanArc.Domain.Entities.Institution;
using CleanArc.Domain.Entities.Mission;
using CleanArc.Domain.Entities.Progression;
using CleanArc.Domain.Entities.Quiz;
using CleanArc.Domain.Entities.Shop;
using CleanArc.Domain.Entities.Sticker;
using CleanArc.Domain.Entities.Streak;
using CleanArc.Domain.Entities.User;
using CleanArc.Domain.Entities.Word;
using CleanArc.SharedKernel.Extensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<User, Role, int, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>
{
    public ApplicationDbContext(DbContextOptions options)
        : base(options)
    {
        base.SavingChanges += OnSavingChanges;
    }

    public DbSet<VisualIcon> VisualIcons { get; set; }

    // Admin & Billing
    public DbSet<Institution> Institutions { get; set; }

    // Word
    public DbSet<Word> Words { get; set; }

    // Quiz & Games
    public DbSet<Game> Games { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<Attempt> Attempts { get; set; }
    public DbSet<ChallengeProgress> ChallengeProgresses { get; set; }

    // Adaptive learning
    public DbSet<SyllabusModule> SyllabusModules { get; set; }
    public DbSet<VocabularyItem> VocabularyItems { get; set; }
    public DbSet<GameTemplate> GameTemplates { get; set; }
    public DbSet<ChallengeItem> ChallengeItems { get; set; }
    public DbSet<StudentChallengeAttempt> StudentChallengeAttempts { get; set; }
    public DbSet<StudentChallengeItemAttempt> StudentChallengeItemAttempts { get; set; }
    public DbSet<StudentWordMastery> StudentWordMasteries { get; set; }
    public DbSet<StudentSkillProfile> StudentSkillProfiles { get; set; }
    public DbSet<ErrorPatternLog> ErrorPatternLogs { get; set; }

    // Streak
    public DbSet<DailyCheckIn> DailyCheckIns { get; set; }
    public DbSet<UserStreak> UserStreaks { get; set; }

    // Achievements
    public DbSet<Badge> Badges { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<UserBadgeProgress> UserBadgeProgresses { get; set; }
    public DbSet<UserAchievementEvent> UserAchievementEvents { get; set; }
    public DbSet<AchievementTrigger> AchievementTriggers { get; set; }

    // Shop
    public DbSet<ShopItem> ShopItems { get; set; }
    public DbSet<UserInventoryItem> UserInventoryItems { get; set; }
    public DbSet<UserEquippedItem> UserEquippedItems { get; set; }
    public DbSet<DailySpecial> DailySpecials { get; set; }
    public DbSet<DiamondTransaction> DiamondTransactions { get; set; }

    // Stickers
    public DbSet<StickerInventoryItem> StickerInventoryItems { get; set; }
    public DbSet<StickerGiftTransaction> StickerGiftTransactions { get; set; }

    // Classroom
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<ClassroomStudent> ClassroomStudents { get; set; }
    public DbSet<CustomModule> CustomModules { get; set; }
    public DbSet<StudentCredential> StudentCredentials { get; set; }

    // Progression
    public DbSet<Level> Levels { get; set; }
    public DbSet<UserProgress> UserProgresses { get; set; }

    // Missions
    public DbSet<Mission> Missions { get; set; }
    public DbSet<UserMission> UserMissions { get; set; }
    public DbSet<UserMissionProgress> UserMissionProgresses { get; set; }

    // Mascots
    // public DbSet<Mascot> Mascots { get; set; }
    // public DbSet<UserMascot> UserMascots { get; set; }

    // Activity
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // Social
    // public DbSet<Friendship> Friendships { get; set; }

    private void OnSavingChanges(object sender, SavingChangesEventArgs e)
    {
        _cleanString();
        ConfigureEntityDates();
    }

    private void _cleanString()
    {
        var changedEntities = ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified);
        foreach (var item in changedEntities)
        {
            if (item.Entity == null)
                continue;

            var properties = item.Entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && p.PropertyType == typeof(string));

            foreach (var property in properties)
            {
                var propName = property.Name;
                var val = (string)property.GetValue(item.Entity, null);

                if (val.HasValue())
                {
                    var newVal = val.Fa2En().FixPersianChars();
                    if (newVal == val)
                        continue;
                    property.SetValue(item.Entity, newVal, null);
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        base.OnModelCreating(modelBuilder);

        var entitiesAssembly = typeof(IEntity).Assembly;
        modelBuilder.RegisterAllEntities<IEntity>(entitiesAssembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        modelBuilder.AddRestrictDeleteBehaviorConvention();
        modelBuilder.AddPluralizingTableNameConvention();
        modelBuilder.Entity<StudentWordMastery>().ToTable("student_word_mastery");
    }

    private void ConfigureEntityDates()
    {
        var updatedEntities = ChangeTracker.Entries()
            .Where(x => x.Entity is ITimeModification && x.State == EntityState.Modified)
            .Select(x => x.Entity as ITimeModification);

        var addedEntities = ChangeTracker.Entries()
            .Where(x => x.Entity is ITimeModification && x.State == EntityState.Added)
            .Select(x => x.Entity as ITimeModification);

        foreach (var entity in updatedEntities)
        {
            if (entity != null)
            {
                entity.ModifiedDate = DateTime.UtcNow; // Ensure UTC
            }
        }

        foreach (var entity in addedEntities)
        {
            if (entity != null)
            {
                entity.CreatedTime = DateTime.UtcNow; // Ensure UTC
                entity.ModifiedDate = DateTime.UtcNow; // Ensure UTC
            }
        }
    }
}
