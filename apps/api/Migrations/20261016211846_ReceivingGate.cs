using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReceivingGate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "GoodsReceipt",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "LooseQuantity",
                table: "GoodsReceipt",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PackCount",
                table: "GoodsReceipt",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PackSize",
                table: "GoodsReceipt",
                type: "numeric(20,6)",
                precision: 20,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TruckVisitId",
                table: "GoodsReceipt",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DockStation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Code = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Maintenance = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockStation", x => x.Id);
                    table.UniqueConstraint("AK_DockStation_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_DockStation_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceivingCheck",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Prompt = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Stage = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePass = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingCheck", x => x.Id);
                    table.UniqueConstraint("AK_ReceivingCheck_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReceivingCheck_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReceivingPolicy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    RequireTruck = table.Column<bool>(type: "boolean", nullable: false),
                    RequireDeliveryNote = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceivingPolicy", x => x.Id);
                    table.UniqueConstraint("AK_ReceivingPolicy_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReceivingPolicy_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TruckVisit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TruckNumber = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    DriverName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Token = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Status = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArrivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnloadingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UnloadingCompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DockStationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderSignature = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruckVisit", x => x.Id);
                    table.UniqueConstraint("AK_TruckVisit_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.CheckConstraint("CK_Visit_Status", "\"Status\" IN ('Expected','Arrived','Docked','Unloading','Unloaded','Out')");
                    table.ForeignKey(
                        name: "FK_TruckVisit_DockStation_TenantId_PlantId_DockStationId",
                        columns: x => new { x.TenantId, x.PlantId, x.DockStationId },
                        principalTable: "DockStation",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TruckVisit_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TruckCheckAnswer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TruckVisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Stage = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    RequirePass = table.Column<bool>(type: "boolean", nullable: false),
                    Passed = table.Column<bool>(type: "boolean", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AnsweredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruckCheckAnswer", x => x.Id);
                    table.UniqueConstraint("AK_TruckCheckAnswer_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TruckCheckAnswer_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TruckCheckAnswer_TruckVisit_TenantId_PlantId_TruckVisitId",
                        columns: x => new { x.TenantId, x.PlantId, x.TruckVisitId },
                        principalTable: "TruckVisit",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TruckOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    TruckVisitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruckOrder", x => x.Id);
                    table.UniqueConstraint("AK_TruckOrder_TenantId_PlantId_Id", x => new { x.TenantId, x.PlantId, x.Id });
                    table.ForeignKey(
                        name: "FK_TruckOrder_Plants_TenantId_PlantId",
                        columns: x => new { x.TenantId, x.PlantId },
                        principalTable: "Plants",
                        principalColumns: new[] { "TenantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TruckOrder_PurchaseOrder_TenantId_PlantId_PurchaseOrderId",
                        columns: x => new { x.TenantId, x.PlantId, x.PurchaseOrderId },
                        principalTable: "PurchaseOrder",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TruckOrder_TruckVisit_TenantId_PlantId_TruckVisitId",
                        columns: x => new { x.TenantId, x.PlantId, x.TruckVisitId },
                        principalTable: "TruckVisit",
                        principalColumns: new[] { "TenantId", "PlantId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_TruckVisitId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "TruckVisitId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Receipt_Packs",
                table: "GoodsReceipt",
                sql: "(\"PackSize\" IS NULL AND \"PackCount\" IS NULL AND \"LooseQuantity\" = 0) OR (\"PackSize\" IS NOT NULL AND \"PackCount\" IS NOT NULL AND \"PackSize\" > 0 AND \"PackCount\" > 0 AND \"LooseQuantity\" >= 0 AND \"Quantity\" = \"PackSize\" * \"PackCount\" + \"LooseQuantity\")");

            migrationBuilder.CreateIndex(
                name: "IX_DockStation_TenantId_PlantId_Code",
                table: "DockStation",
                columns: new[] { "TenantId", "PlantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DockStation_TenantId_PlantId_CreatedAt",
                table: "DockStation",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingCheck_TenantId_PlantId_CreatedAt",
                table: "ReceivingCheck",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingCheck_TenantId_PlantId_Prompt_Stage",
                table: "ReceivingCheck",
                columns: new[] { "TenantId", "PlantId", "Prompt", "Stage" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingPolicy_TenantId_PlantId",
                table: "ReceivingPolicy",
                columns: new[] { "TenantId", "PlantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceivingPolicy_TenantId_PlantId_CreatedAt",
                table: "ReceivingPolicy",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckCheckAnswer_TenantId_PlantId_CreatedAt",
                table: "TruckCheckAnswer",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckCheckAnswer_TenantId_PlantId_TruckVisitId",
                table: "TruckCheckAnswer",
                columns: new[] { "TenantId", "PlantId", "TruckVisitId" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckOrder_TenantId_PlantId_CreatedAt",
                table: "TruckOrder",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckOrder_TenantId_PlantId_PurchaseOrderId",
                table: "TruckOrder",
                columns: new[] { "TenantId", "PlantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckOrder_TenantId_PlantId_TruckVisitId_PurchaseOrderId",
                table: "TruckOrder",
                columns: new[] { "TenantId", "PlantId", "TruckVisitId", "PurchaseOrderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TruckVisit_TenantId_PlantId_CommandId",
                table: "TruckVisit",
                columns: new[] { "TenantId", "PlantId", "CommandId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TruckVisit_TenantId_PlantId_CreatedAt",
                table: "TruckVisit",
                columns: new[] { "TenantId", "PlantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckVisit_TenantId_PlantId_DockStationId",
                table: "TruckVisit",
                columns: new[] { "TenantId", "PlantId", "DockStationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TruckVisit_TenantId_PlantId_Token",
                table: "TruckVisit",
                columns: new[] { "TenantId", "PlantId", "Token" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceipt_TruckVisit_TenantId_PlantId_TruckVisitId",
                table: "GoodsReceipt",
                columns: new[] { "TenantId", "PlantId", "TruckVisitId" },
                principalTable: "TruckVisit",
                principalColumns: new[] { "TenantId", "PlantId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceipt_TruckVisit_TenantId_PlantId_TruckVisitId",
                table: "GoodsReceipt");

            migrationBuilder.DropTable(
                name: "ReceivingCheck");

            migrationBuilder.DropTable(
                name: "ReceivingPolicy");

            migrationBuilder.DropTable(
                name: "TruckCheckAnswer");

            migrationBuilder.DropTable(
                name: "TruckOrder");

            migrationBuilder.DropTable(
                name: "TruckVisit");

            migrationBuilder.DropTable(
                name: "DockStation");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceipt_TenantId_PlantId_TruckVisitId",
                table: "GoodsReceipt");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Receipt_Packs",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "LooseQuantity",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "PackCount",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "PackSize",
                table: "GoodsReceipt");

            migrationBuilder.DropColumn(
                name: "TruckVisitId",
                table: "GoodsReceipt");
        }
    }
}
