using CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;

namespace CleanArc.Application.Contracts.Infrastructure.Exports;

public interface IInstitutionUserCsvExportService
{
    Task<string> ExportAsync(IReadOnlyList<InstitutionUserSummaryDto> users, CancellationToken cancellationToken);
}
