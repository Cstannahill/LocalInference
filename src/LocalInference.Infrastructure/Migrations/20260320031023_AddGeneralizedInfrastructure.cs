using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalInference.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneralizedInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    MaxContextTokens = table.Column<int>(type: "integer", nullable: false),
                    DefaultModel = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SystemProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceData_SystemProfiles_SystemProfileId",
                        column: x => x.SystemProfileId,
                        principalTable: "SystemProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReferenceDataItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<string>(type: "text", nullable: false),
                    ReferenceDataId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceDataItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceDataItems_ReferenceData_ReferenceDataId",
                        column: x => x.ReferenceDataId,
                        principalTable: "ReferenceData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceData_SystemProfileId",
                table: "ReferenceData",
                column: "SystemProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceDataItems_ReferenceDataId",
                table: "ReferenceDataItems",
                column: "ReferenceDataId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceDataItems");

            migrationBuilder.DropTable(
                name: "ReferenceData");

            migrationBuilder.DropTable(
                name: "SystemProfiles");
        }
    }
}
