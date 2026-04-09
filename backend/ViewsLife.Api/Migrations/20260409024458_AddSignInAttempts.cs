using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViewsLife.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSignInAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignInAttempts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastFailedAttemptUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignInAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignInAttempts_NormalizedEmail",
                table: "SignInAttempts",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignInAttempts");
        }
    }
}
