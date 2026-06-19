using System.Collections.Generic;

namespace CleanArc.Application.Contracts.AdaptiveLearning;

public class HardcoreRewardConfig
{
    public int DefaultBonusXp { get; set; } = 100;
    public int DefaultBonusDiamonds { get; set; } = 50;
    public double MascotProbability { get; set; } = 0.5; // 50% chance
    public List<string> MascotNames { get; set; } = new()
    {
        "Pirate King",
        "Crown",
        "Giyu",
        "Rengoku",
        "Inosuke"
    };
    public string BadgeCode { get; set; } = "HARDCORE_HERO";
    public int ExpiryDurationHours { get; set; } = 48;
}
