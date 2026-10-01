using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Escola.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTurmaIdDataToLogAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Data",
                table: "LogsAuditoria",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TurmaId",
                table: "LogsAuditoria",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LogsAuditoria_TurmaId_Data",
                table: "LogsAuditoria",
                columns: new[] { "TurmaId", "Data" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LogsAuditoria_TurmaId_Data",
                table: "LogsAuditoria");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "LogsAuditoria");

            migrationBuilder.DropColumn(
                name: "TurmaId",
                table: "LogsAuditoria");
        }
    }
}
