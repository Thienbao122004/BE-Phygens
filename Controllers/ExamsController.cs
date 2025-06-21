using Microsoft.AspNetCore.Mvc;
using BE_Phygens.Models;
using BE_Phygens.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamsController : ControllerBase
    {
        // Fake in-memory storage for demonstration
        private static List<ExamDto> Exams = new List<ExamDto>();
        private static int NextId = 1;

        // GET: api/exams
        [HttpGet]
        public IActionResult GetAllExams()
        {
            return Ok(Exams);
        }

        // GET: api/exams/{id}
        [HttpGet("{id}")]
        public IActionResult GetExamById(string id)
        {
            var exam = Exams.FirstOrDefault(e => e.ExamId == id);
            if (exam == null) return NotFound();
            return Ok(exam);
        }

        // POST: api/exams
        [HttpPost]
        public IActionResult CreateExam([FromBody] ExamCreateDto examDto)
        {
            var exam = new ExamDto
            {
                ExamId = NextId.ToString(),
                ExamName = examDto.ExamName,
                Description = examDto.Description,
                DurationMinutes = examDto.DurationMinutes,
                ExamType = examDto.ExamType,
                CreatedBy = examDto.CreatedBy,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                Questions = examDto.Questions?.Select(q => new ExamQuestionDto
                {
                    ExamQuestionId = Guid.NewGuid().ToString(),
                    QuestionId = q.QuestionId,
                    QuestionOrder = q.QuestionOrder,
                    PointsWeight = q.PointsWeight,
                    Question = null // Có thể map thêm nếu cần
                }).ToList() ?? new List<ExamQuestionDto>()
            };
            Exams.Add(exam);
            NextId++;
            return CreatedAtAction(nameof(GetExamById), new { id = exam.ExamId }, exam);
        }

        // PUT: api/exams/{id}
        [HttpPut("{id}")]
        public IActionResult UpdateExam(string id, [FromBody] ExamUpdateDto examDto)
        {
            var exam = Exams.FirstOrDefault(e => e.ExamId == id);
            if (exam == null) return NotFound();

            exam.ExamName = examDto.ExamName;
            exam.Description = examDto.Description;
            exam.DurationMinutes = examDto.DurationMinutes;
            exam.ExamType = examDto.ExamType;
            exam.IsPublished = examDto.IsPublished;
            exam.Questions = examDto.Questions?.Select(q => new ExamQuestionDto
            {
                ExamQuestionId = Guid.NewGuid().ToString(),
                QuestionId = q.QuestionId,
                QuestionOrder = q.QuestionOrder,
                PointsWeight = q.PointsWeight,
                Question = null
            }).ToList() ?? new List<ExamQuestionDto>();

            return NoContent();
        }

        // DELETE: api/exams/{id}
        [HttpDelete("{id}")]
        public IActionResult DeleteExam(string id)
        {
            var exam = Exams.FirstOrDefault(e => e.ExamId == id);
            if (exam == null) return NotFound();
            Exams.Remove(exam);
            return NoContent();
        }

        // POST: api/exams/generate
        [HttpPost("generate")]
        public IActionResult GenerateExam([FromBody] ExamGenerateDto generateDto)
        {
            // Fake AI generation logic
            var questions = new List<ExamQuestionDto>();
            int order = 1;
            foreach (var matrix in generateDto.Matrix)
            {
                for (int i = 0; i < matrix.NumEasy + matrix.NumMedium + matrix.NumHard; i++)
                {
                    questions.Add(new ExamQuestionDto
                    {
                        ExamQuestionId = Guid.NewGuid().ToString(),
                        QuestionId = $"Q{Guid.NewGuid()}",
                        QuestionOrder = order++,
                        PointsWeight = 1,
                        Question = null
                    });
                }
            }

            var exam = new ExamDto
            {
                ExamId = NextId.ToString(),
                ExamName = generateDto.ExamName,
                Description = generateDto.Description,
                DurationMinutes = generateDto.DurationMinutes,
                ExamType = generateDto.ExamType,
                CreatedBy = generateDto.CreatedBy,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                Questions = questions
            };

            Exams.Add(exam);
            NextId++;
            return CreatedAtAction(nameof(GetExamById), new { id = exam.ExamId }, exam);
        }
    }
}