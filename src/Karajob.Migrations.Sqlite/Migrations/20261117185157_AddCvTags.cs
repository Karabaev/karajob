using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karajob.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddCvTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CvDocumentTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CvDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvDocumentTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CvDocumentTags_CvDocuments_CvDocumentId",
                        column: x => x.CvDocumentId,
                        principalTable: "CvDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvDocumentTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CvDocumentTags_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvDocumentTags_CvDocumentId_TagId",
                table: "CvDocumentTags",
                columns: new[] { "CvDocumentId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvDocumentTags_TagId",
                table: "CvDocumentTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_CvDocumentTags_UserId",
                table: "CvDocumentTags",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_UserId_Name",
                table: "Tags",
                columns: new[] { "UserId", "Name" },
                unique: true);

            // EF stores Guids in SQLite as upper-case text and compares them case-sensitively, so ids are generated upper-case.
            // SQLite lower() only folds ASCII, so the Cyrillic capitals are folded explicitly to match ToLowerInvariant in code.
            var lowered = "lower(trim(j.value))";
            foreach (var capital in Enumerable.Range(0x0410, 32).Append(0x0401).Select(code => (char)code))
            {
                lowered = $"replace({lowered}, '{capital}', '{char.ToLowerInvariant(capital)}')";
            }

            migrationBuilder.Sql(
                $$"""
                CREATE TEMP TABLE CvTagSource AS
                SELECT DISTINCT d.UserId AS UserId, d.Id AS CvDocumentId, substr({{lowered}}, 1, 50) AS Name
                FROM CvDocuments d, json_each(CASE WHEN json_valid(d.Tags) THEN d.Tags ELSE '[]' END) j
                WHERE trim(j.value) <> '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO Tags (Id, UserId, Name, Color)
                SELECT upper(substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12)),
                  UserId, Name, NULL
                FROM (SELECT DISTINCT UserId, Name, hex(randomblob(16)) AS h FROM (SELECT DISTINCT UserId, Name FROM CvTagSource));
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO CvDocumentTags (Id, UserId, CvDocumentId, TagId)
                SELECT upper(substr(h, 1, 8) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 4) || '-' || substr(h, 17, 4) || '-' || substr(h, 21, 12)),
                  s.UserId, s.CvDocumentId, t.Id
                FROM (SELECT UserId, CvDocumentId, Name, hex(randomblob(16)) AS h FROM CvTagSource) s
                JOIN Tags t ON t.UserId = s.UserId AND t.Name = s.Name;
                """);

            migrationBuilder.Sql("DROP TABLE CvTagSource;");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "CvDocuments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvDocumentTags");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "CvDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
