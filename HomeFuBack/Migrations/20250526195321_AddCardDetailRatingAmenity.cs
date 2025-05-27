using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeFuBack.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDetailRatingAmenity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardDetailId",
                table: "Cards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Amenities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IconUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Amenities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumberOfGuests = table.Column<int>(type: "int", nullable: false),
                    NumberOfBedrooms = table.Column<int>(type: "int", nullable: false),
                    NumberOfBeds = table.Column<int>(type: "int", nullable: false),
                    NumberOfBathrooms = table.Column<int>(type: "int", nullable: false),
                    HostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RatingId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardDetails_Users_HostId",
                        column: x => x.HostId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CardDetailAmenities",
                columns: table => new
                {
                    CardDetailId = table.Column<int>(type: "int", nullable: false),
                    AmenityId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardDetailAmenities", x => new { x.CardDetailId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK_CardDetailAmenities_Amenities_AmenityId",
                        column: x => x.AmenityId,
                        principalTable: "Amenities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardDetailAmenities_CardDetails_CardDetailId",
                        column: x => x.CardDetailId,
                        principalTable: "CardDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CardDetailId = table.Column<int>(type: "int", nullable: false),
                    Cleanliness = table.Column<double>(type: "float", nullable: false),
                    Accuracy = table.Column<double>(type: "float", nullable: false),
                    CheckIn = table.Column<double>(type: "float", nullable: false),
                    Communication = table.Column<double>(type: "float", nullable: false),
                    Location = table.Column<double>(type: "float", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_CardDetails_CardDetailId",
                        column: x => x.CardDetailId,
                        principalTable: "CardDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardDetailId",
                table: "Cards",
                column: "CardDetailId",
                unique: true,
                filter: "[CardDetailId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CardDetailAmenities_AmenityId",
                table: "CardDetailAmenities",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_CardDetails_HostId",
                table: "CardDetails",
                column: "HostId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_CardDetailId",
                table: "Ratings",
                column: "CardDetailId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardDetails_CardDetailId",
                table: "Cards",
                column: "CardDetailId",
                principalTable: "CardDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardDetails_CardDetailId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "CardDetailAmenities");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "Amenities");

            migrationBuilder.DropTable(
                name: "CardDetails");

            migrationBuilder.DropIndex(
                name: "IX_Cards_CardDetailId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardDetailId",
                table: "Cards");
        }
    }
}
