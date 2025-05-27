using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HomeFuBack.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmenity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IconUrl",
                table: "Amenities",
                newName: "IconPath");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IconPath",
                table: "Amenities",
                newName: "IconUrl");
        }
    }
}
