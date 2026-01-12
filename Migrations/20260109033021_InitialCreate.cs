using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaultX_WebAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    userid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    firstname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    lastname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    cnic = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    isVerified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    isBlocked = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    role = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false, defaultValue: "resident"),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    isEmailVerified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_37b098e31baedfa2b76e7876998", x => x.userid);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    internalRole = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    department = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    shift = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    joiningDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    userid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_b9535a98350d5b26e7eb0c26af4", x => x.id);
                    table.ForeignKey(
                        name: "FK_19fc098e857550a576b6b161125",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "otp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    code = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: false),
                    expiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    isUsed = table.Column<bool>(type: "bit", nullable: false),
                    userUserid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_32556d9d7b22031d7d0e1fd6723", x => x.id);
                    table.ForeignKey(
                        name: "FK_a4d3108840413c6e1ccce8ca436",
                        column: x => x.userUserid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "residences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newsequentialid())"),
                    addressLine1 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    addressLine2 = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    residenceType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "owned"),
                    residence = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "flat"),
                    isPrimary = table.Column<bool>(type: "bit", nullable: false),
                    isApprovedBySociety = table.Column<bool>(type: "bit", nullable: false),
                    approvedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    flatNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    block = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    userid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    approvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    updatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_505bad416f6552d9481a82385bb", x => x.id);
                    table.ForeignKey(
                        name: "FK_d3685ad68ed3fa2fbb49d136990",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "societies",
                columns: table => new
                {
                    society_id = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    city = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    state = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    postalCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    user_id = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_54d022c07968203bbc2a1ccc9d8", x => x.society_id);
                    table.ForeignKey(
                        name: "FK_3450f3eceecb321183417e8227a",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "userid");
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                columns: table => new
                {
                    vehicleId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicleType = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicleModel = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicleName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicleLicensePlateNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    vehicleRFIDTagId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    isGuest = table.Column<bool>(type: "bit", nullable: false),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    residentid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    vehicleColor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cc2bbdf57cb1356341edef83d44", x => x.vehicleId);
                    table.ForeignKey(
                        name: "FK_768cdb766dfb621e783856f55ee",
                        column: x => x.residentid,
                        principalTable: "residences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "guests",
                columns: table => new
                {
                    guestId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    guestName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    guestPhoneNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    eta = table.Column<DateTime>(type: "datetime", nullable: false),
                    CheckoutTime = table.Column<DateTime>(type: "datetime", nullable: false),
                    ActualArrivalTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    visitCompleted = table.Column<bool>(type: "bit", nullable: false),
                    isVerified = table.Column<bool>(type: "bit", nullable: false),
                    qrCode = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    userid = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    residenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    vehicleId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    createdAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    updatedAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_a6145db6b105b373e1c1833a3ba", x => x.guestId);
                    table.ForeignKey(
                        name: "FK_guests_residences_residenceId",
                        column: x => x.residenceId,
                        principalTable: "residences",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_guests_users_userid",
                        column: x => x.userid,
                        principalTable: "users",
                        principalColumn: "userid");
                    table.ForeignKey(
                        name: "FK_guests_vehicles_vehicleId",
                        column: x => x.vehicleId,
                        principalTable: "vehicles",
                        principalColumn: "vehicleId");
                });

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
                name: "REL_19fc098e857550a576b6b16112",
                table: "employees",
                column: "userid",
                unique: true,
                filter: "([userid] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_guests_residenceId",
                table: "guests",
                column: "residenceId");

            migrationBuilder.CreateIndex(
                name: "IX_guests_userid",
                table: "guests",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "REL_cac7e9be74c70ea79eae4ac5c3",
                table: "guests",
                column: "vehicleId",
                unique: true,
                filter: "([vehicleId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IDX_844706d729b144ed62f482ac2b",
                table: "otp",
                column: "expiresAt");

            migrationBuilder.CreateIndex(
                name: "REL_a4d3108840413c6e1ccce8ca43",
                table: "otp",
                column: "userUserid",
                unique: true,
                filter: "([userUserid] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_residences_userid",
                table: "residences",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "REL_3450f3eceecb321183417e8227",
                table: "societies",
                column: "user_id",
                unique: true,
                filter: "([user_id] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "UQ_97672ac88f789774dd47f7c8be3",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_access_logs_timestamp",
                table: "vehicle_access_logs",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_access_logs_vehicleId",
                table: "vehicle_access_logs",
                column: "vehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_residentid",
                table: "vehicles",
                column: "residentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "guests");

            migrationBuilder.DropTable(
                name: "otp");

            migrationBuilder.DropTable(
                name: "societies");

            migrationBuilder.DropTable(
                name: "vehicle_access_logs");

            migrationBuilder.DropTable(
                name: "vehicles");

            migrationBuilder.DropTable(
                name: "residences");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
