using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BE_Phygens.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeForPhysicsGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "numeasy",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "topic",
                table: "exammatrix");

            migrationBuilder.RenameColumn(
                name: "nummedium",
                table: "exammatrix",
                newName: "grade");

            migrationBuilder.RenameColumn(
                name: "numhard",
                table: "exammatrix",
                newName: "duration");

            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "question",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "matrixid",
                table: "exammatrix",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "createdby",
                table: "exammatrix",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "exammatrix",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "examname",
                table: "exammatrix",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "examtype",
                table: "exammatrix",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "isactive",
                table: "exammatrix",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "totalpoints",
                table: "exammatrix",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ExamMatrixMatrixId",
                table: "exam",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chapter",
                columns: table => new
                {
                    chapterid = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    chaptername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    grade = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    displayorder = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chapter", x => x.chapterid);
                });

            migrationBuilder.CreateTable(
                name: "ExamMatrixDetail",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exammatrixid = table.Column<int>(type: "integer", nullable: false),
                    chapterid = table.Column<int>(type: "integer", nullable: false),
                    questioncount = table.Column<int>(type: "integer", nullable: false),
                    difficultylevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamMatrixDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamMatrixDetail_chapter_chapterid",
                        column: x => x.chapterid,
                        principalTable: "chapter",
                        principalColumn: "chapterid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamMatrixDetail_exammatrix_exammatrixid",
                        column: x => x.exammatrixid,
                        principalTable: "exammatrix",
                        principalColumn: "matrixid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_question_ChapterId",
                table: "question",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_exam_ExamMatrixMatrixId",
                table: "exam",
                column: "ExamMatrixMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMatrixDetail_chapterid",
                table: "ExamMatrixDetail",
                column: "chapterid");

            migrationBuilder.CreateIndex(
                name: "IX_ExamMatrixDetail_exammatrixid",
                table: "ExamMatrixDetail",
                column: "exammatrixid");

            migrationBuilder.AddForeignKey(
                name: "FK_exam_exammatrix_ExamMatrixMatrixId",
                table: "exam",
                column: "ExamMatrixMatrixId",
                principalTable: "exammatrix",
                principalColumn: "matrixid");

            migrationBuilder.AddForeignKey(
                name: "FK_question_chapter_ChapterId",
                table: "question",
                column: "ChapterId",
                principalTable: "chapter",
                principalColumn: "chapterid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exam_exammatrix_ExamMatrixMatrixId",
                table: "exam");

            migrationBuilder.DropForeignKey(
                name: "FK_question_chapter_ChapterId",
                table: "question");

            migrationBuilder.DropTable(
                name: "ExamMatrixDetail");

            migrationBuilder.DropTable(
                name: "chapter");

            migrationBuilder.DropIndex(
                name: "IX_question_ChapterId",
                table: "question");

            migrationBuilder.DropIndex(
                name: "IX_exam_ExamMatrixMatrixId",
                table: "exam");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "question");

            migrationBuilder.DropColumn(
                name: "createdby",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "description",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "examname",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "examtype",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "isactive",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "totalpoints",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "ExamMatrixMatrixId",
                table: "exam");

            migrationBuilder.RenameColumn(
                name: "grade",
                table: "exammatrix",
                newName: "nummedium");

            migrationBuilder.RenameColumn(
                name: "duration",
                table: "exammatrix",
                newName: "numhard");

            migrationBuilder.AlterColumn<string>(
                name: "matrixid",
                table: "exammatrix",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "numeasy",
                table: "exammatrix",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "subject",
                table: "exammatrix",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "topic",
                table: "exammatrix",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
