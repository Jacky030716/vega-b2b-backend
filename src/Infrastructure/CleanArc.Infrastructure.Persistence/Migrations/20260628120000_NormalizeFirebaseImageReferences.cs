using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class NormalizeFirebaseImageReferences : Migration
{
  /// <inheritdoc />
  protected override void Up(MigrationBuilder migrationBuilder)
  {
    migrationBuilder.Sql("""
      UPDATE "Badges"
      SET "ImageRef" = CASE
        WHEN "ImageRef" ILIKE 'gs://%/%' THEN regexp_replace("ImageRef", '^gs://[^/]+/', '')
        WHEN "ImageRef" ILIKE 'https://firebasestorage.googleapis.com/%/o/%'
          THEN replace(replace(split_part(split_part("ImageRef", '/o/', 2), '?', 1), '%2F', '/'), '%2f', '/')
        WHEN "ImageRef" ILIKE 'https://storage.googleapis.com/%/%'
          THEN regexp_replace("ImageRef", '^https://storage.googleapis.com/[^/]+/', '')
        ELSE regexp_replace("ImageRef", '^/+', '')
      END
      WHERE "ImageRef" IS NOT NULL
        AND "ImageRef" <> '';
      """);

    migrationBuilder.Sql("""
      UPDATE "StickerInventoryItems"
      SET "ImageUrl" = CASE
        WHEN "ImageUrl" ILIKE 'gs://%/%' THEN regexp_replace("ImageUrl", '^gs://[^/]+/', '')
        WHEN "ImageUrl" ILIKE 'https://firebasestorage.googleapis.com/%/o/%'
          THEN replace(replace(split_part(split_part("ImageUrl", '/o/', 2), '?', 1), '%2F', '/'), '%2f', '/')
        WHEN "ImageUrl" ILIKE 'https://storage.googleapis.com/%/%'
          THEN regexp_replace("ImageUrl", '^https://storage.googleapis.com/[^/]+/', '')
        ELSE regexp_replace("ImageUrl", '^/+', '')
      END
      WHERE "ImageUrl" IS NOT NULL
        AND "ImageUrl" <> '';
      """);
  }

  /// <inheritdoc />
  protected override void Down(MigrationBuilder migrationBuilder)
  {
  }
}
