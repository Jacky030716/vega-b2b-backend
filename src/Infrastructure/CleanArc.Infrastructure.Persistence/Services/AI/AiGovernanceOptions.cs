namespace CleanArc.Infrastructure.Persistence.Services.AI;

public sealed class AiUsageLimitOptions
{
  public int StandardImageMonthlyLimit { get; set; } = 2;
  public int PlusImageMonthlyLimit { get; set; } = 10;
  public int PremiumImageMonthlyLimit { get; set; } = 30;
  public int AdminImageMonthlyLimit { get; set; } = 60;

  public int StandardTextMonthlyLimit { get; set; } = 20;
  public int PlusTextMonthlyLimit { get; set; } = 100;
  public int PremiumTextMonthlyLimit { get; set; } = 300;
  public int AdminTextMonthlyLimit { get; set; } = 600;

  public int DefaultImageMonthlyLimit { get; set; } = 2;
  public int DefaultTextMonthlyLimit { get; set; } = 20;
}

public sealed class AiRateLimitOptions
{
  // Global per-user guardrail across all AI endpoints/features.
  // This is intentionally strict to prevent rapid repeat calls from the client UI.
  public int GlobalWindowSeconds { get; set; } = 10;
  public int GlobalMaxRequests { get; set; } = 1;

  public int ImageWindowMinutes { get; set; } = 10;
  public int ImageMaxRequests { get; set; } = 3;

  public int TextWindowSeconds { get; set; } = 60;
  public int TextMaxRequests { get; set; } = 10;

  public int AuditorWindowSeconds { get; set; } = 60;
  public int AuditorMaxRequests { get; set; } = 5;
}
