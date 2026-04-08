using CleanArc.Application.Contracts.Persistence;
using CleanArc.Domain.Entities.User;
using CleanArc.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace CleanArc.Infrastructure.Persistence.Repositories;

internal class StudentCredentialRepository(ApplicationDbContext dbContext)
    : BaseAsyncRepository<StudentCredential>(dbContext), IStudentCredentialRepository
{
  public async Task<StudentCredential?> GetByLoginCodeAsync(string loginCode)
  {
    var normalizedCode = loginCode?.Trim().ToUpperInvariant() ?? string.Empty;

    return await Table
        .Include(sc => sc.User)
        .Include(sc => sc.Classroom)
        .FirstOrDefaultAsync(sc => sc.StudentLoginCode == normalizedCode);
  }

  public async Task<List<StudentCredential>> GetByClassroomIdAsync(int classroomId)
  {
    return await TableNoTracking
        .Include(sc => sc.User)
        .Where(sc => sc.ClassroomId == classroomId && sc.IsActive)
        .ToListAsync();
  }

  public async Task<StudentCredential> CreateAsync(StudentCredential credential)
  {
    await AddAsync(credential);
    return credential;
  }

  public async Task UpdateAsync(StudentCredential credential)
  {
    DbContext.StudentCredentials.Update(credential);
    await DbContext.SaveChangesAsync();
  }
}
