using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BE_Phygens.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAnswerChoiceDisplayOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExamMatrixDetail_chapter_chapterid",
                table: "ExamMatrixDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ExamMatrixDetail_exammatrix_exammatrixid",
                table: "ExamMatrixDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_question_chapter_ChapterId",
                table: "question");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExamMatrixDetail",
                table: "ExamMatrixDetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Exam_ExamType",
                table: "exam");

            migrationBuilder.DropColumn(
                name: "createdby",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "description",
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

            migrationBuilder.RenameTable(
                name: "ExamMatrixDetail",
                newName: "exammatrixdetail");

            migrationBuilder.RenameColumn(
                name: "ChapterId",
                table: "question",
                newName: "chapterid");

            migrationBuilder.RenameIndex(
                name: "IX_question_ChapterId",
                table: "question",
                newName: "IX_question_chapterid");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "exammatrixdetail",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_ExamMatrixDetail_exammatrixid",
                table: "exammatrixdetail",
                newName: "IX_exammatrixdetail_exammatrixid");

            migrationBuilder.RenameIndex(
                name: "IX_ExamMatrixDetail_chapterid",
                table: "exammatrixdetail",
                newName: "IX_exammatrixdetail_chapterid");

            migrationBuilder.RenameColumn(
                name: "grade",
                table: "exammatrix",
                newName: "nummedium");

            migrationBuilder.RenameColumn(
                name: "duration",
                table: "exammatrix",
                newName: "numhard");

            migrationBuilder.AddColumn<bool>(
                name: "aigenerated",
                table: "question",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "aigenerationmetadata",
                table: "question",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aimodel",
                table: "question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aipromptused",
                table: "question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aiprovider",
                table: "question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aivalidationstatus",
                table: "question",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "explanation",
                table: "question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "qualityscore",
                table: "question",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "specifictopic",
                table: "question",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string[]>(
                name: "tags",
                table: "question",
                type: "text[]",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "exammatrixid",
                table: "exammatrixdetail",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "examname",
                table: "exammatrix",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

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

            migrationBuilder.AlterColumn<string>(
                name: "ExamMatrixMatrixId",
                table: "exam",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "adaptivedifficulty",
                table: "exam",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "aigenerationconfig",
                table: "exam",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "autogradingenabled",
                table: "exam",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "isaigenerated",
                table: "exam",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "displayorder",
                table: "answerchoice",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_exammatrixdetail",
                table: "exammatrixdetail",
                column: "id");

            migrationBuilder.CreateTable(
                name: "adaptivelearningdata",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<string>(type: "text", nullable: false),
                    chapterid = table.Column<int>(type: "integer", nullable: false),
                    difficultypreference = table.Column<string>(type: "text", nullable: false),
                    weaktopics = table.Column<string[]>(type: "text[]", nullable: true),
                    strongtopics = table.Column<string[]>(type: "text[]", nullable: true),
                    recommendeddifficulty = table.Column<string>(type: "text", nullable: true),
                    performancetrend = table.Column<string>(type: "text", nullable: true),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_adaptivelearningdata", x => x.id);
                    table.ForeignKey(
                        name: "FK_adaptivelearningdata_User_userid",
                        column: x => x.userid,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_adaptivelearningdata_chapter_chapterid",
                        column: x => x.chapterid,
                        principalTable: "chapter",
                        principalColumn: "chapterid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aigenerationhistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sessionid = table.Column<string>(type: "text", nullable: false),
                    userid = table.Column<string>(type: "text", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    modelname = table.Column<string>(type: "text", nullable: false),
                    prompt = table.Column<string>(type: "text", nullable: false),
                    response = table.Column<string>(type: "text", nullable: true),
                    tokensused = table.Column<int>(type: "integer", nullable: true),
                    generationtimems = table.Column<int>(type: "integer", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    errormessage = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aigenerationhistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_aigenerationhistory_User_userid",
                        column: x => x.userid,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "aimodelconfigs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(type: "text", nullable: false),
                    modelname = table.Column<string>(type: "text", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    maxtokens = table.Column<int>(type: "integer", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric", nullable: false),
                    costper1ktokens = table.Column<decimal>(type: "numeric", nullable: true),
                    ratelimitperminute = table.Column<int>(type: "integer", nullable: false),
                    qualityrating = table.Column<decimal>(type: "numeric", nullable: true),
                    specialties = table.Column<string[]>(type: "text[]", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aimodelconfigs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "aiusagestats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    totalrequests = table.Column<int>(type: "integer", nullable: false),
                    successfulrequests = table.Column<int>(type: "integer", nullable: false),
                    failedrequests = table.Column<int>(type: "integer", nullable: false),
                    totaltokens = table.Column<int>(type: "integer", nullable: false),
                    totalcost = table.Column<decimal>(type: "numeric", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aiusagestats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questionqualityfeedback",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    questionid = table.Column<string>(type: "text", nullable: false),
                    userid = table.Column<string>(type: "text", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    feedbacktext = table.Column<string>(type: "text", nullable: true),
                    feedbacktype = table.Column<string>(type: "text", nullable: true),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questionqualityfeedback", x => x.id);
                    table.CheckConstraint("CK_QuestionQualityFeedback_FeedbackType", "feedbacktype IN ('quality', 'difficulty', 'clarity', 'accuracy')");
                    table.CheckConstraint("CK_QuestionQualityFeedback_Rating", "rating >= 1 AND rating <= 5");
                    table.ForeignKey(
                        name: "FK_questionqualityfeedback_User_userid",
                        column: x => x.userid,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_questionqualityfeedback_question_questionid",
                        column: x => x.questionid,
                        principalTable: "question",
                        principalColumn: "questionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "smartexamtemplates",
                columns: table => new
                {
                    templateid = table.Column<string>(type: "text", nullable: false),
                    templatename = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    targetgrade = table.Column<int>(type: "integer", nullable: false),
                    examtype = table.Column<string>(type: "text", nullable: false),
                    difficultydistribution = table.Column<string>(type: "text", nullable: true),
                    chapterweights = table.Column<string>(type: "text", nullable: true),
                    totalquestions = table.Column<int>(type: "integer", nullable: false),
                    durationminutes = table.Column<int>(type: "integer", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_smartexamtemplates", x => x.templateid);
                    table.ForeignKey(
                        name: "FK_smartexamtemplates_User_createdby",
                        column: x => x.createdby,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "User",
                sql: "role IN ('student', 'admin')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Question_AiValidationStatus",
                table: "question",
                sql: "aivalidationstatus IN ('pending', 'validated', 'needsReview', 'rejected')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Question_QualityScore",
                table: "question",
                sql: "qualityscore >= 0 AND qualityscore <= 10");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExamMatrixDetail_DifficultyLevel",
                table: "exammatrixdetail",
                sql: "difficultylevel IN ('easy', 'medium', 'hard')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExamMatrixDetail_QuestionCount",
                table: "exammatrixdetail",
                sql: "questioncount >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Exam_ExamType",
                table: "exam",
                sql: "examtype IN ('15p', '1tiet', 'cuoiky', 'ai_generated', 'smart_exam', 'adaptive')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chapter_DisplayOrder",
                table: "chapter",
                sql: "displayorder > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Chapter_Grade",
                table: "chapter",
                sql: "grade IN (10, 11, 12)");

            migrationBuilder.CreateIndex(
                name: "IX_adaptivelearningdata_chapterid",
                table: "adaptivelearningdata",
                column: "chapterid");

            migrationBuilder.CreateIndex(
                name: "IX_adaptivelearningdata_userid_chapterid",
                table: "adaptivelearningdata",
                columns: new[] { "userid", "chapterid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aigenerationhistory_userid",
                table: "aigenerationhistory",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_aimodelconfigs_provider_modelname",
                table: "aimodelconfigs",
                columns: new[] { "provider", "modelname" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_aiusagestats_date_provider",
                table: "aiusagestats",
                columns: new[] { "date", "provider" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questionqualityfeedback_questionid",
                table: "questionqualityfeedback",
                column: "questionid");

            migrationBuilder.CreateIndex(
                name: "IX_questionqualityfeedback_userid",
                table: "questionqualityfeedback",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_smartexamtemplates_createdby",
                table: "smartexamtemplates",
                column: "createdby");

            migrationBuilder.AddForeignKey(
                name: "FK_exammatrixdetail_chapter_chapterid",
                table: "exammatrixdetail",
                column: "chapterid",
                principalTable: "chapter",
                principalColumn: "chapterid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_exammatrixdetail_exammatrix_exammatrixid",
                table: "exammatrixdetail",
                column: "exammatrixid",
                principalTable: "exammatrix",
                principalColumn: "matrixid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_question_chapter_chapterid",
                table: "question",
                column: "chapterid",
                principalTable: "chapter",
                principalColumn: "chapterid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_exammatrixdetail_chapter_chapterid",
                table: "exammatrixdetail");

            migrationBuilder.DropForeignKey(
                name: "FK_exammatrixdetail_exammatrix_exammatrixid",
                table: "exammatrixdetail");

            migrationBuilder.DropForeignKey(
                name: "FK_question_chapter_chapterid",
                table: "question");

            migrationBuilder.DropTable(
                name: "adaptivelearningdata");

            migrationBuilder.DropTable(
                name: "aigenerationhistory");

            migrationBuilder.DropTable(
                name: "aimodelconfigs");

            migrationBuilder.DropTable(
                name: "aiusagestats");

            migrationBuilder.DropTable(
                name: "questionqualityfeedback");

            migrationBuilder.DropTable(
                name: "smartexamtemplates");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "User");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Question_AiValidationStatus",
                table: "question");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Question_QualityScore",
                table: "question");

            migrationBuilder.DropPrimaryKey(
                name: "PK_exammatrixdetail",
                table: "exammatrixdetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExamMatrixDetail_DifficultyLevel",
                table: "exammatrixdetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExamMatrixDetail_QuestionCount",
                table: "exammatrixdetail");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Exam_ExamType",
                table: "exam");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chapter_DisplayOrder",
                table: "chapter");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Chapter_Grade",
                table: "chapter");

            migrationBuilder.DropColumn(
                name: "aigenerated",
                table: "question");

            migrationBuilder.DropColumn(
                name: "aigenerationmetadata",
                table: "question");

            migrationBuilder.DropColumn(
                name: "aimodel",
                table: "question");

            migrationBuilder.DropColumn(
                name: "aipromptused",
                table: "question");

            migrationBuilder.DropColumn(
                name: "aiprovider",
                table: "question");

            migrationBuilder.DropColumn(
                name: "aivalidationstatus",
                table: "question");

            migrationBuilder.DropColumn(
                name: "explanation",
                table: "question");

            migrationBuilder.DropColumn(
                name: "qualityscore",
                table: "question");

            migrationBuilder.DropColumn(
                name: "specifictopic",
                table: "question");

            migrationBuilder.DropColumn(
                name: "tags",
                table: "question");

            migrationBuilder.DropColumn(
                name: "numeasy",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "subject",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "topic",
                table: "exammatrix");

            migrationBuilder.DropColumn(
                name: "adaptivedifficulty",
                table: "exam");

            migrationBuilder.DropColumn(
                name: "aigenerationconfig",
                table: "exam");

            migrationBuilder.DropColumn(
                name: "autogradingenabled",
                table: "exam");

            migrationBuilder.DropColumn(
                name: "isaigenerated",
                table: "exam");

            migrationBuilder.RenameTable(
                name: "exammatrixdetail",
                newName: "ExamMatrixDetail");

            migrationBuilder.RenameColumn(
                name: "chapterid",
                table: "question",
                newName: "ChapterId");

            migrationBuilder.RenameIndex(
                name: "IX_question_chapterid",
                table: "question",
                newName: "IX_question_ChapterId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ExamMatrixDetail",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_exammatrixdetail_exammatrixid",
                table: "ExamMatrixDetail",
                newName: "IX_ExamMatrixDetail_exammatrixid");

            migrationBuilder.RenameIndex(
                name: "IX_exammatrixdetail_chapterid",
                table: "ExamMatrixDetail",
                newName: "IX_ExamMatrixDetail_chapterid");

            migrationBuilder.RenameColumn(
                name: "nummedium",
                table: "exammatrix",
                newName: "grade");

            migrationBuilder.RenameColumn(
                name: "numhard",
                table: "exammatrix",
                newName: "duration");

            migrationBuilder.AlterColumn<int>(
                name: "exammatrixid",
                table: "ExamMatrixDetail",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "examname",
                table: "exammatrix",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

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

            migrationBuilder.AlterColumn<int>(
                name: "ExamMatrixMatrixId",
                table: "exam",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "displayorder",
                table: "answerchoice",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExamMatrixDetail",
                table: "ExamMatrixDetail",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "User",
                sql: "role IN ( 'student', 'admin')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Exam_ExamType",
                table: "exam",
                sql: "examtype IN ('15p', '1tiet', 'cuoiky')");

            migrationBuilder.AddForeignKey(
                name: "FK_ExamMatrixDetail_chapter_chapterid",
                table: "ExamMatrixDetail",
                column: "chapterid",
                principalTable: "chapter",
                principalColumn: "chapterid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExamMatrixDetail_exammatrix_exammatrixid",
                table: "ExamMatrixDetail",
                column: "exammatrixid",
                principalTable: "exammatrix",
                principalColumn: "matrixid",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_question_chapter_ChapterId",
                table: "question",
                column: "ChapterId",
                principalTable: "chapter",
                principalColumn: "chapterid");
        }
    }
}
