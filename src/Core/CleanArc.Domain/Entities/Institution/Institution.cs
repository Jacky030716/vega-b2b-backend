using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.User;

namespace CleanArc.Domain.Entities.Institution;

public class Institution : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Seat limit for the institution
    public int MaxSeats { get; set; } = 50;
    public int SeatsUsed { get; set; } = 0;
    
    // Subscription info
    public DateTime RenewalDate { get; set; } = DateTime.UtcNow.AddYears(1);
    public string SubscriptionTier { get; set; } = "Standard";
    public string StripeCustomerId { get; set; }

    // Navigation property to Users in this institution
    public ICollection<User.User> Users { get; set; } = new List<User.User>();
}
