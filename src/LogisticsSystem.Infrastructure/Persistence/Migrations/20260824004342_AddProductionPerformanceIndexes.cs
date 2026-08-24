using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_IsActive",
                table: "Vehicles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_IsActive_Type",
                table: "Vehicles",
                columns: new[] { "IsActive", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Type",
                table: "Vehicles",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackings_ShipmentId_RecordedAt",
                table: "ShipmentTrackings",
                columns: new[] { "ShipmentId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CreatedAt",
                table: "Shipments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CustomerId_CreatedAt",
                table: "Shipments",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ScheduledAt",
                table: "Shipments",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status_CreatedAt",
                table: "Shipments",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_Status",
                table: "Drivers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchAssignments_DriverId_Status",
                table: "DispatchAssignments",
                columns: new[] { "DriverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DispatchAssignments_Status_SentAt",
                table: "DispatchAssignments",
                columns: new[] { "Status", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IsActive",
                table: "AspNetUsers",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Vehicles_IsActive",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_IsActive_Type",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_Type",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_ShipmentTrackings_ShipmentId_RecordedAt",
                table: "ShipmentTrackings");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_CreatedAt",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_CustomerId_CreatedAt",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_ScheduledAt",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_Shipments_Status_CreatedAt",
                table: "Shipments");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_IsRevoked_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_Status",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_DispatchAssignments_DriverId_Status",
                table: "DispatchAssignments");

            migrationBuilder.DropIndex(
                name: "IX_DispatchAssignments_Status_SentAt",
                table: "DispatchAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IsActive",
                table: "AspNetUsers");
        }
    }
}
