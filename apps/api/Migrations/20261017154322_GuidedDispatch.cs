using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class GuidedDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DispatchArea",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ShippingPoint = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PackBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoadingBinId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickMode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PackMode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    AllowOverpick = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchArea", x => x.Id);
                    table.UniqueConstraint("AK_DispatchArea_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DispatchArea_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchArea_WarehouseBin_TenantId_PlantId_LoadingBinId",
                        columns: x => new { x.TenantId, x.PlantId, x.LoadingBinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchArea_WarehouseBin_TenantId_PlantId_PackBinId",
                        columns: x => new { x.TenantId, x.PlantId, x.PackBinId },
                        principalTable: "WarehouseBin",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchCommand",
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
                    table.PrimaryKey("PK_DispatchCommand", x => x.Id);
                    table.UniqueConstraint("AK_DispatchCommand_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DispatchCommand_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchInstruction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Picking = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Packing = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Shipping = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Safety = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    MinimumShelfDays = table.Column<int>(type: "integer", nullable: false),
                    ProductPacking = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchInstruction", x => x.Id);
                    table.UniqueConstraint("AK_DispatchInstruction_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchInstruction_Shelf", "\"MinimumShelfDays\" BETWEEN 0 AND 3650");
                    table.ForeignKey(
                        name: "FK_DispatchInstruction_Partner_TenantId_PlantId_CustomerId",
                        columns: x => new { x.TenantId, x.PlantId, x.CustomerId },
                        principalTable: "Partner",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchInstruction_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPackage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    LengthCm = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    WidthCm = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    HeightCm = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TareKg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    MaximumGrossKg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TemperatureDeviceRequired = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPackage", x => x.Id);
                    table.UniqueConstraint("AK_DispatchPackage_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchPackage_Size", "\"LengthCm\" > 0 AND \"WidthCm\" > 0 AND \"HeightCm\" > 0 AND \"TareKg\" >= 0 AND \"MaximumGrossKg\" > \"TareKg\"");
                    table.ForeignKey(
                        name: "FK_DispatchPackage_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchWarehouse",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ReceivingPlant = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    StorageLocation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Gate = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchWarehouse", x => x.Id);
                    table.UniqueConstraint("AK_DispatchWarehouse_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DispatchWarehouse_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DispatchAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedToId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<bool>(type: "boolean", nullable: false),
                    Route = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Carrier = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Truck = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SettingsJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReversedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GoodsIssueReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRun", x => x.Id);
                    table.UniqueConstraint("AK_DispatchRun_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchRun_State", "\"Status\" IN ('Planned','Picking','Picked','Packing','Packed','Loading','Loaded','Issued','Cancelled','Reversed') AND \"Kind\" IN ('Customer','Shuttle') AND ((\"Kind\"='Customer' AND \"DispatchWarehouseId\" IS NULL) OR (\"Kind\"='Shuttle' AND \"DispatchWarehouseId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_DispatchRun_DispatchArea_TenantId_PlantId_DispatchAreaId",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchAreaId },
                        principalTable: "DispatchArea",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRun_DispatchWarehouse_TenantId_PlantId_DispatchWare~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchWarehouseId },
                        principalTable: "DispatchWarehouse",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRun_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRun_Users_AssignedToId",
                        column: x => x.AssignedToId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_DispatchEvent", x => x.Id);
                    table.UniqueConstraint("AK_DispatchEvent_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DispatchEvent_DispatchRun_TenantId_PlantId_DispatchRunId",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunId },
                        principalTable: "DispatchRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchEvent_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchHandlingUnit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchPackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PackageJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TemperatureDevice = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    GrossKg = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchHandlingUnit", x => x.Id);
                    table.UniqueConstraint("AK_DispatchHandlingUnit_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DispatchHandlingUnit_DispatchPackage_TenantId_PlantId_Dispa~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchPackageId },
                        principalTable: "DispatchPackage",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchHandlingUnit_DispatchRun_TenantId_PlantId_DispatchR~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunId },
                        principalTable: "DispatchRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchHandlingUnit_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchRunLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    InstructionsJson = table.Column<string>(type: "jsonb", maxLength: 16000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchRunLine", x => x.Id);
                    table.UniqueConstraint("AK_DispatchRunLine_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchRunLine_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_DispatchRunLine_DeliveryOrder_TenantId_PlantId_DeliveryOrde~",
                        columns: x => new { x.TenantId, x.PlantId, x.DeliveryOrderId },
                        principalTable: "DeliveryOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRunLine_DispatchRun_TenantId_PlantId_DispatchRunId",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunId },
                        principalTable: "DispatchRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRunLine_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRunLine_Partner_TenantId_PlantId_CustomerId",
                        columns: x => new { x.TenantId, x.PlantId, x.CustomerId },
                        principalTable: "Partner",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchRunLine_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchPick",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickedPalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    DispatchHandlingUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchPick", x => x.Id);
                    table.UniqueConstraint("AK_DispatchPick_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchPick_Quantity", "\"PickedQuantity\" > 0 AND \"Quantity\" >= 0 AND \"ReturnedQuantity\" >= 0 AND \"Quantity\" + \"ReturnedQuantity\" = \"PickedQuantity\"");
                    table.ForeignKey(
                        name: "FK_DispatchPick_DispatchHandlingUnit_TenantId_PlantId_Dispatch~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchHandlingUnitId },
                        principalTable: "DispatchHandlingUnit",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPick_DispatchRunLine_TenantId_PlantId_DispatchRunLi~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunLineId },
                        principalTable: "DispatchRunLine",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPick_DispatchRun_TenantId_PlantId_DispatchRunId",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunId },
                        principalTable: "DispatchRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPick_Pallet_TenantId_PlantId_PickedPalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PickedPalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPick_Pallet_TenantId_PlantId_SourcePalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.SourcePalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchPick_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchIssueLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchPickId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchIssueLine", x => x.Id);
                    table.UniqueConstraint("AK_DispatchIssueLine_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchIssueLine_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_DispatchIssueLine_DispatchPick_TenantId_PlantId_DispatchPic~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchPickId },
                        principalTable: "DispatchPick",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchIssueLine_DispatchRun_TenantId_PlantId_DispatchRunId",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchRunId },
                        principalTable: "DispatchRun",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchIssueLine_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchIssueLine_Shipment_TenantId_PlantId_ShipmentId",
                        columns: x => new { x.TenantId, x.PlantId, x.ShipmentId },
                        principalTable: "Shipment",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispatchReversalLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchIssueLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchReversalLine", x => x.Id);
                    table.UniqueConstraint("AK_DispatchReversalLine_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DispatchReversalLine_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_DispatchReversalLine_DispatchIssueLine_TenantId_PlantId_Dis~",
                        columns: x => new { x.TenantId, x.PlantId, x.DispatchIssueLineId },
                        principalTable: "DispatchIssueLine",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchReversalLine_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DispatchReversalLine_Shipment_TenantId_PlantId_ShipmentId",
                        columns: x => new { x.TenantId, x.PlantId, x.ShipmentId },
                        principalTable: "Shipment",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchArea_TenantId_PlantId_Code",
                table: "DispatchArea",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchArea_TenantId_PlantId_CreatedAt",
                table: "DispatchArea",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchArea_TenantId_PlantId_LoadingBinId",
                table: "DispatchArea",
                columns: new[] { "TenantId", "PlantId", "LoadingBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchArea_TenantId_PlantId_PackBinId",
                table: "DispatchArea",
                columns: new[] { "TenantId", "PlantId", "PackBinId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchCommand_TenantId_PlantId_CommandId",
                table: "DispatchCommand",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchCommand_TenantId_PlantId_CreatedAt",
                table: "DispatchCommand",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchEvent_TenantId_PlantId_CreatedAt",
                table: "DispatchEvent",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchEvent_TenantId_PlantId_DispatchRunId",
                table: "DispatchEvent",
                columns: new[] { "TenantId", "PlantId", "DispatchRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchHandlingUnit_TenantId_PlantId_Code",
                table: "DispatchHandlingUnit",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchHandlingUnit_TenantId_PlantId_CreatedAt",
                table: "DispatchHandlingUnit",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchHandlingUnit_TenantId_PlantId_DispatchPackageId",
                table: "DispatchHandlingUnit",
                columns: new[] { "TenantId", "PlantId", "DispatchPackageId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchHandlingUnit_TenantId_PlantId_DispatchRunId",
                table: "DispatchHandlingUnit",
                columns: new[] { "TenantId", "PlantId", "DispatchRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchInstruction_TenantId_PlantId_CreatedAt",
                table: "DispatchInstruction",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchInstruction_TenantId_PlantId_CustomerId",
                table: "DispatchInstruction",
                columns: new[] { "TenantId", "PlantId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchIssueLine_TenantId_PlantId_CreatedAt",
                table: "DispatchIssueLine",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchIssueLine_TenantId_PlantId_DispatchPickId",
                table: "DispatchIssueLine",
                columns: new[] { "TenantId", "PlantId", "DispatchPickId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchIssueLine_TenantId_PlantId_DispatchRunId",
                table: "DispatchIssueLine",
                columns: new[] { "TenantId", "PlantId", "DispatchRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchIssueLine_TenantId_PlantId_ShipmentId",
                table: "DispatchIssueLine",
                columns: new[] { "TenantId", "PlantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPackage_TenantId_PlantId_Code",
                table: "DispatchPackage",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPackage_TenantId_PlantId_CreatedAt",
                table: "DispatchPackage",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_CreatedAt",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_DispatchHandlingUnitId",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "DispatchHandlingUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_DispatchRunId",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "DispatchRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_DispatchRunLineId",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "DispatchRunLineId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_PickedPalletId",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "PickedPalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchPick_TenantId_PlantId_SourcePalletId",
                table: "DispatchPick",
                columns: new[] { "TenantId", "PlantId", "SourcePalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchReversalLine_TenantId_PlantId_CreatedAt",
                table: "DispatchReversalLine",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchReversalLine_TenantId_PlantId_DispatchIssueLineId",
                table: "DispatchReversalLine",
                columns: new[] { "TenantId", "PlantId", "DispatchIssueLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchReversalLine_TenantId_PlantId_ShipmentId",
                table: "DispatchReversalLine",
                columns: new[] { "TenantId", "PlantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRun_AssignedToId",
                table: "DispatchRun",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRun_TenantId_PlantId_CreatedAt",
                table: "DispatchRun",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRun_TenantId_PlantId_DispatchAreaId",
                table: "DispatchRun",
                columns: new[] { "TenantId", "PlantId", "DispatchAreaId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRun_TenantId_PlantId_DispatchWarehouseId",
                table: "DispatchRun",
                columns: new[] { "TenantId", "PlantId", "DispatchWarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRun_TenantId_PlantId_Number",
                table: "DispatchRun",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRunLine_TenantId_PlantId_CreatedAt",
                table: "DispatchRunLine",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRunLine_TenantId_PlantId_CustomerId",
                table: "DispatchRunLine",
                columns: new[] { "TenantId", "PlantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRunLine_TenantId_PlantId_DeliveryOrderId",
                table: "DispatchRunLine",
                columns: new[] { "TenantId", "PlantId", "DeliveryOrderId" },
                unique: true,
                filter: "\"Active\" AND \"DeliveryOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRunLine_TenantId_PlantId_DispatchRunId",
                table: "DispatchRunLine",
                columns: new[] { "TenantId", "PlantId", "DispatchRunId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchRunLine_TenantId_PlantId_MaterialId",
                table: "DispatchRunLine",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchWarehouse_TenantId_PlantId_Code",
                table: "DispatchWarehouse",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispatchWarehouse_TenantId_PlantId_CreatedAt",
                table: "DispatchWarehouse",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });
            migrationBuilder.Sql(@"CREATE FUNCTION greenink_dispatch_history_immutable() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Dispatch history is append-only'; END $$;
CREATE TRIGGER dispatchevent_immutable BEFORE UPDATE OR DELETE ON ""DispatchEvent"" FOR EACH ROW EXECUTE FUNCTION greenink_dispatch_history_immutable();
CREATE TRIGGER dispatchcommand_immutable BEFORE UPDATE OR DELETE ON ""DispatchCommand"" FOR EACH ROW EXECUTE FUNCTION greenink_dispatch_history_immutable();
CREATE TRIGGER dispatchissueline_immutable BEFORE UPDATE OR DELETE ON ""DispatchIssueLine"" FOR EACH ROW EXECUTE FUNCTION greenink_dispatch_history_immutable();
CREATE TRIGGER dispatchreversalline_immutable BEFORE UPDATE OR DELETE ON ""DispatchReversalLine"" FOR EACH ROW EXECUTE FUNCTION greenink_dispatch_history_immutable();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispatchCommand");

            migrationBuilder.DropTable(
                name: "DispatchEvent");

            migrationBuilder.DropTable(
                name: "DispatchInstruction");

            migrationBuilder.DropTable(
                name: "DispatchReversalLine");

            migrationBuilder.DropTable(
                name: "DispatchIssueLine");

            migrationBuilder.DropTable(
                name: "DispatchPick");

            migrationBuilder.DropTable(
                name: "DispatchHandlingUnit");

            migrationBuilder.DropTable(
                name: "DispatchRunLine");

            migrationBuilder.DropTable(
                name: "DispatchPackage");

            migrationBuilder.DropTable(
                name: "DispatchRun");

            migrationBuilder.DropTable(
                name: "DispatchArea");

            migrationBuilder.DropTable(
                name: "DispatchWarehouse");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS greenink_dispatch_history_immutable();");
        }
    }
}
