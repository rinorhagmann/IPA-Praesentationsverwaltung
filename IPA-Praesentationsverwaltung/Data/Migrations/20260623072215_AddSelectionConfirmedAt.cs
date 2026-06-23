using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IPA_Praesentationsverwaltung.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectionConfirmedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SelectionConfirmedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectionConfirmedAt",
                table: "Users");
        }
    }
}
