using System.Globalization;
using CleanArc.Application.Contracts.Infrastructure.Exports;
using CleanArc.Application.Features.Admin.Queries.GetInstitutionUsers;
using CsvHelper;

namespace CleanArc.Infrastructure.Persistence.Services;

public sealed class CsvExportService : IInstitutionUserCsvExportService
{
    public Task<string> ExportAsync(IReadOnlyList<InstitutionUserSummaryDto> users, CancellationToken cancellationToken)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField("UserName");
        csv.WriteField("Email");
        csv.WriteField("Role");
        csv.WriteField("IsActive");
        csv.WriteField("LastLoginAt");
        csv.WriteField("ClassName");
        csv.NextRecord();

        foreach (var user in users)
        {
            csv.WriteField(user.UserName);
            csv.WriteField(user.Email);
            csv.WriteField(user.Role);
            csv.WriteField(user.IsActive);
            csv.WriteField(user.LastLoginAt?.ToString("O"));
            csv.WriteField(user.ClassName ?? string.Empty);
            csv.NextRecord();
        }

        csv.Flush();
        return Task.FromResult(writer.ToString());
    }
}
