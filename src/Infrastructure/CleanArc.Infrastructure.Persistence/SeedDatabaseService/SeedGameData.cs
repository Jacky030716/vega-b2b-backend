using CleanArc.Domain.Entities.Achievement;
using CleanArc.Domain.Entities.Mascot;
using CleanArc.Domain.Entities.Progression;
using CleanArc.Domain.Entities.Shop;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.SeedDatabaseService;

public interface ISeedGameData
{
  Task Seed();
}

public class SeedGameData(ApplicationDbContext dbContext) : ISeedGameData
{
  public async Task Seed()
  {
    await SeedLevels();
    await SeedMascots();
    await SeedBadges();
    await SeedShopItems();
  }

  private async Task SeedLevels()
  {
    if (await dbContext.Levels.AnyAsync()) return;

    var levels = new List<Level>
        {
            new() { LevelNumber = 1, Name = "Beginner", RequiredXP = 0, UnlocksGameType = "magic_backpack" },
            new() { LevelNumber = 2, Name = "Explorer", RequiredXP = 100, UnlocksGameType = "word_bridge" },
            new() { LevelNumber = 3, Name = "Adventurer", RequiredXP = 300, UnlocksGameType = "story_recall" },
            new() { LevelNumber = 4, Name = "Scholar", RequiredXP = 600 },
            new() { LevelNumber = 5, Name = "Champion", RequiredXP = 1000 },
            new() { LevelNumber = 6, Name = "Master", RequiredXP = 1500 },
            new() { LevelNumber = 7, Name = "Legend", RequiredXP = 2200 },
            new() { LevelNumber = 8, Name = "Mythic", RequiredXP = 3000 },
            new() { LevelNumber = 9, Name = "Celestial", RequiredXP = 4000 },
            new() { LevelNumber = 10, Name = "Transcendent", RequiredXP = 5500 }
        };

    await dbContext.Levels.AddRangeAsync(levels);
    await dbContext.SaveChangesAsync();
  }

  private async Task SeedMascots()
  {
    if (await dbContext.Mascots.AnyAsync()) return;

    var mascots = new List<Mascot>
        {
            new()
            {
                Name = "Bear",
                ImageUrl = "bear",
                Description = "A friendly bear companion who loves learning!",
                IsDefault = true,
                UnlockCondition = null
            },
            new()
            {
                Name = "Fox",
                ImageUrl = "fox",
                Description = "A clever fox who helps you solve puzzles.",
                IsDefault = false,
                UnlockCondition = "{\"type\":\"level\",\"value\":3}"
            },
            new()
            {
                Name = "Owl",
                ImageUrl = "owl",
                Description = "A wise owl that guides you through stories.",
                IsDefault = false,
                UnlockCondition = "{\"type\":\"level\",\"value\":5}"
            },
            new()
            {
                Name = "Rabbit",
                ImageUrl = "rabbit",
                Description = "A speedy rabbit that races through word challenges.",
                IsDefault = false,
                UnlockCondition = "{\"type\":\"streak\",\"value\":7}"
            },
            new()
            {
                Name = "Dragon",
                ImageUrl = "dragon",
                Description = "A mighty dragon unlocked by true dedication.",
                IsDefault = false,
                UnlockCondition = "{\"type\":\"level\",\"value\":10}"
            }
        };

    await dbContext.Mascots.AddRangeAsync(mascots);
    await dbContext.SaveChangesAsync();
  }

  private async Task SeedBadges()
  {
    if (await dbContext.Badges.AnyAsync()) return;

    var badges = new List<Badge>
        {
            // Streak badges
            new()
            {
                Name = "First Flame",
                Description = "Complete your first daily check-in.",
                BadgeImageUrl = "badge_first_flame",
                Rarity = "common",
                Category = "streak",
                MaxProgress = 1,
                UnlockCriteria = "{\"type\":\"streak\",\"count\":1}",
                IsActive = true
            },
            new()
            {
                Name = "Week Warrior",
                Description = "Maintain a 7-day streak.",
                BadgeImageUrl = "badge_week_warrior",
                Rarity = "rare",
                Category = "streak",
                MaxProgress = 7,
                UnlockCriteria = "{\"type\":\"streak\",\"count\":7}",
                IsActive = true
            },
            new()
            {
                Name = "Month Master",
                Description = "Maintain a 30-day streak.",
                BadgeImageUrl = "badge_month_master",
                Rarity = "legendary",
                Category = "streak",
                MaxProgress = 30,
                UnlockCriteria = "{\"type\":\"streak\",\"count\":30}",
                IsActive = true
            },

            // Quiz badges
            new()
            {
                Name = "Quiz Starter",
                Description = "Complete your first quiz.",
                BadgeImageUrl = "badge_quiz_starter",
                Rarity = "common",
                Category = "quiz",
                MaxProgress = 1,
                UnlockCriteria = "{\"type\":\"quizzes_completed\",\"count\":1}",
                IsActive = true
            },
            new()
            {
                Name = "Quiz Enthusiast",
                Description = "Complete 10 quizzes.",
                BadgeImageUrl = "badge_quiz_enthusiast",
                Rarity = "rare",
                Category = "quiz",
                MaxProgress = 10,
                UnlockCriteria = "{\"type\":\"quizzes_completed\",\"count\":10}",
                IsActive = true
            },
            new()
            {
                Name = "Quiz Master",
                Description = "Complete 50 quizzes.",
                BadgeImageUrl = "badge_quiz_master",
                Rarity = "epic",
                Category = "quiz",
                MaxProgress = 50,
                UnlockCriteria = "{\"type\":\"quizzes_completed\",\"count\":50}",
                IsActive = true
            },

            // Level badges
            new()
            {
                Name = "Level Up!",
                Description = "Reach level 2.",
                BadgeImageUrl = "badge_level_up",
                Rarity = "common",
                Category = "level",
                MaxProgress = 2,
                UnlockCriteria = "{\"type\":\"level\",\"value\":2}",
                IsActive = true
            },
            new()
            {
                Name = "Rising Star",
                Description = "Reach level 5.",
                BadgeImageUrl = "badge_rising_star",
                Rarity = "rare",
                Category = "level",
                MaxProgress = 5,
                UnlockCriteria = "{\"type\":\"level\",\"value\":5}",
                IsActive = true
            },
            new()
            {
                Name = "Legendary Learner",
                Description = "Reach level 10.",
                BadgeImageUrl = "badge_legendary_learner",
                Rarity = "legendary",
                Category = "level",
                MaxProgress = 10,
                UnlockCriteria = "{\"type\":\"level\",\"value\":10}",
                IsActive = true
            },

            // Social badges
            new()
            {
                Name = "First Friend",
                Description = "Add your first friend.",
                BadgeImageUrl = "badge_first_friend",
                Rarity = "common",
                Category = "social",
                MaxProgress = 1,
                UnlockCriteria = "{\"type\":\"friends\",\"count\":1}",
                IsActive = true
            },
            new()
            {
                Name = "Social Butterfly",
                Description = "Have 10 friends.",
                BadgeImageUrl = "badge_social_butterfly",
                Rarity = "rare",
                Category = "social",
                MaxProgress = 10,
                UnlockCriteria = "{\"type\":\"friends\",\"count\":10}",
                IsActive = true
            },

            // Special badges
            new()
            {
                Name = "Classroom Champion",
                Description = "Get the highest score in a classroom quiz.",
                BadgeImageUrl = "badge_classroom_champion",
                Rarity = "epic",
                Category = "classroom",
                MaxProgress = 1,
                UnlockCriteria = "{\"type\":\"classroom_top_score\",\"count\":1}",
                IsActive = true
            },
            new()
            {
                Name = "Mission Accomplished",
                Description = "Complete your first special mission.",
                BadgeImageUrl = "badge_mission_accomplished",
                Rarity = "rare",
                Category = "mission",
                MaxProgress = 1,
                UnlockCriteria = "{\"type\":\"missions_completed\",\"count\":1}",
                IsActive = true
            }
        };

    await dbContext.Badges.AddRangeAsync(badges);
    await dbContext.SaveChangesAsync();
  }

  private async Task SeedShopItems()
  {
    if (await dbContext.ShopItems.AnyAsync()) return;

    var shopItems = new List<ShopItem>
        {
            // Avatars
            new()
            {
                Name = "Pirate Hat",
                Description = "Arr! A cool pirate hat for your avatar.",
                Category = "avatar",
                Price = 50,
                Currency = "diamonds",
                ImageUrl = "shop_pirate_hat",
                Rarity = "common",
                IsAvailable = true,
                IsLimitedEdition = false
            },
            new()
            {
                Name = "Crown",
                Description = "A golden crown fit for a quiz champion.",
                Category = "avatar",
                Price = 150,
                Currency = "diamonds",
                ImageUrl = "shop_crown",
                Rarity = "rare",
                IsAvailable = true,
                IsLimitedEdition = false
            },
            new()
            {
                Name = "Wizard Hat",
                Description = "A mysterious wizard hat with magical powers.",
                Category = "avatar",
                Price = 300,
                Currency = "diamonds",
                ImageUrl = "shop_wizard_hat",
                Rarity = "epic",
                RequiredLevel = 5,
                IsAvailable = true,
                IsLimitedEdition = false
            },

            // Themes
            new()
            {
                Name = "Ocean Theme",
                Description = "Dive into the deep blue ocean theme.",
                Category = "theme",
                Price = 100,
                Currency = "diamonds",
                ImageUrl = "shop_ocean_theme",
                Rarity = "common",
                IsAvailable = true,
                IsLimitedEdition = false
            },
            new()
            {
                Name = "Space Theme",
                Description = "Explore the cosmos with a space-themed background.",
                Category = "theme",
                Price = 200,
                Currency = "diamonds",
                ImageUrl = "shop_space_theme",
                Rarity = "rare",
                IsAvailable = true,
                IsLimitedEdition = false
            },
            new()
            {
                Name = "Forest Theme",
                Description = "A peaceful forest theme for calm studying.",
                Category = "theme",
                Price = 100,
                Currency = "diamonds",
                ImageUrl = "shop_forest_theme",
                Rarity = "common",
                IsAvailable = true,
                IsLimitedEdition = false
            },

            // Power-ups
            new()
            {
                Name = "Double XP Boost",
                Description = "Earn double XP for your next quiz!",
                Category = "powerup",
                Price = 75,
                Currency = "diamonds",
                ImageUrl = "shop_double_xp",
                Rarity = "common",
                IsAvailable = true,
                IsLimitedEdition = false
            },
            new()
            {
                Name = "Hint Token",
                Description = "Get a free hint during any quiz question.",
                Category = "powerup",
                Price = 50,
                Currency = "diamonds",
                ImageUrl = "shop_hint_token",
                Rarity = "common",
                IsAvailable = true,
                IsLimitedEdition = false
            }
        };

    await dbContext.ShopItems.AddRangeAsync(shopItems);
    await dbContext.SaveChangesAsync();
  }
}
