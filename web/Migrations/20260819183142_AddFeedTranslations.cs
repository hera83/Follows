using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace web.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "FeedPosts",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "da");

            migrationBuilder.AddColumn<string>(
                name: "OriginalLanguage",
                table: "FeedComments",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "da");

            migrationBuilder.CreateTable(
                name: "FeedCommentTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedCommentTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedCommentTranslations_FeedComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "FeedComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedPostTranslations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PostId = table.Column<int>(type: "INTEGER", nullable: false),
                    LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedPostTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedPostTranslations_FeedPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "FeedPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeedCommentTranslations_CommentId_LanguageCode",
                table: "FeedCommentTranslations",
                columns: new[] { "CommentId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeedPostTranslations_PostId_LanguageCode",
                table: "FeedPostTranslations",
                columns: new[] { "PostId", "LanguageCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeedCommentTranslations");

            migrationBuilder.DropTable(
                name: "FeedPostTranslations");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "FeedPosts");

            migrationBuilder.DropColumn(
                name: "OriginalLanguage",
                table: "FeedComments");
        }
    }
}
