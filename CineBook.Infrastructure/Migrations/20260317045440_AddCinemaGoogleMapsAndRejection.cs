using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CineBook.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCinemaGoogleMapsAndRejection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RejectionStatus",
                table: "Cinemas",
                newName: "RejectionReason");

            migrationBuilder.RenameColumn(
                name: "GoogleMapLink",
                table: "Cinemas",
                newName: "GoogleMapsLink");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "Cinemas",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RejectionReason",
                table: "Cinemas",
                newName: "RejectionStatus");

            migrationBuilder.RenameColumn(
                name: "GoogleMapsLink",
                table: "Cinemas",
                newName: "GoogleMapLink");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RegisteredAt",
                table: "Cinemas",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
