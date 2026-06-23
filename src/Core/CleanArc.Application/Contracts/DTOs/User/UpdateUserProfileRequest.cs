namespace CleanArc.Application.Contracts.DTOs.User;

public class UpdateUserProfileRequest
{
  public string Name { get; set; }
  public string FamilyName { get; set; }
  public string UserName { get; set; }
  public string Email { get; set; }
  public string PhoneNumber { get; set; }
  public string AvatarId { get; set; }
}
