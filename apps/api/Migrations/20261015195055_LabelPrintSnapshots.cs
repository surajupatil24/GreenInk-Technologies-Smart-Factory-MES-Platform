using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenInk.Api.Migrations
{
    /// <inheritdoc />
    public partial class LabelPrintSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RenderedContent",
                table: "PrintJob",
                type: "character varying(32768)",
                maxLength: 32768,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TemplateRevision",
                table: "PrintJob",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenderedContent",
                table: "PrintJob");

            migrationBuilder.DropColumn(
                name: "TemplateRevision",
                table: "PrintJob");
        }
    }
}
