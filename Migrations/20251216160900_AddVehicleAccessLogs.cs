using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultX_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleAccessLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_access_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    vehicleId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    accessType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false),
                    gateName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    recordedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_vehicle_access_logs_vehicles",
                        column: x => x.vehicleId,
                        principalTable: "vehicles",
                        principalColumn: "vehicleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_access_logs_timestamp",
                table: "vehicle_access_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_access_logs_vehicleId",
                table: "vehicle_access_logs",
                column: "vehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_access_logs");
        }
    }
}
