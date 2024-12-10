using CleanArc.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace CleanArc.Domain.Entities.User;

public class Role:IdentityRole<int>,IEntity
{
    public Role()
    {
        CreatedDate=DateTime.UtcNow;
    }

    public string DisplayName { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public ICollection<RoleClaim> Claims { get; set; }
    public ICollection<UserRole> Users { get; set; }


}