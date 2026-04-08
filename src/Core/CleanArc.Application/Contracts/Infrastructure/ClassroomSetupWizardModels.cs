namespace CleanArc.Application.Contracts.Infrastructure;

public record ParsedStudentCredential(string StudentName, string StudentLoginCode, string VisualPassword);

public record StudentCredentialPreview(string StudentName, string StudentLoginCode, string VisualPassword);

public record SetupClassroomWizardResult(
    int ClassroomId,
    string ClassroomName,
    string JoinCode,
    string AssignedQuizId,
    string RosterFileName,
    string RosterPdfBase64,
    IReadOnlyList<StudentCredentialPreview> StudentCredentials
);