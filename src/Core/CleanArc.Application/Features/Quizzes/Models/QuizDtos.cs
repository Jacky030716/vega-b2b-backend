namespace CleanArc.Application.Features.Quizzes.Models;

public class QuizSummaryDto
{
  public string Id { get; set; }
  public string Title { get; set; }
  public string Description { get; set; }
  public string Theme { get; set; }
  public string Type { get; set; }
  public string Difficulty { get; set; }
  public int EstimatedTime { get; set; }
  public int TotalPoints { get; set; }
  public string CreatedAt { get; set; }
  public string ImageUrl { get; set; }
  public string Category { get; set; }
  public int QuestionCount { get; set; }
}

public class QuizDetailDto : QuizSummaryDto
{
  public IReadOnlyList<object> Questions { get; set; } = Array.Empty<object>();
}

public class CreateQuizAttemptRequest
{
  public string Mode { get; set; }
  public string ClientVersion { get; set; }
}

public class CreateQuizAttemptResponse
{
  public string AttemptId { get; set; }
  public string QuizId { get; set; }
  public DateTime StartedAt { get; set; }
  public string Seed { get; set; }
}

public class UpdateQuizAttemptRequest
{
  public string QuestionId { get; set; }
  public object Answer { get; set; }
  public bool? IsCorrect { get; set; }
  public int? ElapsedSec { get; set; }
}

public class UpdateQuizAttemptResponse
{
  public string AttemptId { get; set; }
  public string QuizId { get; set; }
  public int AnswersCount { get; set; }
}

public class SubmitQuizAttemptRequest
{
  public List<AttemptAnswerDto> Answers { get; set; } = new();
  public DateTime? CompletedAt { get; set; }
  public int? TotalTimeSec { get; set; }
}

public class SubmitQuizAttemptResponse
{
  public string AttemptId { get; set; }
  public int Score { get; set; }
  public int TotalPoints { get; set; }
  public int Percentage { get; set; }
  public int XpAwarded { get; set; }
  public int DiamondsAwarded { get; set; }
  public List<string> BadgesUnlocked { get; set; } = new();
}

public class AttemptAnswerDto
{
  public string QuestionId { get; set; }
  public object Answer { get; set; }
  public bool? IsCorrect { get; set; }
  public int? TimeSpentSec { get; set; }
}

public class QuizAttemptDto
{
  public string AttemptId { get; set; }
  public string QuizId { get; set; }
  public int UserId { get; set; }
  public DateTime StartedAt { get; set; }
  public DateTime? CompletedAt { get; set; }
  public int? TotalTimeSec { get; set; }
  public List<AttemptAnswerDto> Answers { get; set; } = new();
}

public class QuizAttemptRecord : QuizAttemptDto
{
  public string Mode { get; set; }
  public string ClientVersion { get; set; }
}
