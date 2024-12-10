using CleanArc.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CleanArc.Domain.Entities.User;

public class RoleClaim:IdentityRoleClaim<int>,IEntity
{
    public RoleClaim()
    {
        CreatedClaim=DateTime.UtcNow;
    }

    public DateTime CreatedClaim { get; set; } = DateTime.UtcNow;
    public Role Role { get; set; }

}