using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project.Migrations
{
    /// <inheritdoc />
    public partial class Updatroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Rolls_EventId",
                table: "Rolls",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Rolls_Events_EventId",
                table: "Rolls",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rolls_Events_EventId",
                table: "Rolls");

            migrationBuilder.DropIndex(
                name: "IX_Rolls_EventId",
                table: "Rolls");
        }
    }
}
