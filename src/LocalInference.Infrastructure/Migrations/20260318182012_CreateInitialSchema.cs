using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalInference.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateInitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InferenceConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ModelIdentifier = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderType = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.69999999999999996),
                    TopP = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.90000000000000002),
                    TopK = table.Column<double>(type: "double precision", nullable: true),
                    RepeatPenalty = table.Column<double>(type: "double precision", nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: true),
                    ContextWindow = table.Column<int>(type: "integer", nullable: true),
                    StopSequences = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    SystemPrompt = table.Column<string>(type: "text", nullable: true),
                    Seed = table.Column<int>(type: "integer", nullable: true),
                    FrequencyPenalty = table.Column<double>(type: "double precision", nullable: true),
                    PresencePenalty = table.Column<double>(type: "double precision", nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InferenceConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnicalDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SourcePath = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Language = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Framework = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TokenCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsIndexed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastIndexedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicalDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    InferenceConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContextWindowTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 8192),
                    MaxOutputTokens = table.Column<int>(type: "integer", nullable: false, defaultValue: 2048),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastActivityAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_InferenceConfigs_InferenceConfigId",
                        column: x => x.InferenceConfigId,
                        principalTable: "InferenceConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicalDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    StartPosition = table.Column<int>(type: "integer", nullable: false),
                    EndPosition = table.Column<int>(type: "integer", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "jsonb", nullable: true),
                    Embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_TechnicalDocuments_TechnicalDocumentId",
                        column: x => x.TechnicalDocumentId,
                        principalTable: "TechnicalDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMessageIndex = table.Column<int>(type: "integer", nullable: false),
                    EndMessageIndex = table.Column<int>(type: "integer", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    OriginalTokenCount = table.Column<int>(type: "integer", nullable: false),
                    CompressedTokenCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ReplacedByCheckpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextCheckpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContextCheckpoints_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TokenCount = table.Column<int>(type: "integer", nullable: false),
                    SequenceNumber = table.Column<int>(type: "integer", nullable: false),
                    IsSummarized = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CheckpointId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContextMessages_ContextCheckpoints_CheckpointId",
                        column: x => x.CheckpointId,
                        principalTable: "ContextCheckpoints",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContextMessages_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContextCheckpoints_IsActive",
                table: "ContextCheckpoints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ContextCheckpoints_SessionId",
                table: "ContextCheckpoints",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMessages_CheckpointId",
                table: "ContextMessages",
                column: "CheckpointId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMessages_IsSummarized",
                table: "ContextMessages",
                column: "IsSummarized");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMessages_SessionId",
                table: "ContextMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextMessages_SessionId_SequenceNumber",
                table: "ContextMessages",
                columns: new[] { "SessionId", "SequenceNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_ChunkIndex",
                table: "DocumentChunks",
                column: "ChunkIndex");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_TechnicalDocumentId",
                table: "DocumentChunks",
                column: "TechnicalDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceConfigs_IsDefault",
                table: "InferenceConfigs",
                column: "IsDefault",
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_InferenceConfigs_ProviderType",
                table: "InferenceConfigs",
                column: "ProviderType");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedAt",
                table: "Sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_InferenceConfigId",
                table: "Sessions",
                column: "InferenceConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IsActive",
                table: "Sessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastActivityAt",
                table: "Sessions",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_DocumentType",
                table: "TechnicalDocuments",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_Framework",
                table: "TechnicalDocuments",
                column: "Framework");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_IsIndexed",
                table: "TechnicalDocuments",
                column: "IsIndexed");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDocuments_Language",
                table: "TechnicalDocuments",
                column: "Language");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContextMessages");

            migrationBuilder.DropTable(
                name: "DocumentChunks");

            migrationBuilder.DropTable(
                name: "ContextCheckpoints");

            migrationBuilder.DropTable(
                name: "TechnicalDocuments");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "InferenceConfigs");
        }
    }
}
