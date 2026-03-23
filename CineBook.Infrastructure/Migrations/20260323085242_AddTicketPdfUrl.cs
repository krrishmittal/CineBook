using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketPdfUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TicketPdfUrl",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TicketPdfUrl",
                table: "Bookings");
        }
    }
}
