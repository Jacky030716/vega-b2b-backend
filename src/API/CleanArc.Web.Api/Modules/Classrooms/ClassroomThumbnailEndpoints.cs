using System.Security.Claims;
using Carter;
using CleanArc.Application.Contracts.Infrastructure.AI;
using CleanArc.Application.Contracts.Infrastructure.ClassroomThumbnails;
using CleanArc.Application.Contracts.Persistence;
using CleanArc.Application.Models.Common;
using CleanArc.SharedKernel.Extensions;
using CleanArc.WebFramework.WebExtensions;
using Microsoft.AspNetCore.Mvc;

#nullable enable

namespace CleanArc.Web.Api.Endpoints;

public sealed class ClassroomThumbnailEndpoints : ICarterModule
{
  private const string RoutePrefix = "/api/v{version:apiVersion}/Classrooms/";
  private const double Version = 1.1;
  private const string Tag = "Classroom Thumbnails";

  public void AddRoutes(IEndpointRouteBuilder app)
  {
    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}thumbnail/upload",
      async (
        [FromForm] IFormFile file,
        [FromForm] int? classroomId,
        ClaimsPrincipal user,
        IClassroomThumbnailImageStorageService storageService,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken) =>
      {
        var userId = int.Parse(user.Identity!.GetUserId());

        if (classroomId is int requestedClassroomId)
        {
          var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(requestedClassroomId, tracking: true);
          if (classroom is null)
            return Results.NotFound(new { message = "Classroom not found." });

          if (classroom.TeacherId != userId && !user.IsInRole("admin"))
            return Results.Forbid();
        }

        var result = await UploadAsync(file, storageService, cancellationToken);
        if (!result.IsSuccess)
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { result.ErrorMessage ?? "Thumbnail upload failed." } } });

        if (classroomId is int id)
        {
          var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(id, tracking: true);
          if (classroom is not null)
          {
            classroom.ThumbnailType = "UPLOADED";
            classroom.ThumbnailAssetId = result.Result.AssetId;
            classroom.ThumbnailUrl = result.Result.Url;
            classroom.ThumbnailPrompt = null;
            classroom.ThumbnailGeneratedAt = null;
            classroom.Thumbnail = result.Result.Url;
            await unitOfWork.ClassroomRepository.UpdateClassroomAsync(classroom);
          }
        }

        return Results.Ok(new
        {
          assetId = result.Result.AssetId,
          url = result.Result.Url,
          thumbnailType = "UPLOADED"
        });
      }), Version, "UploadClassroomThumbnail", Tag)
      .DisableAntiforgery()
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));

    app.MapEndpoint(builder => builder.MapPost(
      $"{RoutePrefix}{{classroomId:int}}/thumbnail/upload",
      async (
        int classroomId,
        [FromForm] IFormFile file,
        ClaimsPrincipal user,
        IClassroomThumbnailImageStorageService storageService,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken) =>
      {
        var userId = int.Parse(user.Identity!.GetUserId());

        var classroom = await unitOfWork.ClassroomRepository.GetClassroomByIdAsync(classroomId, tracking: true);
        if (classroom is null)
          return Results.NotFound(new { message = "Classroom not found." });

        if (classroom.TeacherId != userId && !user.IsInRole("admin"))
          return Results.Forbid();

        var result = await UploadAsync(file, storageService, cancellationToken);
        if (!result.IsSuccess)
          return Results.BadRequest(new Dictionary<string, List<string>> { { "GeneralError", new() { result.ErrorMessage ?? "Thumbnail upload failed." } } });

        classroom.ThumbnailType = "UPLOADED";
        classroom.ThumbnailAssetId = result.Result.AssetId;
        classroom.ThumbnailUrl = result.Result.Url;
        classroom.ThumbnailPrompt = null;
        classroom.ThumbnailGeneratedAt = null;
        classroom.Thumbnail = result.Result.Url;
        await unitOfWork.ClassroomRepository.UpdateClassroomAsync(classroom);

        return Results.Ok(new
        {
          assetId = result.Result.AssetId,
          url = result.Result.Url,
          thumbnailType = "UPLOADED"
        });
      }), Version, "UploadClassroomThumbnailForClassroom", Tag)
      .DisableAntiforgery()
      .RequireAuthorization(builder => builder.RequireRole("teacher", "admin"));
  }

  private static async Task<OperationResult<ClassroomThumbnailUploadResult>> UploadAsync(
    IFormFile file,
    IClassroomThumbnailImageStorageService storageService,
    CancellationToken cancellationToken)
  {
    if (file is null || file.Length == 0)
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Please choose an image file.");

    var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "image/jpeg",
      "image/jpg",
      "image/png",
      "image/webp"
    };

    var contentType = (file.ContentType ?? string.Empty).Trim().ToLowerInvariant();
    if (!allowedContentTypes.Contains(contentType))
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Only JPG, PNG, and WebP images are supported.");

    const long maxBytes = 5 * 1024 * 1024;
    if (file.Length > maxBytes)
      return OperationResult<ClassroomThumbnailUploadResult>.FailureResult("Thumbnail images must be 5 MB or smaller.");

    await using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream, cancellationToken);

    return await storageService.UploadAsync(
      memoryStream.ToArray(),
      Path.GetFileNameWithoutExtension(file.FileName),
      contentType,
      cancellationToken);
  }
}
