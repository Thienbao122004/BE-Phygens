using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BE_Phygens.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exammatrix",
                columns: table => new
                {
                    matrixid = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "text", nullable: false),
                    numeasy = table.Column<int>(type: "integer", nullable: false),
                    nummedium = table.Column<int>(type: "integer", nullable: false),
                    numhard = table.Column<int>(type: "integer", nullable: false),
                    totalquestions = table.Column<int>(type: "integer", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exammatrix", x => x.matrixid);
                    table.CheckConstraint("CK_ExamMatrix_NumEasy", "numeasy >= 0");
                    table.CheckConstraint("CK_ExamMatrix_NumHard", "numhard >= 0");
                    table.CheckConstraint("CK_ExamMatrix_NumMedium", "nummedium >= 0");
                });

            migrationBuilder.CreateTable(
                name: "physicstopic",
                columns: table => new
                {
                    topicid = table.Column<string>(type: "text", nullable: false),
                    topicname = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    gradelevel = table.Column<string>(type: "text", nullable: false),
                    displayorder = table.Column<int>(type: "integer", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_physicstopic", x => x.topicid);
                    table.CheckConstraint("CK_PhysicsTopic_DisplayOrder", "displayorder > 0");
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    userid = table.Column<string>(type: "text", nullable: false),
                    username = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    fullname = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    passwordhash = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.userid);
                    table.CheckConstraint("CK_User_Role", "role IN ('teacher', 'student', 'admin')");
                });

            migrationBuilder.CreateTable(
                name: "exam",
                columns: table => new
                {
                    examid = table.Column<string>(type: "text", nullable: false),
                    examname = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    durationminutes = table.Column<int>(type: "integer", nullable: true),
                    examtype = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    ispublished = table.Column<bool>(type: "boolean", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exam", x => x.examid);
                    table.CheckConstraint("CK_Exam_DurationMinutes", "durationminutes > 0");
                    table.CheckConstraint("CK_Exam_ExamType", "examtype IN ('15p', '1tiet', 'cuoiky')");
                    table.ForeignKey(
                        name: "FK_exam_User_createdby",
                        column: x => x.createdby,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "learningprogress",
                columns: table => new
                {
                    progressid = table.Column<string>(type: "text", nullable: false),
                    userid = table.Column<string>(type: "text", nullable: false),
                    topicid = table.Column<string>(type: "text", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    avgscore = table.Column<decimal>(type: "numeric", nullable: false),
                    lastupdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learningprogress", x => x.progressid);
                    table.CheckConstraint("CK_LearningProgress_Attempts", "attempts >= 0");
                    table.CheckConstraint("CK_LearningProgress_AvgScore", "avgscore >= 0 AND avgscore <= 10");
                    table.ForeignKey(
                        name: "FK_learningprogress_User_userid",
                        column: x => x.userid,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_learningprogress_physicstopic_topicid",
                        column: x => x.topicid,
                        principalTable: "physicstopic",
                        principalColumn: "topicid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question",
                columns: table => new
                {
                    questionid = table.Column<string>(type: "text", nullable: false),
                    topicid = table.Column<string>(type: "text", nullable: false),
                    questiontext = table.Column<string>(type: "text", nullable: false),
                    questiontype = table.Column<string>(type: "text", nullable: false),
                    difficultylevel = table.Column<string>(type: "text", nullable: false),
                    imageurl = table.Column<string>(type: "text", nullable: true),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    isactive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question", x => x.questionid);
                    table.CheckConstraint("CK_Question_DifficultyLevel", "difficultylevel IN ('easy', 'medium', 'hard')");
                    table.CheckConstraint("CK_Question_QuestionType", "questiontype IN ('multiple_choice', 'true_false', 'essay')");
                    table.ForeignKey(
                        name: "FK_question_User_createdby",
                        column: x => x.createdby,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_question_physicstopic_topicid",
                        column: x => x.topicid,
                        principalTable: "physicstopic",
                        principalColumn: "topicid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studentattempt",
                columns: table => new
                {
                    attemptid = table.Column<string>(type: "text", nullable: false),
                    userid = table.Column<string>(type: "text", nullable: false),
                    examid = table.Column<string>(type: "text", nullable: false),
                    starttime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    endtime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    totalscore = table.Column<decimal>(type: "numeric", nullable: false),
                    maxscore = table.Column<decimal>(type: "numeric", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studentattempt", x => x.attemptid);
                    table.CheckConstraint("CK_StudentAttempt_MaxScore", "maxscore > 0 AND maxscore <= 10");
                    table.CheckConstraint("CK_StudentAttempt_Status", "status IN ('in_progress', 'completed', 'abandoned')");
                    table.CheckConstraint("CK_StudentAttempt_TotalScore", "totalscore >= 0 AND totalscore <= 10");
                    table.ForeignKey(
                        name: "FK_studentattempt_User_userid",
                        column: x => x.userid,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_studentattempt_exam_examid",
                        column: x => x.examid,
                        principalTable: "exam",
                        principalColumn: "examid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "answerchoice",
                columns: table => new
                {
                    choiceid = table.Column<string>(type: "text", nullable: false),
                    questionid = table.Column<string>(type: "text", nullable: false),
                    choicelabel = table.Column<string>(type: "text", nullable: false),
                    choicetext = table.Column<string>(type: "text", nullable: false),
                    iscorrect = table.Column<bool>(type: "boolean", nullable: false),
                    displayorder = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_answerchoice", x => x.choiceid);
                    table.CheckConstraint("CK_AnswerChoice_DisplayOrder", "displayorder > 0");
                    table.ForeignKey(
                        name: "FK_answerchoice_question_questionid",
                        column: x => x.questionid,
                        principalTable: "question",
                        principalColumn: "questionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "examquestion",
                columns: table => new
                {
                    examquestionid = table.Column<string>(type: "text", nullable: false),
                    examid = table.Column<string>(type: "text", nullable: false),
                    questionid = table.Column<string>(type: "text", nullable: false),
                    questionorder = table.Column<int>(type: "integer", nullable: true),
                    pointsweight = table.Column<decimal>(type: "numeric", nullable: false),
                    addedat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_examquestion", x => x.examquestionid);
                    table.CheckConstraint("CK_ExamQuestion_PointsWeight", "pointsweight >= 0");
                    table.CheckConstraint("CK_ExamQuestion_QuestionOrder", "questionorder > 0");
                    table.ForeignKey(
                        name: "FK_examquestion_exam_examid",
                        column: x => x.examid,
                        principalTable: "exam",
                        principalColumn: "examid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_examquestion_question_questionid",
                        column: x => x.questionid,
                        principalTable: "question",
                        principalColumn: "questionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "explanations",
                columns: table => new
                {
                    explanationid = table.Column<string>(type: "text", nullable: false),
                    questionid = table.Column<string>(type: "text", nullable: false),
                    explanationtext = table.Column<string>(type: "text", nullable: false),
                    createdby = table.Column<string>(type: "text", nullable: false),
                    createdat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_explanations", x => x.explanationid);
                    table.ForeignKey(
                        name: "FK_explanations_User_createdby",
                        column: x => x.createdby,
                        principalTable: "User",
                        principalColumn: "userid",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_explanations_question_questionid",
                        column: x => x.questionid,
                        principalTable: "question",
                        principalColumn: "questionid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studentanswer",
                columns: table => new
                {
                    answerid = table.Column<string>(type: "text", nullable: false),
                    attemptid = table.Column<string>(type: "text", nullable: false),
                    questionid = table.Column<string>(type: "text", nullable: false),
                    selectedchoiceid = table.Column<string>(type: "text", nullable: true),
                    studenttextanswer = table.Column<string>(type: "text", nullable: true),
                    iscorrect = table.Column<bool>(type: "boolean", nullable: false),
                    pointsearned = table.Column<decimal>(type: "numeric", nullable: false),
                    answeredat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_studentanswer", x => x.answerid);
                    table.CheckConstraint("CK_StudentAnswer_PointsEarned", "pointsearned >= 0");
                    table.ForeignKey(
                        name: "FK_studentanswer_answerchoice_selectedchoiceid",
                        column: x => x.selectedchoiceid,
                        principalTable: "answerchoice",
                        principalColumn: "choiceid",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_studentanswer_question_questionid",
                        column: x => x.questionid,
                        principalTable: "question",
                        principalColumn: "questionid",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_studentanswer_studentattempt_attemptid",
                        column: x => x.attemptid,
                        principalTable: "studentattempt",
                        principalColumn: "attemptid",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_answerchoice_questionid",
                table: "answerchoice",
                column: "questionid");

            migrationBuilder.CreateIndex(
                name: "IX_exam_createdby",
                table: "exam",
                column: "createdby");

            migrationBuilder.CreateIndex(
                name: "IX_examquestion_examid",
                table: "examquestion",
                column: "examid");

            migrationBuilder.CreateIndex(
                name: "IX_examquestion_questionid",
                table: "examquestion",
                column: "questionid");

            migrationBuilder.CreateIndex(
                name: "IX_explanations_createdby",
                table: "explanations",
                column: "createdby");

            migrationBuilder.CreateIndex(
                name: "IX_explanations_questionid",
                table: "explanations",
                column: "questionid");

            migrationBuilder.CreateIndex(
                name: "IX_learningprogress_topicid",
                table: "learningprogress",
                column: "topicid");

            migrationBuilder.CreateIndex(
                name: "IX_learningprogress_userid",
                table: "learningprogress",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_physicstopic_topicname",
                table: "physicstopic",
                column: "topicname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_createdby",
                table: "question",
                column: "createdby");

            migrationBuilder.CreateIndex(
                name: "IX_question_topicid",
                table: "question",
                column: "topicid");

            migrationBuilder.CreateIndex(
                name: "IX_studentanswer_attemptid",
                table: "studentanswer",
                column: "attemptid");

            migrationBuilder.CreateIndex(
                name: "IX_studentanswer_questionid",
                table: "studentanswer",
                column: "questionid");

            migrationBuilder.CreateIndex(
                name: "IX_studentanswer_selectedchoiceid",
                table: "studentanswer",
                column: "selectedchoiceid");

            migrationBuilder.CreateIndex(
                name: "IX_studentattempt_examid",
                table: "studentattempt",
                column: "examid");

            migrationBuilder.CreateIndex(
                name: "IX_studentattempt_userid",
                table: "studentattempt",
                column: "userid");

            migrationBuilder.CreateIndex(
                name: "IX_User_email",
                table: "User",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_username",
                table: "User",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exammatrix");

            migrationBuilder.DropTable(
                name: "examquestion");

            migrationBuilder.DropTable(
                name: "explanations");

            migrationBuilder.DropTable(
                name: "learningprogress");

            migrationBuilder.DropTable(
                name: "studentanswer");

            migrationBuilder.DropTable(
                name: "answerchoice");

            migrationBuilder.DropTable(
                name: "studentattempt");

            migrationBuilder.DropTable(
                name: "question");

            migrationBuilder.DropTable(
                name: "exam");

            migrationBuilder.DropTable(
                name: "physicstopic");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
