using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class MaterialPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RequirementQuantity",
                table: "Reservation",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StagingPickId",
                table: "Reservation",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RequirementQuantity",
                table: "Consumption",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BatchStagingFactor",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Evidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchStagingFactor", x => x.Id);
                    table.UniqueConstraint("AK_BatchStagingFactor_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_BatchStagingFactor_Staging", "\"Factor\" > 0");
                    table.ForeignKey(
                        name: "FK_BatchStagingFactor_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchStagingFactor_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KanbanPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    BinId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LowStock = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReorderStock = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    MaxStock = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanPolicy", x => x.Id);
                    table.UniqueConstraint("AK_KanbanPolicy_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_KanbanPolicy_Staging", "\"LowStock\" >= 0 AND \"LowStock\" <= \"ReorderStock\" AND \"ReorderStock\" <= \"MaxStock\" AND \"MaxStock\" > 0");
                    table.ForeignKey(
                        name: "FK_KanbanPolicy_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KanbanPolicy_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KanbanPolicy_WarehouseBin_TenantId_PlantId_BinId",
                        columns: x => new { x.TenantId, x.PlantId, x.BinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingArea",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StagingBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplyBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    Workflow = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingArea", x => x.Id);
                    table.UniqueConstraint("AK_StagingArea_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StagingArea_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingArea_WarehouseBin_TenantId_PlantId_StagingBinId",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingBinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingArea_WarehouseBin_TenantId_PlantId_SupplyBinId",
                        columns: x => new { x.TenantId, x.PlantId, x.SupplyBinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingRule",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackSize = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PreweighRequired = table.Column<bool>(type: "boolean", nullable: false),
                    TolerancePercent = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CorrectionRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SafetyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HandlingNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingRule", x => x.Id);
                    table.UniqueConstraint("AK_StagingRule_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_StagingRule_Staging", "\"PackSize\" >= 0 AND \"TolerancePercent\" >= 0 AND \"TolerancePercent\" <= 100");
                    table.ForeignKey(
                        name: "FK_StagingRule_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingRule_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingStockIssue",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingStockIssue", x => x.Id);
                    table.UniqueConstraint("AK_StagingStockIssue_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StagingStockIssue_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingStockIssue_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingPlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StagingAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovalNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Workflow = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    KitCode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingPlan", x => x.Id);
                    table.UniqueConstraint("AK_StagingPlan_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StagingPlan_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPlan_ProductionOrder_TenantId_PlantId_ProductionOrde~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPlan_StagingArea_TenantId_PlantId_StagingAreaId",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingAreaId },
                        principalTable: "StagingArea",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReplenishmentRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    StagingPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    FulfilledQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplenishmentRequest", x => x.Id);
                    table.UniqueConstraint("AK_ReplenishmentRequest_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_ReplenishmentRequest_Staging", "\"Quantity\" > 0 AND \"FulfilledQuantity\" >= 0 AND \"FulfilledQuantity\" <= \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_StagingPlan_TenantId_PlantId_StagingPl~",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingPlanId },
                        principalTable: "StagingPlan",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReplenishmentRequest_WarehouseBin_TenantId_PlantId_Destinat~",
                        columns: x => new { x.TenantId, x.PlantId, x.DestinationBinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingCommand",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Signature = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    StagingPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingCommand", x => x.Id);
                    table.UniqueConstraint("AK_StagingCommand_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StagingCommand_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingCommand_StagingPlan_TenantId_PlantId_StagingPlanId",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingPlanId },
                        principalTable: "StagingPlan",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingPick",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    StagingPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickedPalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentPalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    RequirementQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CorrectionFactor = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TolerancePercent = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PreweighRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SafetyNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    HandlingNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingPick", x => x.Id);
                    table.UniqueConstraint("AK_StagingPick_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_StagingPick_Staging", "\"Quantity\" > 0 AND \"RequirementQuantity\" > 0 AND \"CorrectionFactor\" > 0");
                    table.ForeignKey(
                        name: "FK_StagingPick_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPick_Pallet_TenantId_PlantId_CurrentPalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.CurrentPalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPick_Pallet_TenantId_PlantId_PickedPalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PickedPalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPick_Pallet_TenantId_PlantId_SourcePalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.SourcePalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPick_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingPick_StagingPlan_TenantId_PlantId_StagingPlanId",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingPlanId },
                        principalTable: "StagingPlan",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StagingMeasurement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    StagingPickId = table.Column<Guid>(type: "uuid", nullable: false),
                    Gross = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Tare = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Net = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ScaleReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Method = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingMeasurement", x => x.Id);
                    table.UniqueConstraint("AK_StagingMeasurement_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_StagingMeasurement_Staging", "\"Gross\" > \"Tare\" AND \"Tare\" >= 0 AND \"Net\" = \"Gross\" - \"Tare\" AND \"ExpectedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_StagingMeasurement_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StagingMeasurement_StagingPick_TenantId_PlantId_StagingPick~",
                        columns: x => new { x.TenantId, x.PlantId, x.StagingPickId },
                        principalTable: "StagingPick",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TenantId_PlantId_StagingPickId",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "StagingPickId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservation_Staging",
                table: "Reservation",
                sql: "\"RequirementQuantity\" IS NULL OR \"RequirementQuantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Consumption_Staging",
                table: "Consumption",
                sql: "\"RequirementQuantity\" IS NULL OR \"RequirementQuantity\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_BatchStagingFactor_TenantId_PlantId_BatchId",
                table: "BatchStagingFactor",
                columns: new[] { "TenantId", "PlantId", "BatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchStagingFactor_TenantId_PlantId_CreatedAt",
                table: "BatchStagingFactor",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanPolicy_TenantId_PlantId_BinId_MaterialId",
                table: "KanbanPolicy",
                columns: new[] { "TenantId", "PlantId", "BinId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanPolicy_TenantId_PlantId_CreatedAt",
                table: "KanbanPolicy",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_KanbanPolicy_TenantId_PlantId_MaterialId",
                table: "KanbanPolicy",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_TenantId_PlantId_CreatedAt",
                table: "ReplenishmentRequest",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_TenantId_PlantId_DestinationBinId",
                table: "ReplenishmentRequest",
                columns: new[] { "TenantId", "PlantId", "DestinationBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_TenantId_PlantId_MaterialId",
                table: "ReplenishmentRequest",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReplenishmentRequest_TenantId_PlantId_StagingPlanId",
                table: "ReplenishmentRequest",
                columns: new[] { "TenantId", "PlantId", "StagingPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingArea_TenantId_PlantId_Code",
                table: "StagingArea",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingArea_TenantId_PlantId_CreatedAt",
                table: "StagingArea",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingArea_TenantId_PlantId_StagingBinId",
                table: "StagingArea",
                columns: new[] { "TenantId", "PlantId", "StagingBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingArea_TenantId_PlantId_SupplyBinId",
                table: "StagingArea",
                columns: new[] { "TenantId", "PlantId", "SupplyBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingCommand_TenantId_PlantId_CommandId",
                table: "StagingCommand",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingCommand_TenantId_PlantId_CreatedAt",
                table: "StagingCommand",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingCommand_TenantId_PlantId_StagingPlanId",
                table: "StagingCommand",
                columns: new[] { "TenantId", "PlantId", "StagingPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingMeasurement_TenantId_PlantId_CreatedAt",
                table: "StagingMeasurement",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingMeasurement_TenantId_PlantId_StagingPickId",
                table: "StagingMeasurement",
                columns: new[] { "TenantId", "PlantId", "StagingPickId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_CreatedAt",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_CurrentPalletId",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "CurrentPalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_MaterialId",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_PickedPalletId",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "PickedPalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_SourcePalletId",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "SourcePalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPick_TenantId_PlantId_StagingPlanId",
                table: "StagingPick",
                columns: new[] { "TenantId", "PlantId", "StagingPlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPlan_TenantId_PlantId_CreatedAt",
                table: "StagingPlan",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingPlan_TenantId_PlantId_KitCode",
                table: "StagingPlan",
                columns: new[] { "TenantId", "PlantId", "KitCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingPlan_TenantId_PlantId_ProductionOrderId",
                table: "StagingPlan",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingPlan_TenantId_PlantId_StagingAreaId",
                table: "StagingPlan",
                columns: new[] { "TenantId", "PlantId", "StagingAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingRule_TenantId_PlantId_CreatedAt",
                table: "StagingRule",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingRule_TenantId_PlantId_MaterialId",
                table: "StagingRule",
                columns: new[] { "TenantId", "PlantId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StagingStockIssue_TenantId_PlantId_CreatedAt",
                table: "StagingStockIssue",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StagingStockIssue_TenantId_PlantId_PalletId",
                table: "StagingStockIssue",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservation_StagingPick_TenantId_PlantId_StagingPickId",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "StagingPickId" },
                principalTable: "StagingPick",
                principalColumns: new[] { "TenantId", "PlantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservation_StagingPick_TenantId_PlantId_StagingPickId",
                table: "Reservation");

            migrationBuilder.DropTable(
                name: "BatchStagingFactor");

            migrationBuilder.DropTable(
                name: "KanbanPolicy");

            migrationBuilder.DropTable(
                name: "ReplenishmentRequest");

            migrationBuilder.DropTable(
                name: "StagingCommand");

            migrationBuilder.DropTable(
                name: "StagingMeasurement");

            migrationBuilder.DropTable(
                name: "StagingRule");

            migrationBuilder.DropTable(
                name: "StagingStockIssue");

            migrationBuilder.DropTable(
                name: "StagingPick");

            migrationBuilder.DropTable(
                name: "StagingPlan");

            migrationBuilder.DropTable(
                name: "StagingArea");

            migrationBuilder.DropIndex(
                name: "IX_Reservation_TenantId_PlantId_StagingPickId",
                table: "Reservation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservation_Staging",
                table: "Reservation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Consumption_Staging",
                table: "Consumption");

            migrationBuilder.DropColumn(
                name: "RequirementQuantity",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "StagingPickId",
                table: "Reservation");

            migrationBuilder.DropColumn(
                name: "RequirementQuantity",
                table: "Consumption");
        }
    }
}
