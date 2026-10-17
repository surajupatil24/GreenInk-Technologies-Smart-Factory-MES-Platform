using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class ManufacturingExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinishedPack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputPalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackSize = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PackCount = table.Column<int>(type: "integer", nullable: false),
                    LooseQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PackagingReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinishedPack", x => x.Id);
                    table.UniqueConstraint("AK_FinishedPack_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_FinishedPack_Quantity", "\"PackSize\" > 0 AND \"PackCount\" >= 0 AND \"LooseQuantity\" >= 0 AND \"LooseQuantity\" < \"PackSize\" AND \"Quantity\" > 0 AND \"Quantity\" = \"PackSize\" * \"PackCount\" + \"LooseQuantity\"");
                    table.ForeignKey(
                        name: "FK_FinishedPack_Pallet_TenantId_PlantId_OutputPalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.OutputPalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinishedPack_Pallet_TenantId_PlantId_SourcePalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.SourcePalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinishedPack_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManufacturingCommand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Signature = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManufacturingCommand", x => x.Id);
                    table.UniqueConstraint("AK_ManufacturingCommand_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ManufacturingCommand_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecipe",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Precautions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PhasesJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRecipe", x => x.Id);
                    table.UniqueConstraint("AK_ProductionRecipe_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductionRecipe_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRecipe_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionResource",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Maintenance = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionResource", x => x.Id);
                    table.UniqueConstraint("AK_ProductionResource_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductionResource_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualitySpecification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TestsJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualitySpecification", x => x.Id);
                    table.UniqueConstraint("AK_QualitySpecification_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_QualitySpecification_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySpecification_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionResourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualitySpecificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SafetyConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PausedMinutes = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionRun", x => x.Id);
                    table.UniqueConstraint("AK_ProductionRun_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Run_State", "\"Status\" IN ('Scheduled','Running','Paused','Completed') AND \"PausedMinutes\" >= 0");
                    table.ForeignKey(
                        name: "FK_ProductionRun_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRun_ProductionOrder_TenantId_PlantId_ProductionOr~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRun_ProductionRecipe_TenantId_PlantId_ProductionR~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionRecipeId },
                        principalTable: "ProductionRecipe",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRun_ProductionResource_TenantId_PlantId_Productio~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionResourceId },
                        principalTable: "ProductionResource",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionRun_QualitySpecification_TenantId_PlantId_Quality~",
                        columns: x => new { x.TenantId, x.PlantId, x.QualitySpecificationId },
                        principalTable: "QualitySpecification",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(16000)", maxLength: 16000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEvent", x => x.Id);
                    table.UniqueConstraint("AK_ProductionEvent_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ProductionEvent_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionEvent_ProductionRun_TenantId_PlantId_ProductionRu~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionRunId },
                        principalTable: "ProductionRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionPhase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RequiresSample = table.Column<bool>(type: "boolean", nullable: false),
                    ChecksJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    ParametersJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    EvidenceJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionPhase", x => x.Id);
                    table.UniqueConstraint("AK_ProductionPhase_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Phase_State", "\"Sequence\" > 0 AND \"Status\" IN ('Pending','Running','Completed')");
                    table.ForeignKey(
                        name: "FK_ProductionPhase_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionPhase_ProductionRun_TenantId_PlantId_ProductionRu~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionRunId },
                        principalTable: "ProductionRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualitySample",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    QualitySpecificationId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionPhaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RetestOfId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TestsJson = table.Column<string>(type: "jsonb", maxLength: 65536, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    HandedOverAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TestingAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualitySample", x => x.Id);
                    table.UniqueConstraint("AK_QualitySample_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Sample_Target", "(\"BatchId\" IS NOT NULL) <> (\"ProductionPhaseId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QualitySample_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySample_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySample_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySample_ProductionPhase_TenantId_PlantId_ProductionPh~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionPhaseId },
                        principalTable: "ProductionPhase",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySample_QualitySample_TenantId_PlantId_RetestOfId",
                        columns: x => new { x.TenantId, x.PlantId, x.RetestOfId },
                        principalTable: "QualitySample",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualitySample_QualitySpecification_TenantId_PlantId_Quality~",
                        columns: x => new { x.TenantId, x.PlantId, x.QualitySpecificationId },
                        principalTable: "QualitySpecification",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityCase",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    QualitySampleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Severity = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    RootCause = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityCase", x => x.Id);
                    table.UniqueConstraint("AK_QualityCase_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Case_Target", "\"BatchId\" IS NOT NULL OR \"ProductionRunId\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_QualityCase_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityCase_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityCase_ProductionRun_TenantId_PlantId_ProductionRunId",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionRunId },
                        principalTable: "ProductionRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityCase_QualitySample_TenantId_PlantId_QualitySampleId",
                        columns: x => new { x.TenantId, x.PlantId, x.QualitySampleId },
                        principalTable: "QualitySample",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityTestReading",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QualitySampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NumericValue = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    PassedValue = table.Column<bool>(type: "boolean", nullable: true),
                    InSpec = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityTestReading", x => x.Id);
                    table.UniqueConstraint("AK_QualityTestReading_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Reading_Value", "(\"NumericValue\" IS NOT NULL) <> (\"PassedValue\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QualityTestReading_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityTestReading_QualitySample_TenantId_PlantId_QualitySa~",
                        columns: x => new { x.TenantId, x.PlantId, x.QualitySampleId },
                        principalTable: "QualitySample",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPack_TenantId_PlantId_CreatedAt",
                table: "FinishedPack",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPack_TenantId_PlantId_OutputPalletId",
                table: "FinishedPack",
                columns: new[] { "TenantId", "PlantId", "OutputPalletId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinishedPack_TenantId_PlantId_SourcePalletId",
                table: "FinishedPack",
                columns: new[] { "TenantId", "PlantId", "SourcePalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturingCommand_TenantId_PlantId_CommandId",
                table: "ManufacturingCommand",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ManufacturingCommand_TenantId_PlantId_CreatedAt",
                table: "ManufacturingCommand",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvent_TenantId_PlantId_CreatedAt",
                table: "ProductionEvent",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEvent_TenantId_PlantId_ProductionRunId",
                table: "ProductionEvent",
                columns: new[] { "TenantId", "PlantId", "ProductionRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPhase_TenantId_PlantId_CreatedAt",
                table: "ProductionPhase",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionPhase_TenantId_PlantId_ProductionRunId_Sequence",
                table: "ProductionPhase",
                columns: new[] { "TenantId", "PlantId", "ProductionRunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipe_TenantId_PlantId_Code_Revision",
                table: "ProductionRecipe",
                columns: new[] { "TenantId", "PlantId", "Code", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipe_TenantId_PlantId_CreatedAt",
                table: "ProductionRecipe",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipe_TenantId_PlantId_MaterialId",
                table: "ProductionRecipe",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionResource_TenantId_PlantId_Code",
                table: "ProductionResource",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionResource_TenantId_PlantId_CreatedAt",
                table: "ProductionResource",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRun_TenantId_PlantId_CreatedAt",
                table: "ProductionRun",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRun_TenantId_PlantId_ProductionOrderId",
                table: "ProductionRun",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRun_TenantId_PlantId_ProductionRecipeId",
                table: "ProductionRun",
                columns: new[] { "TenantId", "PlantId", "ProductionRecipeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRun_TenantId_PlantId_ProductionResourceId",
                table: "ProductionRun",
                columns: new[] { "TenantId", "PlantId", "ProductionResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRun_TenantId_PlantId_QualitySpecificationId",
                table: "ProductionRun",
                columns: new[] { "TenantId", "PlantId", "QualitySpecificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityCase_TenantId_PlantId_BatchId",
                table: "QualityCase",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityCase_TenantId_PlantId_CreatedAt",
                table: "QualityCase",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityCase_TenantId_PlantId_ProductionRunId",
                table: "QualityCase",
                columns: new[] { "TenantId", "PlantId", "ProductionRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityCase_TenantId_PlantId_QualitySampleId",
                table: "QualityCase",
                columns: new[] { "TenantId", "PlantId", "QualitySampleId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_BatchId",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_CreatedAt",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_MaterialId",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_Number",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_ProductionPhaseId",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "ProductionPhaseId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_QualitySpecificationId",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "QualitySpecificationId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySample_TenantId_PlantId_RetestOfId",
                table: "QualitySample",
                columns: new[] { "TenantId", "PlantId", "RetestOfId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySpecification_TenantId_PlantId_CreatedAt",
                table: "QualitySpecification",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QualitySpecification_TenantId_PlantId_MaterialId_Revision",
                table: "QualitySpecification",
                columns: new[] { "TenantId", "PlantId", "MaterialId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityTestReading_TenantId_PlantId_CreatedAt",
                table: "QualityTestReading",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityTestReading_TenantId_PlantId_QualitySampleId",
                table: "QualityTestReading",
                columns: new[] { "TenantId", "PlantId", "QualitySampleId" });
            migrationBuilder.Sql(@"CREATE FUNCTION greenink_manufacturing_history_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Manufacturing evidence is append-only'; END $$;
CREATE TRIGGER productionevent_immutable BEFORE UPDATE OR DELETE ON ""ProductionEvent"" FOR EACH ROW EXECUTE FUNCTION greenink_manufacturing_history_immutable();
CREATE TRIGGER qualitytestreading_immutable BEFORE UPDATE OR DELETE ON ""QualityTestReading"" FOR EACH ROW EXECUTE FUNCTION greenink_manufacturing_history_immutable();
CREATE TRIGGER finishedpack_immutable BEFORE UPDATE OR DELETE ON ""FinishedPack"" FOR EACH ROW EXECUTE FUNCTION greenink_manufacturing_history_immutable();
CREATE TRIGGER manufacturingcommand_immutable BEFORE UPDATE OR DELETE ON ""ManufacturingCommand"" FOR EACH ROW EXECUTE FUNCTION greenink_manufacturing_history_immutable();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinishedPack");

            migrationBuilder.DropTable(
                name: "ManufacturingCommand");

            migrationBuilder.DropTable(
                name: "ProductionEvent");

            migrationBuilder.DropTable(
                name: "QualityCase");

            migrationBuilder.DropTable(
                name: "QualityTestReading");

            migrationBuilder.DropTable(
                name: "QualitySample");

            migrationBuilder.DropTable(
                name: "ProductionPhase");

            migrationBuilder.DropTable(
                name: "ProductionRun");

            migrationBuilder.DropTable(
                name: "ProductionRecipe");

            migrationBuilder.DropTable(
                name: "ProductionResource");

            migrationBuilder.DropTable(
                name: "QualitySpecification");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS greenink_manufacturing_history_immutable();");
        }
    }
}
