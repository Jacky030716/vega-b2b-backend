namespace CleanArc.Application.Contracts.DTOs.User;

public class GetUserProfileResponse
{
  public int UserId { get; set; }
  public string UserName { get; set; }
  public string Email { get; set; }
  public string Name { get; set; }
  public string FamilyName { get; set; }
  public string PhoneNumber { get; set; }
  public int Level { get; set; }
  public int Experience { get; set; }
  public int Diamonds { get; set; }
  public string AvatarId { get; set; }
}
