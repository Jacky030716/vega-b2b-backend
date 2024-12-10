using CleanArc.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CleanArc.Domain.Entities.User;

public class UserLogin:IdentityUserLogin<int>,IEntity
{
    public UserLogin()
    {
        LoggedOn=DateTime.UtcNow;
    }

    public User User { get; set; }
    public DateTime LoggedOn { get; set; } = DateTime.UtcNow;
}