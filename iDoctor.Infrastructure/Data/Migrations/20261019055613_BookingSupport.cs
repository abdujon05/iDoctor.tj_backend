using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iDoctor.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class BookingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_appointments_SlotId",
                table: "appointments");

            migrationBuilder.AddCheckConstraint(
                name: "ck_slot_duration",
                table: "doctor_working_hours",
                sql: "\"SlotDurationMinutes\" BETWEEN 5 AND 240");

            migrationBuilder.AddCheckConstraint(
                name: "ck_working_hours_range",
                table: "doctor_working_hours",
                sql: "\"EndTime\" > \"StartTime\"");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_SlotId",
                table: "appointments",
                column: "SlotId",
                unique: true,
                filter: "\"Status\" NOT IN ('CancelledByPatient', 'CancelledByDoctor')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_slot_time_range",
                table: "appointment_slots",
                sql: "\"EndTime\" > \"StartTime\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_slot_duration",
                table: "doctor_working_hours");

            migrationBuilder.DropCheckConstraint(
                name: "ck_working_hours_range",
                table: "doctor_working_hours");

            migrationBuilder.DropIndex(
                name: "IX_appointments_SlotId",
                table: "appointments");

            migrationBuilder.DropCheckConstraint(
                name: "ck_slot_time_range",
                table: "appointment_slots");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_SlotId",
                table: "appointments",
                column: "SlotId",
                unique: true);
        }
    }
}
