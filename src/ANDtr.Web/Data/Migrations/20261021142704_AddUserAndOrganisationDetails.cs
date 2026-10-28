using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ANDtr.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAndOrganisationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "CustomerOrganisations",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CustomerOrganisations",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "CustomerOrganisations",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Existing rows: use the email as the name and today as the created date.
            migrationBuilder.Sql("UPDATE AspNetUsers SET FullName = COALESCE(Email, UserName, '') WHERE FullName = '';");
            migrationBuilder.Sql("UPDATE AspNetUsers SET CreatedAt = datetime('now');");
            migrationBuilder.Sql("UPDATE CustomerOrganisations SET CreatedAt = datetime('now');");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrganisations_Name",
                table: "CustomerOrganisations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerOrganisations_Name",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "CustomerOrganisations");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "AspNetUsers");
        }
    }
}
