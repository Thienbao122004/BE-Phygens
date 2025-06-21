CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE exammatrix (
        matrixid text NOT NULL,
        subject text NOT NULL,
        topic text NOT NULL,
        numeasy integer NOT NULL,
        nummedium integer NOT NULL,
        numhard integer NOT NULL,
        totalquestions integer NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_exammatrix" PRIMARY KEY (matrixid),
        CONSTRAINT "CK_ExamMatrix_NumEasy" CHECK (numeasy >= 0),
        CONSTRAINT "CK_ExamMatrix_NumHard" CHECK (numhard >= 0),
        CONSTRAINT "CK_ExamMatrix_NumMedium" CHECK (nummedium >= 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE physicstopic (
        topicid text NOT NULL,
        topicname text NOT NULL,
        description text,
        gradelevel text NOT NULL,
        displayorder integer NOT NULL,
        isactive boolean NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_physicstopic" PRIMARY KEY (topicid),
        CONSTRAINT "CK_PhysicsTopic_DisplayOrder" CHECK (displayorder > 0)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE "User" (
        userid text NOT NULL,
        username text NOT NULL,
        email text NOT NULL,
        fullname text NOT NULL,
        role text NOT NULL,
        passwordhash text NOT NULL,
        createdat timestamp with time zone NOT NULL,
        isactive boolean NOT NULL,
        CONSTRAINT "PK_User" PRIMARY KEY (userid),
        CONSTRAINT "CK_User_Role" CHECK (role IN ('teacher', 'student', 'admin'))
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE exam (
        examid text NOT NULL,
        examname text NOT NULL,
        description text,
        durationminutes integer,
        examtype text NOT NULL,
        createdby text NOT NULL,
        ispublished boolean NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_exam" PRIMARY KEY (examid),
        CONSTRAINT "CK_Exam_DurationMinutes" CHECK (durationminutes > 0),
        CONSTRAINT "CK_Exam_ExamType" CHECK (examtype IN ('15p', '1tiet', 'cuoiky')),
        CONSTRAINT "FK_exam_User_createdby" FOREIGN KEY (createdby) REFERENCES "User" (userid) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE learningprogress (
        progressid text NOT NULL,
        userid text NOT NULL,
        topicid text NOT NULL,
        attempts integer NOT NULL,
        avgscore numeric NOT NULL,
        lastupdated timestamp with time zone NOT NULL,
        CONSTRAINT "PK_learningprogress" PRIMARY KEY (progressid),
        CONSTRAINT "CK_LearningProgress_Attempts" CHECK (attempts >= 0),
        CONSTRAINT "CK_LearningProgress_AvgScore" CHECK (avgscore >= 0 AND avgscore <= 10),
        CONSTRAINT "FK_learningprogress_User_userid" FOREIGN KEY (userid) REFERENCES "User" (userid) ON DELETE CASCADE,
        CONSTRAINT "FK_learningprogress_physicstopic_topicid" FOREIGN KEY (topicid) REFERENCES physicstopic (topicid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE question (
        questionid text NOT NULL,
        topicid text NOT NULL,
        questiontext text NOT NULL,
        questiontype text NOT NULL,
        difficultylevel text NOT NULL,
        imageurl text,
        createdby text NOT NULL,
        createdat timestamp with time zone NOT NULL,
        isactive boolean NOT NULL,
        CONSTRAINT "PK_question" PRIMARY KEY (questionid),
        CONSTRAINT "CK_Question_DifficultyLevel" CHECK (difficultylevel IN ('easy', 'medium', 'hard')),
        CONSTRAINT "CK_Question_QuestionType" CHECK (questiontype IN ('multiple_choice', 'true_false', 'essay')),
        CONSTRAINT "FK_question_User_createdby" FOREIGN KEY (createdby) REFERENCES "User" (userid) ON DELETE RESTRICT,
        CONSTRAINT "FK_question_physicstopic_topicid" FOREIGN KEY (topicid) REFERENCES physicstopic (topicid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE studentattempt (
        attemptid text NOT NULL,
        userid text NOT NULL,
        examid text NOT NULL,
        starttime timestamp with time zone NOT NULL,
        endtime timestamp with time zone,
        totalscore numeric NOT NULL,
        maxscore numeric,
        status text NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_studentattempt" PRIMARY KEY (attemptid),
        CONSTRAINT "CK_StudentAttempt_MaxScore" CHECK (maxscore > 0 AND maxscore <= 10),
        CONSTRAINT "CK_StudentAttempt_Status" CHECK (status IN ('in_progress', 'completed', 'abandoned')),
        CONSTRAINT "CK_StudentAttempt_TotalScore" CHECK (totalscore >= 0 AND totalscore <= 10),
        CONSTRAINT "FK_studentattempt_User_userid" FOREIGN KEY (userid) REFERENCES "User" (userid) ON DELETE CASCADE,
        CONSTRAINT "FK_studentattempt_exam_examid" FOREIGN KEY (examid) REFERENCES exam (examid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE answerchoice (
        choiceid text NOT NULL,
        questionid text NOT NULL,
        choicelabel text NOT NULL,
        choicetext text NOT NULL,
        iscorrect boolean NOT NULL,
        displayorder integer,
        CONSTRAINT "PK_answerchoice" PRIMARY KEY (choiceid),
        CONSTRAINT "CK_AnswerChoice_DisplayOrder" CHECK (displayorder > 0),
        CONSTRAINT "FK_answerchoice_question_questionid" FOREIGN KEY (questionid) REFERENCES question (questionid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE examquestion (
        examquestionid text NOT NULL,
        examid text NOT NULL,
        questionid text NOT NULL,
        questionorder integer,
        pointsweight numeric NOT NULL,
        addedat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_examquestion" PRIMARY KEY (examquestionid),
        CONSTRAINT "CK_ExamQuestion_PointsWeight" CHECK (pointsweight >= 0),
        CONSTRAINT "CK_ExamQuestion_QuestionOrder" CHECK (questionorder > 0),
        CONSTRAINT "FK_examquestion_exam_examid" FOREIGN KEY (examid) REFERENCES exam (examid) ON DELETE CASCADE,
        CONSTRAINT "FK_examquestion_question_questionid" FOREIGN KEY (questionid) REFERENCES question (questionid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE explanations (
        explanationid text NOT NULL,
        questionid text NOT NULL,
        explanationtext text NOT NULL,
        createdby text NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_explanations" PRIMARY KEY (explanationid),
        CONSTRAINT "FK_explanations_User_createdby" FOREIGN KEY (createdby) REFERENCES "User" (userid) ON DELETE RESTRICT,
        CONSTRAINT "FK_explanations_question_questionid" FOREIGN KEY (questionid) REFERENCES question (questionid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE TABLE studentanswer (
        answerid text NOT NULL,
        attemptid text NOT NULL,
        questionid text NOT NULL,
        selectedchoiceid text,
        studenttextanswer text,
        iscorrect boolean NOT NULL,
        pointsearned numeric NOT NULL,
        answeredat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_studentanswer" PRIMARY KEY (answerid),
        CONSTRAINT "CK_StudentAnswer_PointsEarned" CHECK (pointsearned >= 0),
        CONSTRAINT "FK_studentanswer_answerchoice_selectedchoiceid" FOREIGN KEY (selectedchoiceid) REFERENCES answerchoice (choiceid) ON DELETE SET NULL,
        CONSTRAINT "FK_studentanswer_question_questionid" FOREIGN KEY (questionid) REFERENCES question (questionid) ON DELETE CASCADE,
        CONSTRAINT "FK_studentanswer_studentattempt_attemptid" FOREIGN KEY (attemptid) REFERENCES studentattempt (attemptid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_answerchoice_questionid" ON answerchoice (questionid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_exam_createdby" ON exam (createdby);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_examquestion_examid" ON examquestion (examid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_examquestion_questionid" ON examquestion (questionid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_explanations_createdby" ON explanations (createdby);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_explanations_questionid" ON explanations (questionid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_learningprogress_topicid" ON learningprogress (topicid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_learningprogress_userid" ON learningprogress (userid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_physicstopic_topicname" ON physicstopic (topicname);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_question_createdby" ON question (createdby);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_question_topicid" ON question (topicid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_studentanswer_attemptid" ON studentanswer (attemptid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_studentanswer_questionid" ON studentanswer (questionid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_studentanswer_selectedchoiceid" ON studentanswer (selectedchoiceid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_studentattempt_examid" ON studentattempt (examid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE INDEX "IX_studentattempt_userid" ON studentattempt (userid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_User_email" ON "User" (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_User_username" ON "User" (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621082509_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250621082509_InitialCreate', '8.0.0');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix DROP COLUMN numeasy;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix DROP COLUMN subject;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix DROP COLUMN topic;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix RENAME COLUMN nummedium TO grade;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix RENAME COLUMN numhard TO duration;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE question ADD "ChapterId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ALTER COLUMN matrixid TYPE integer;
    ALTER TABLE exammatrix ALTER COLUMN matrixid DROP DEFAULT;
    ALTER TABLE exammatrix ALTER COLUMN matrixid ADD GENERATED BY DEFAULT AS IDENTITY;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD createdby character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD description character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD examname character varying(200) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD examtype character varying(50) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD isactive boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exammatrix ADD totalpoints numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exam ADD "ExamMatrixMatrixId" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE TABLE chapter (
        chapterid integer GENERATED BY DEFAULT AS IDENTITY,
        chaptername character varying(100) NOT NULL,
        grade integer NOT NULL,
        description character varying(500),
        displayorder integer NOT NULL,
        isactive boolean NOT NULL,
        createdat timestamp with time zone NOT NULL,
        CONSTRAINT "PK_chapter" PRIMARY KEY (chapterid)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE TABLE "ExamMatrixDetail" (
        "Id" integer GENERATED BY DEFAULT AS IDENTITY,
        exammatrixid integer NOT NULL,
        chapterid integer NOT NULL,
        questioncount integer NOT NULL,
        difficultylevel character varying(50) NOT NULL,
        CONSTRAINT "PK_ExamMatrixDetail" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ExamMatrixDetail_chapter_chapterid" FOREIGN KEY (chapterid) REFERENCES chapter (chapterid) ON DELETE CASCADE,
        CONSTRAINT "FK_ExamMatrixDetail_exammatrix_exammatrixid" FOREIGN KEY (exammatrixid) REFERENCES exammatrix (matrixid) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE INDEX "IX_question_ChapterId" ON question ("ChapterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE INDEX "IX_exam_ExamMatrixMatrixId" ON exam ("ExamMatrixMatrixId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE INDEX "IX_ExamMatrixDetail_chapterid" ON "ExamMatrixDetail" (chapterid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    CREATE INDEX "IX_ExamMatrixDetail_exammatrixid" ON "ExamMatrixDetail" (exammatrixid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE exam ADD CONSTRAINT "FK_exam_exammatrix_ExamMatrixMatrixId" FOREIGN KEY ("ExamMatrixMatrixId") REFERENCES exammatrix (matrixid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    ALTER TABLE question ADD CONSTRAINT "FK_question_chapter_ChapterId" FOREIGN KEY ("ChapterId") REFERENCES chapter (chapterid);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20250621131657_OptimizeForPhysicsGrades') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20250621131657_OptimizeForPhysicsGrades', '8.0.0');
    END IF;
END $EF$;
COMMIT;

