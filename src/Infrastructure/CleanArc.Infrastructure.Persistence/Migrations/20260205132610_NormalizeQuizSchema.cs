using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CleanArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeQuizSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PayloadJson",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "AnswerJson",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "ConfigJson",
                table: "GameConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Quizzes",
                type: "text",
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Explanation",
                table: "QuizQuestions",
                type: "text",
                nullable: true,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Mode",
                table: "QuizAttempts",
                type: "text",
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClientVersion",
                table: "QuizAttempts",
                type: "text",
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionType",
                table: "QuizAttemptAnswers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultAgeGroup",
                table: "GameConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultDifficulty",
                table: "GameConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DefaultRounds",
                table: "GameConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DefaultStartingDifficulty",
                table: "GameConfigs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DefaultThemeId",
                table: "GameConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "GameDifficulties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameConfigId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SequenceLength = table.Column<int>(type: "integer", nullable: false),
                    Speed = table.Column<int>(type: "integer", nullable: false),
                    GhostMode = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameDifficulties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameDifficulties_GameConfigs_GameConfigId",
                        column: x => x.GameConfigId,
                        principalTable: "GameConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameConfigId = table.Column<int>(type: "integer", nullable: false),
                    ThemeId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameThemes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameThemes_GameConfigs_GameConfigId",
                        column: x => x.GameConfigId,
                        principalTable: "GameConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MagicBackpackAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizAttemptAnswerId = table.Column<int>(type: "integer", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicBackpackAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicBackpackAttemptAnswers_QuizAttemptAnswers_QuizAttemptA~",
                        column: x => x.QuizAttemptAnswerId,
                        principalTable: "QuizAttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MagicBackpackQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizQuestionId = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    AgeGroup = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicBackpackQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicBackpackQuestions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryRecallAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizAttemptAnswerId = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRecallAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryRecallAttemptAnswers_QuizAttemptAnswers_QuizAttemptAns~",
                        column: x => x.QuizAttemptAnswerId,
                        principalTable: "QuizAttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryRecallQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizQuestionId = table.Column<int>(type: "integer", nullable: false),
                    Theme = table.Column<string>(type: "text", nullable: false),
                    StoryAudioUrl = table.Column<string>(type: "text", nullable: false),
                    StoryText = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRecallQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryRecallQuestions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WordBridgeAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizAttemptAnswerId = table.Column<int>(type: "integer", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    TimeMs = table.Column<int>(type: "integer", nullable: false),
                    TargetWord = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordBridgeAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordBridgeAttemptAnswers_QuizAttemptAnswers_QuizAttemptAnsw~",
                        column: x => x.QuizAttemptAnswerId,
                        principalTable: "QuizAttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WordBridgeQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuizQuestionId = table.Column<int>(type: "integer", nullable: false),
                    TargetWord = table.Column<string>(type: "text", nullable: false),
                    Translation = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<string>(type: "text", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: true, defaultValue: ""),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordBridgeQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WordBridgeQuestions_QuizQuestions_QuizQuestionId",
                        column: x => x.QuizQuestionId,
                        principalTable: "QuizQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameThemeGradients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameThemeId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameThemeGradients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameThemeGradients_GameThemes_GameThemeId",
                        column: x => x.GameThemeId,
                        principalTable: "GameThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GameThemeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameThemeId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameThemeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameThemeItems_GameThemes_GameThemeId",
                        column: x => x.GameThemeId,
                        principalTable: "GameThemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MagicBackpackAttemptSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MagicBackpackAttemptAnswerId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicBackpackAttemptSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicBackpackAttemptSelections_MagicBackpackAttemptAnswers_~",
                        column: x => x.MagicBackpackAttemptAnswerId,
                        principalTable: "MagicBackpackAttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MagicBackpackItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MagicBackpackQuestionId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Emoji = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicBackpackItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicBackpackItems_MagicBackpackQuestions_MagicBackpackQues~",
                        column: x => x.MagicBackpackQuestionId,
                        principalTable: "MagicBackpackQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MagicBackpackSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MagicBackpackQuestionId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MagicBackpackSequences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MagicBackpackSequences_MagicBackpackQuestions_MagicBackpack~",
                        column: x => x.MagicBackpackQuestionId,
                        principalTable: "MagicBackpackQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryRecallAttemptSelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryRecallAttemptAnswerId = table.Column<int>(type: "integer", nullable: false),
                    RecallQuestionId = table.Column<string>(type: "text", nullable: false),
                    SelectedOption = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRecallAttemptSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryRecallAttemptSelections_StoryRecallAttemptAnswers_Stor~",
                        column: x => x.StoryRecallAttemptAnswerId,
                        principalTable: "StoryRecallAttemptAnswers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryRecallItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryRecallQuestionId = table.Column<int>(type: "integer", nullable: false),
                    RecallQuestionId = table.Column<string>(type: "text", nullable: false),
                    QuestionText = table.Column<string>(type: "text", nullable: false),
                    CorrectAnswer = table.Column<int>(type: "integer", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRecallItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryRecallItems_StoryRecallQuestions_StoryRecallQuestionId",
                        column: x => x.StoryRecallQuestionId,
                        principalTable: "StoryRecallQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryRecallOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StoryRecallItemId = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryRecallOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryRecallOptions_StoryRecallItems_StoryRecallItemId",
                        column: x => x.StoryRecallItemId,
                        principalTable: "StoryRecallItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameDifficulties_GameConfigId",
                table: "GameDifficulties",
                column: "GameConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_GameThemeGradients_GameThemeId",
                table: "GameThemeGradients",
                column: "GameThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameThemeItems_GameThemeId",
                table: "GameThemeItems",
                column: "GameThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_GameThemes_GameConfigId",
                table: "GameThemes",
                column: "GameConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicBackpackAttemptAnswers_QuizAttemptAnswerId",
                table: "MagicBackpackAttemptAnswers",
                column: "QuizAttemptAnswerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MagicBackpackAttemptSelections_MagicBackpackAttemptAnswerId",
                table: "MagicBackpackAttemptSelections",
                column: "MagicBackpackAttemptAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicBackpackItems_MagicBackpackQuestionId",
                table: "MagicBackpackItems",
                column: "MagicBackpackQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_MagicBackpackQuestions_QuizQuestionId",
                table: "MagicBackpackQuestions",
                column: "QuizQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MagicBackpackSequences_MagicBackpackQuestionId",
                table: "MagicBackpackSequences",
                column: "MagicBackpackQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryRecallAttemptAnswers_QuizAttemptAnswerId",
                table: "StoryRecallAttemptAnswers",
                column: "QuizAttemptAnswerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryRecallAttemptSelections_StoryRecallAttemptAnswerId",
                table: "StoryRecallAttemptSelections",
                column: "StoryRecallAttemptAnswerId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryRecallItems_StoryRecallQuestionId",
                table: "StoryRecallItems",
                column: "StoryRecallQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryRecallOptions_StoryRecallItemId",
                table: "StoryRecallOptions",
                column: "StoryRecallItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StoryRecallQuestions_QuizQuestionId",
                table: "StoryRecallQuestions",
                column: "QuizQuestionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordBridgeAttemptAnswers_QuizAttemptAnswerId",
                table: "WordBridgeAttemptAnswers",
                column: "QuizAttemptAnswerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WordBridgeQuestions_QuizQuestionId",
                table: "WordBridgeQuestions",
                column: "QuizQuestionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameDifficulties");

            migrationBuilder.DropTable(
                name: "GameThemeGradients");

            migrationBuilder.DropTable(
                name: "GameThemeItems");

            migrationBuilder.DropTable(
                name: "MagicBackpackAttemptSelections");

            migrationBuilder.DropTable(
                name: "MagicBackpackItems");

            migrationBuilder.DropTable(
                name: "MagicBackpackSequences");

            migrationBuilder.DropTable(
                name: "StoryRecallAttemptSelections");

            migrationBuilder.DropTable(
                name: "StoryRecallOptions");

            migrationBuilder.DropTable(
                name: "WordBridgeAttemptAnswers");

            migrationBuilder.DropTable(
                name: "WordBridgeQuestions");

            migrationBuilder.DropTable(
                name: "GameThemes");

            migrationBuilder.DropTable(
                name: "MagicBackpackAttemptAnswers");

            migrationBuilder.DropTable(
                name: "MagicBackpackQuestions");

            migrationBuilder.DropTable(
                name: "StoryRecallAttemptAnswers");

            migrationBuilder.DropTable(
                name: "StoryRecallItems");

            migrationBuilder.DropTable(
                name: "StoryRecallQuestions");

            migrationBuilder.DropColumn(
                name: "Explanation",
                table: "QuizQuestions");

            migrationBuilder.DropColumn(
                name: "QuestionType",
                table: "QuizAttemptAnswers");

            migrationBuilder.DropColumn(
                name: "DefaultAgeGroup",
                table: "GameConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultDifficulty",
                table: "GameConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultRounds",
                table: "GameConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultStartingDifficulty",
                table: "GameConfigs");

            migrationBuilder.DropColumn(
                name: "DefaultThemeId",
                table: "GameConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Quizzes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayloadJson",
                table: "QuizQuestions",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Mode",
                table: "QuizAttempts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ClientVersion",
                table: "QuizAttempts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldDefaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AnswerJson",
                table: "QuizAttemptAnswers",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConfigJson",
                table: "GameConfigs",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }
    }
}
