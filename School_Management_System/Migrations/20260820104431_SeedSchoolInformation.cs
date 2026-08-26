using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace School_Management_System.Migrations
{
    /// <inheritdoc />
    public partial class SeedSchoolInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "SchoolInfos",
                columns: new[] { "SchoolInfoId", "Address", "Email", "PhoneNumber", "SchoolName", "WebsiteUrl" },
                values: new object[] { 1, "52 Mission Rd", "sisterjoan's@school.com", "+27-10-123-4567", "Sister Joan's", "https://www.sisterjoan's.com" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SchoolInfos",
                keyColumn: "SchoolInfoId",
                keyValue: 1);
        }
    }
}
