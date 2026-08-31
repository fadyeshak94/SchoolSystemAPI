using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddTarbeyaTrips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TarbeyaPointTransactions_Users_ServantId",
                table: "TarbeyaPointTransactions");

            migrationBuilder.CreateTable(
                name: "TarbeyaTrips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TripDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaTrips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaTrips_TarbeyaFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "TarbeyaFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaTripExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    ItemDescription = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedByFamilyAdminId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaTripExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaTripExpenses_TarbeyaTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "TarbeyaTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TarbeyaTripExpenses_Users_AddedByFamilyAdminId",
                        column: x => x.AddedByFamilyAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TarbeyaTripSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ServantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TarbeyaTripSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TarbeyaTripSubscriptions_TarbeyaStudents_StudentId",
                        column: x => x.StudentId,
                        principalTable: "TarbeyaStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TarbeyaTripSubscriptions_TarbeyaTrips_TripId",
                        column: x => x.TripId,
                        principalTable: "TarbeyaTrips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TarbeyaTripSubscriptions_Users_ServantId",
                        column: x => x.ServantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTripExpenses_AddedByFamilyAdminId",
                table: "TarbeyaTripExpenses",
                column: "AddedByFamilyAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTripExpenses_TripId",
                table: "TarbeyaTripExpenses",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTrips_FamilyId",
                table: "TarbeyaTrips",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTripSubscriptions_ServantId",
                table: "TarbeyaTripSubscriptions",
                column: "ServantId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTripSubscriptions_StudentId",
                table: "TarbeyaTripSubscriptions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TarbeyaTripSubscriptions_TripId",
                table: "TarbeyaTripSubscriptions",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_TarbeyaPointTransactions_Users_ServantId",
                table: "TarbeyaPointTransactions",
                column: "ServantId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TarbeyaPointTransactions_Users_ServantId",
                table: "TarbeyaPointTransactions");

            migrationBuilder.DropTable(
                name: "TarbeyaTripExpenses");

            migrationBuilder.DropTable(
                name: "TarbeyaTripSubscriptions");

            migrationBuilder.DropTable(
                name: "TarbeyaTrips");

            migrationBuilder.AddForeignKey(
                name: "FK_TarbeyaPointTransactions_Users_ServantId",
                table: "TarbeyaPointTransactions",
                column: "ServantId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
