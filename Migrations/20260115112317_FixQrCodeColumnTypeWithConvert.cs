using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultX_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixQrCodeColumnTypeWithConvert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create a temporary column with varbinary(max)
            migrationBuilder.AddColumn<byte[]>(
                name: "qrCode_temp",
                table: "guests",
                type: "varbinary(max)",
                nullable: true);

            // Step 2: Copy data with explicit conversion
            migrationBuilder.Sql(
                @"UPDATE [guests]
          SET qrCode_temp = CONVERT(varbinary(max), qrCode)");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "qrCode",
                table: "guests");

            // Step 4: Rename temp column to original name
            migrationBuilder.RenameColumn(
                name: "qrCode_temp",
                table: "guests",
                newName: "qrCode");

            // Step 5: Make it NOT NULL if your model requires it
            // (optional – only if you want to enforce NOT NULL)
            migrationBuilder.AlterColumn<byte[]>(
                name: "qrCode",
                table: "guests",
                type: "varbinary(max)",
                nullable: false,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "qrCode",
                table: "guests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)");
        }
    }
}
