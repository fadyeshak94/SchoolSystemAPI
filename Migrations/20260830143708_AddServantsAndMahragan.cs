using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddServantsAndMahragan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MahraganEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    ThemeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MahraganEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServantAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServantId = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeetingType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServantAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServantAttendances_TarbeyaFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "TarbeyaFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServantAttendances_Users_ServantId",
                        column: x => x.ServantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToServantId = table.Column<int>(type: "int", nullable: false),
                    AssignedByAdminId = table.Column<int>(type: "int", nullable: false),
                    FamilyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceTasks_TarbeyaFamilies_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "TarbeyaFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTasks_Users_AssignedByAdminId",
                        column: x => x.AssignedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceTasks_Users_AssignedToServantId",
                        column: x => x.AssignedToServantId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MahraganCompetitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TargetStageId = table.Column<int>(type: "int", nullable: false),
                    PassingScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MahraganCompetitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MahraganCompetitions_MahraganEvents_EventId",
                        column: x => x.EventId,
                        principalTable: "MahraganEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MahraganCompetitions_TarbeyaStages_TargetStageId",
                        column: x => x.TargetStageId,
                        principalTable: "TarbeyaStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MahraganEnrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CompetitionId = table.Column<int>(type: "int", nullable: false),
                    BarcodeString = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MahraganEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MahraganEnrollments_MahraganCompetitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "MahraganCompetitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MahraganEnrollments_TarbeyaStudents_StudentId",
                        column: x => x.StudentId,
                        principalTable: "TarbeyaStudents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MahraganScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrollmentId = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsQualified = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MahraganScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MahraganScores_MahraganEnrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "MahraganEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MahraganCompetitions_EventId",
                table: "MahraganCompetitions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_MahraganCompetitions_TargetStageId",
                table: "MahraganCompetitions",
                column: "TargetStageId");

            migrationBuilder.CreateIndex(
                name: "IX_MahraganEnrollments_CompetitionId",
                table: "MahraganEnrollments",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_MahraganEnrollments_StudentId",
                table: "MahraganEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_MahraganScores_EnrollmentId",
                table: "MahraganScores",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ServantAttendances_FamilyId",
                table: "ServantAttendances",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_ServantAttendances_ServantId",
                table: "ServantAttendances",
                column: "ServantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTasks_AssignedByAdminId",
                table: "ServiceTasks",
                column: "AssignedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTasks_AssignedToServantId",
                table: "ServiceTasks",
                column: "AssignedToServantId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTasks_FamilyId",
                table: "ServiceTasks",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MahraganScores");

            migrationBuilder.DropTable(
                name: "ServantAttendances");

            migrationBuilder.DropTable(
                name: "ServiceTasks");

            migrationBuilder.DropTable(
                name: "MahraganEnrollments");

            migrationBuilder.DropTable(
                name: "MahraganCompetitions");

            migrationBuilder.DropTable(
                name: "MahraganEvents");
        }
    }
}
