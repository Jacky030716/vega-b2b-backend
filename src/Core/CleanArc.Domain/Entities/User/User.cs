using CleanArc.Domain.Common;
using CleanArc.Domain.Entities.Word;
using Microsoft.AspNetCore.Identity;

namespace CleanArc.Domain.Entities.User;

public class User : IdentityUser<int>, IEntity
{
    public User()
    {
        this.GeneratedCode = Guid.NewGuid().ToString().Substring(0, 8);
        this.Level = 1;
        this.Experience = 0;
        this.Diamonds = 0;
        this.AvatarId = "0"; // default avatar sentinel (no equipped shop avatar)
    }

    public string Name { get; set; }
    public string FamilyName { get; set; }
    public string GeneratedCode { get; set; }

    // User Profile Data
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int Diamonds { get; set; } = 0;
    public string AvatarId { get; set; } = "0";

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

    public IList<WordList> WordLists { get; set; }

    #endregion

}