using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class OperationsLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Plants_TenantId_Id",
                table: "Plants",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateTable(
                name: "IntegrationMessage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Payload = table.Column<string>(type: "character varying(32768)", maxLength: 32768, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationMessage", x => x.Id);
                    table.UniqueConstraint("AK_IntegrationMessage_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_IntegrationMessage_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabelTemplate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabelTemplate", x => x.Id);
                    table.UniqueConstraint("AK_LabelTemplate_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_LabelTemplate_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Material",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Uom = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                    table.UniqueConstraint("AK_Material_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Material_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Partner",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partner", x => x.Id);
                    table.UniqueConstraint("AK_Partner_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Partner_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseBin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Warehouse = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarehouseBin", x => x.Id);
                    table.UniqueConstraint("AK_WarehouseBin_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_WarehouseBin_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CompletedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrder", x => x.Id);
                    table.UniqueConstraint("AK_ProductionOrder_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Production_Quantity", "\"PlannedQuantity\" > 0 AND \"CompletedQuantity\" >= 0 AND \"CompletedQuantity\" <= \"PlannedQuantity\"");
                    table.ForeignKey(
                        name: "FK_ProductionOrder_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrder_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryOrder", x => x.Id);
                    table.UniqueConstraint("AK_DeliveryOrder_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_DeliveryOrder_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_Partner_TenantId_PlantId_CustomerId",
                        columns: x => new { x.TenantId, x.PlantId, x.CustomerId },
                        principalTable: "Partner",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeliveryOrder_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReceivedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrder", x => x.Id);
                    table.UniqueConstraint("AK_PurchaseOrder_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_PO_Quantities", "\"Quantity\" > 0 AND \"ReceivedQuantity\" >= 0 AND \"ReceivedQuantity\" <= \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Partner_TenantId_PlantId_SupplierId",
                        columns: x => new { x.TenantId, x.PlantId, x.SupplierId },
                        principalTable: "Partner",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrder_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Batch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    QualityStatus = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ExpiryDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batch", x => x.Id);
                    table.UniqueConstraint("AK_Batch_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_Batch_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Batch_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Batch_ProductionOrder_TenantId_PlantId_ProductionOrderId",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BomLine",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BomLine", x => x.Id);
                    table.UniqueConstraint("AK_BomLine_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_BomLine_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_BomLine_Material_TenantId_PlantId_MaterialId",
                        columns: x => new { x.TenantId, x.PlantId, x.MaterialId },
                        principalTable: "Material",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomLine_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BomLine_ProductionOrder_TenantId_PlantId_ProductionOrderId",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DowntimeEvent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Minutes = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DowntimeEvent", x => x.Id);
                    table.UniqueConstraint("AK_DowntimeEvent_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DowntimeEvent_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DowntimeEvent_ProductionOrder_TenantId_PlantId_ProductionOr~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Location = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ReservedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pallet", x => x.Id);
                    table.UniqueConstraint("AK_Pallet_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Pallet_Stock", "\"Quantity\" >= 0 AND \"ReservedQuantity\" >= 0 AND \"ReservedQuantity\" <= \"Quantity\"");
                    table.ForeignKey(
                        name: "FK_Pallet_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pallet_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QualityInspection",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Decision = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityInspection", x => x.Id);
                    table.UniqueConstraint("AK_QualityInspection_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_QualityInspection_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QualityInspection_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Consumption",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consumption", x => x.Id);
                    table.UniqueConstraint("AK_Consumption_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Consumption_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_Consumption_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumption_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consumption_ProductionOrder_TenantId_PlantId_ProductionOrde~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceipt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierBatch = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceipt", x => x.Id);
                    table.UniqueConstraint("AK_GoodsReceipt_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_GoodsReceipt_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GoodsReceipt_PurchaseOrder_TenantId_PlantId_PurchaseOrderId",
                        columns: x => new { x.TenantId, x.PlantId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintJob",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    LabelTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Printer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Copies = table.Column<int>(type: "integer", nullable: false),
                    ReprintOfId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintJob", x => x.Id);
                    table.UniqueConstraint("AK_PrintJob_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_PrintJob_LabelTemplate_TenantId_PlantId_LabelTemplateId",
                        columns: x => new { x.TenantId, x.PlantId, x.LabelTemplateId },
                        principalTable: "LabelTemplate",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJob_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJob_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrintJob_PrintJob_TenantId_PlantId_ReprintOfId",
                        columns: x => new { x.TenantId, x.PlantId, x.ReprintOfId },
                        principalTable: "PrintJob",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionConfirmation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ScrapQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionConfirmation", x => x.Id);
                    table.UniqueConstraint("AK_ProductionConfirmation_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_ProductionConfirmation_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_ProductionConfirmation_Batch_TenantId_PlantId_BatchId",
                        columns: x => new { x.TenantId, x.PlantId, x.BatchId },
                        principalTable: "Batch",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionConfirmation_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionConfirmation_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionConfirmation_ProductionOrder_TenantId_PlantId_Pro~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservation", x => x.Id);
                    table.UniqueConstraint("AK_Reservation_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Reservation_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_Reservation_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservation_ProductionOrder_TenantId_PlantId_ProductionOrde~",
                        columns: x => new { x.TenantId, x.PlantId, x.ProductionOrderId },
                        principalTable: "ProductionOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    DeliveryOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Truck = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipment", x => x.Id);
                    table.UniqueConstraint("AK_Shipment_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Shipment_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_Shipment_DeliveryOrder_TenantId_PlantId_DeliveryOrderId",
                        columns: x => new { x.TenantId, x.PlantId, x.DeliveryOrderId },
                        principalTable: "DeliveryOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipment_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipment_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    PalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Kind = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    FromLocation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ToLocation = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Reference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovement", x => x.Id);
                    table.UniqueConstraint("AK_StockMovement_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_StockMovement_Pallet_TenantId_PlantId_PalletId",
                        columns: x => new { x.TenantId, x.PlantId, x.PalletId },
                        principalTable: "Pallet",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovement_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batch_TenantId_PlantId_CreatedAt",
                table: "Batch",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Batch_TenantId_PlantId_MaterialId",
                table: "Batch",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_Batch_TenantId_PlantId_Number",
                table: "Batch",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Batch_TenantId_PlantId_ProductionOrderId",
                table: "Batch",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomLine_TenantId_PlantId_CreatedAt",
                table: "BomLine",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BomLine_TenantId_PlantId_MaterialId",
                table: "BomLine",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_BomLine_TenantId_PlantId_ProductionOrderId_MaterialId",
                table: "BomLine",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_TenantId_PlantId_CommandId",
                table: "Consumption",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_TenantId_PlantId_CreatedAt",
                table: "Consumption",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_TenantId_PlantId_PalletId",
                table: "Consumption",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_Consumption_TenantId_PlantId_ProductionOrderId",
                table: "Consumption",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_TenantId_PlantId_CreatedAt",
                table: "DeliveryOrder",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_TenantId_PlantId_CustomerId",
                table: "DeliveryOrder",
                columns: new[] { "TenantId", "PlantId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_TenantId_PlantId_MaterialId",
                table: "DeliveryOrder",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryOrder_TenantId_PlantId_Number",
                table: "DeliveryOrder",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeEvent_TenantId_PlantId_CreatedAt",
                table: "DowntimeEvent",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeEvent_TenantId_PlantId_ProductionOrderId",
                table: "DowntimeEvent",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_BatchId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_CommandId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_CreatedAt",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_PalletId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_PurchaseOrderId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessage_TenantId_PlantId_CreatedAt",
                table: "IntegrationMessage",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationMessage_TenantId_PlantId_ExternalId",
                table: "IntegrationMessage",
                columns: new[] { "TenantId", "PlantId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabelTemplate_TenantId_PlantId_CreatedAt",
                table: "LabelTemplate",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabelTemplate_TenantId_PlantId_Name",
                table: "LabelTemplate",
                columns: new[] { "TenantId", "PlantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Material_TenantId_PlantId_Code",
                table: "Material",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Material_TenantId_PlantId_CreatedAt",
                table: "Material",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Pallet_TenantId_PlantId_BatchId",
                table: "Pallet",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pallet_TenantId_PlantId_Code",
                table: "Pallet",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pallet_TenantId_PlantId_CreatedAt",
                table: "Pallet",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Partner_TenantId_PlantId_Code",
                table: "Partner",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partner_TenantId_PlantId_CreatedAt",
                table: "Partner",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJob_TenantId_PlantId_CommandId",
                table: "PrintJob",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintJob_TenantId_PlantId_CreatedAt",
                table: "PrintJob",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJob_TenantId_PlantId_LabelTemplateId",
                table: "PrintJob",
                columns: new[] { "TenantId", "PlantId", "LabelTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJob_TenantId_PlantId_PalletId",
                table: "PrintJob",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrintJob_TenantId_PlantId_ReprintOfId",
                table: "PrintJob",
                columns: new[] { "TenantId", "PlantId", "ReprintOfId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConfirmation_TenantId_PlantId_BatchId",
                table: "ProductionConfirmation",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConfirmation_TenantId_PlantId_CommandId",
                table: "ProductionConfirmation",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConfirmation_TenantId_PlantId_CreatedAt",
                table: "ProductionConfirmation",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConfirmation_TenantId_PlantId_PalletId",
                table: "ProductionConfirmation",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionConfirmation_TenantId_PlantId_ProductionOrderId",
                table: "ProductionConfirmation",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_TenantId_PlantId_CreatedAt",
                table: "ProductionOrder",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_TenantId_PlantId_MaterialId",
                table: "ProductionOrder",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_TenantId_PlantId_Number",
                table: "ProductionOrder",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_PlantId_CreatedAt",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_PlantId_MaterialId",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "PlantId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_PlantId_Number",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "PlantId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrder_TenantId_PlantId_SupplierId",
                table: "PurchaseOrder",
                columns: new[] { "TenantId", "PlantId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspection_TenantId_PlantId_BatchId",
                table: "QualityInspection",
                columns: new[] { "TenantId", "PlantId", "BatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspection_TenantId_PlantId_CommandId",
                table: "QualityInspection",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QualityInspection_TenantId_PlantId_CreatedAt",
                table: "QualityInspection",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TenantId_PlantId_CommandId",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TenantId_PlantId_CreatedAt",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TenantId_PlantId_PalletId",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservation_TenantId_PlantId_ProductionOrderId",
                table: "Reservation",
                columns: new[] { "TenantId", "PlantId", "ProductionOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_TenantId_PlantId_CommandId",
                table: "Shipment",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_TenantId_PlantId_CreatedAt",
                table: "Shipment",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_TenantId_PlantId_DeliveryOrderId",
                table: "Shipment",
                columns: new[] { "TenantId", "PlantId", "DeliveryOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_TenantId_PlantId_PalletId",
                table: "Shipment",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_TenantId_PlantId_CommandId",
                table: "StockMovement",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_TenantId_PlantId_CreatedAt",
                table: "StockMovement",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovement_TenantId_PlantId_PalletId",
                table: "StockMovement",
                columns: new[] { "TenantId", "PlantId", "PalletId" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBin_TenantId_PlantId_Code",
                table: "WarehouseBin",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseBin_TenantId_PlantId_CreatedAt",
                table: "WarehouseBin",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });
            migrationBuilder.Sql("""
                CREATE FUNCTION greenink_stock_immutable() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN RAISE EXCEPTION 'Stock movements are immutable; post a correction' USING ERRCODE = '23514'; END;
                $$;
                CREATE TRIGGER greenink_stock_immutable BEFORE UPDATE OR DELETE ON "StockMovement"
                FOR EACH ROW EXECUTE FUNCTION greenink_stock_immutable();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER greenink_stock_immutable ON \"StockMovement\"; DROP FUNCTION greenink_stock_immutable();");
            migrationBuilder.DropTable(
                name: "BomLine");

            migrationBuilder.DropTable(
                name: "Consumption");

            migrationBuilder.DropTable(
                name: "DowntimeEvent");

            migrationBuilder.DropTable(
                name: "GoodsReceipt");

            migrationBuilder.DropTable(
                name: "IntegrationMessage");

            migrationBuilder.DropTable(
                name: "PrintJob");

            migrationBuilder.DropTable(
                name: "ProductionConfirmation");

            migrationBuilder.DropTable(
                name: "QualityInspection");

            migrationBuilder.DropTable(
                name: "Reservation");

            migrationBuilder.DropTable(
                name: "Shipment");

            migrationBuilder.DropTable(
                name: "StockMovement");

            migrationBuilder.DropTable(
                name: "WarehouseBin");

            migrationBuilder.DropTable(
                name: "PurchaseOrder");

            migrationBuilder.DropTable(
                name: "LabelTemplate");

            migrationBuilder.DropTable(
                name: "DeliveryOrder");

            migrationBuilder.DropTable(
                name: "Pallet");

            migrationBuilder.DropTable(
                name: "Partner");

            migrationBuilder.DropTable(
                name: "Batch");

            migrationBuilder.DropTable(
                name: "ProductionOrder");

            migrationBuilder.DropTable(
                name: "Material");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Plants_TenantId_Id",
                table: "Plants");
        }
    }
}
