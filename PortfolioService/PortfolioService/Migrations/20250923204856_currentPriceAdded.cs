using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioService.Migrations
{
    /// <inheritdoc />
    public partial class currentPriceAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CurrentPrice",
                table: "PortfolioItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPrice",
                table: "PortfolioItems");
        }
    }
}
