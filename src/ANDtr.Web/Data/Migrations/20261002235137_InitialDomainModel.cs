using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ANDtr.Web.Data.Migrations
{
    public partial class InitialDomainModel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OrganisationId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomerOrganisations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerOrganisations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductCode = table.Column<string>(type: "TEXT", nullable: false),
                    ProductCodeName = table.Column<string>(type: "TEXT", nullable: false),
                    RegulationNumber = table.Column<string>(type: "TEXT", nullable: false),
                    WebAddress = table.Column<string>(type: "TEXT", nullable: false),
                    RecallNumber = table.Column<string>(type: "TEXT", nullable: false),
                    ProductDescription = table.Column<string>(type: "TEXT", nullable: false),
                    TradeName = table.Column<string>(type: "TEXT", nullable: true),
                    RecallClass = table.Column<int>(type: "INTEGER", nullable: false),
                    CenterClassificationDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PostedInternetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    FeiNumber = table.Column<string>(type: "TEXT", nullable: true),
                    FirmName = table.Column<string>(type: "TEXT", nullable: false),
                    ManufacturerRecallReason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", nullable: false),
                    OrganisationId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SharedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskReviews_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskReviews_CustomerOrganisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "CustomerOrganisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    RiskReviewId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_RiskReviews_RiskReviewId",
                        column: x => x.RiskReviewId,
                        principalTable: "RiskReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RiskReviewId = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notifications_RiskReviews_RiskReviewId",
                        column: x => x.RiskReviewId,
                        principalTable: "RiskReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReviewNotices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RiskReviewId = table.Column<int>(type: "INTEGER", nullable: false),
                    NoticeId = table.Column<int>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewNotices_Notices_NoticeId",
                        column: x => x.NoticeId,
                        principalTable: "Notices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReviewNotices_RiskReviews_RiskReviewId",
                        column: x => x.RiskReviewId,
                        principalTable: "RiskReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RiskReviewId = table.Column<int>(type: "INTEGER", nullable: false),
                    ReviewNoticeId = table.Column<int>(type: "INTEGER", nullable: true),
                    AuthorId = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsInternal = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_ReviewNotices_ReviewNoticeId",
                        column: x => x.ReviewNoticeId,
                        principalTable: "ReviewNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_RiskReviews_RiskReviewId",
                        column: x => x.RiskReviewId,
                        principalTable: "RiskReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganisationId",
                table: "AspNetUsers",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_RiskReviewId",
                table: "ActivityLogs",
                column: "RiskReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_AuthorId",
                table: "Comments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ReviewNoticeId",
                table: "Comments",
                column: "ReviewNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_RiskReviewId",
                table: "Comments",
                column: "RiskReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_RecallNumber",
                table: "Notices",
                column: "RecallNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_RecallNumber_ProductCode",
                table: "Notices",
                columns: new[] { "RecallNumber", "ProductCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RiskReviewId",
                table: "Notifications",
                column: "RiskReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewNotices_NoticeId",
                table: "ReviewNotices",
                column: "NoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewNotices_RiskReviewId_NoticeId",
                table: "ReviewNotices",
                columns: new[] { "RiskReviewId", "NoticeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskReviews_OrganisationId",
                table: "RiskReviews",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskReviews_OwnerId",
                table: "RiskReviews",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_CustomerOrganisations_OrganisationId",
                table: "AspNetUsers",
                column: "OrganisationId",
                principalTable: "CustomerOrganisations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_CustomerOrganisations_OrganisationId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReviewNotices");

            migrationBuilder.DropTable(
                name: "Notices");

            migrationBuilder.DropTable(
                name: "RiskReviews");

            migrationBuilder.DropTable(
                name: "CustomerOrganisations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_OrganisationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OrganisationId",
                table: "AspNetUsers");
        }
    }
}
