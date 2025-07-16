using Microsoft.AspNetCore.Mvc;
using BE_Phygens.Models;
using Microsoft.EntityFrameworkCore;

namespace BE_Phygens.Controllers
{
    [ApiController]
    [Route("topics")]
    public class TopicsController : ControllerBase
    {
        private readonly PhygensContext _context;

        public TopicsController(PhygensContext context)
        {
            _context = context;
        }

        // DTO Classes for requests
        public class CreateTopicDto
        {
            public string TopicName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string GradeLevel { get; set; } = string.Empty;
            public int DisplayOrder { get; set; }
        }

        public class UpdateTopicDto
        {
            public string? TopicName { get; set; }
            public string? Description { get; set; }
            public string? GradeLevel { get; set; }
            public int? DisplayOrder { get; set; }
            public bool? IsActive { get; set; }
        }

        // GET: topics
        [HttpGet]
        public async Task<IActionResult> GetAllTopics([FromQuery] string? gradeLevel = null, [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.PhysicsTopics
                    .Include(t => t.Questions)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(gradeLevel))
                {
                    query = query.Where(t => t.GradeLevel == gradeLevel);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(t => t.IsActive == isActive.Value);
                }
                else
                {
                    // Default: chỉ lấy active topics
                    query = query.Where(t => t.IsActive);
                }

                var topics = await query
                    .OrderBy(t => t.DisplayOrder)
                    .ToListAsync();

                var topicDtos = topics.Select(t => new
                {
                    topicId = t.TopicId,
                    topicName = t.TopicName,
                    description = t.Description,
                    gradeLevel = t.GradeLevel,
                    displayOrder = t.DisplayOrder,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    questionCount = t.Questions?.Count ?? 0
                }).ToList();

                return Ok(topicDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // GET: topics/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTopicById(string id)
        {
            try
            {
                var topic = await _context.PhysicsTopics
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.TopicId == id);

                if (topic == null) 
                    return NotFound(new { error = "Topic not found" });

                var topicDto = new
                {
                    topicId = topic.TopicId,
                    topicName = topic.TopicName,
                    description = topic.Description,
                    gradeLevel = topic.GradeLevel,
                    displayOrder = topic.DisplayOrder,
                    isActive = topic.IsActive,
                    createdAt = topic.CreatedAt,
                    questionCount = topic.Questions?.Count ?? 0
                };

                return Ok(topicDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // POST: topics
        [HttpPost]
        public async Task<IActionResult> CreateTopic([FromBody] CreateTopicDto topicDto)
        {
            try
            {
                // Validation
                if (string.IsNullOrEmpty(topicDto.TopicName))
                {
                    return BadRequest(new { error = "Topic name is required" });
                }

                if (string.IsNullOrEmpty(topicDto.GradeLevel))
                {
                    return BadRequest(new { error = "Grade level is required" });
                }

                // Check if topic with same name and grade already exists
                var existingTopic = await _context.PhysicsTopics
                    .FirstOrDefaultAsync(t => t.TopicName == topicDto.TopicName && 
                                            t.GradeLevel == topicDto.GradeLevel);

                if (existingTopic != null)
                {
                    return BadRequest(new { 
                        error = "Topic with same name already exists for this grade level",
                        existingTopicId = existingTopic.TopicId 
                    });
                }

                var topic = new PhysicsTopic
                {
                    TopicId = Guid.NewGuid().ToString(),
                    TopicName = topicDto.TopicName,
                    Description = topicDto.Description,
                    GradeLevel = topicDto.GradeLevel,
                    DisplayOrder = topicDto.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PhysicsTopics.Add(topic);
                await _context.SaveChangesAsync();

                var responseDto = new
                {
                    topicId = topic.TopicId,
                    topicName = topic.TopicName,
                    description = topic.Description,
                    gradeLevel = topic.GradeLevel,
                    displayOrder = topic.DisplayOrder,
                    isActive = topic.IsActive,
                    createdAt = topic.CreatedAt,
                    questionCount = 0
                };

                return CreatedAtAction(nameof(GetTopicById), new { id = topic.TopicId }, responseDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // PUT: topics/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTopic(string id, [FromBody] UpdateTopicDto topicDto)
        {
            try
            {
                var topic = await _context.PhysicsTopics.FindAsync(id);
                if (topic == null) 
                    return NotFound(new { error = "Topic not found" });

                // Update only provided fields
                if (!string.IsNullOrEmpty(topicDto.TopicName))
                {
                    // Check for duplicate name in same grade
                    var existingTopic = await _context.PhysicsTopics
                        .FirstOrDefaultAsync(t => t.TopicId != id && 
                                            t.TopicName == topicDto.TopicName && 
                                            t.GradeLevel == (topicDto.GradeLevel ?? topic.GradeLevel));

                    if (existingTopic != null)
                    {
                        return BadRequest(new { 
                            error = "Topic with same name already exists for this grade level" 
                        });
                    }

                    topic.TopicName = topicDto.TopicName;
                }

                if (topicDto.Description != null)
                    topic.Description = topicDto.Description;

                if (!string.IsNullOrEmpty(topicDto.GradeLevel))
                    topic.GradeLevel = topicDto.GradeLevel;

                if (topicDto.DisplayOrder.HasValue)
                    topic.DisplayOrder = topicDto.DisplayOrder.Value;

                if (topicDto.IsActive.HasValue)
                    topic.IsActive = topicDto.IsActive.Value;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // DELETE: topics/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTopic(string id)
        {
            try
            {
                var topic = await _context.PhysicsTopics
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.TopicId == id);

                if (topic == null) 
                    return NotFound(new { error = "Topic not found" });

                // Check if topic has questions
                if (topic.Questions != null && topic.Questions.Any())
                {
                    return BadRequest(new { 
                        error = "Cannot delete topic that has questions", 
                        questionCount = topic.Questions.Count 
                    });
                }

                _context.PhysicsTopics.Remove(topic);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }

        // GET: topics/grade/{gradeLevel} - Convenience endpoint
        [HttpGet("grades/{gradeLevel}")]
        public async Task<IActionResult> GetTopicsByGrade(string gradeLevel)
        {
            try
            {
                var topics = await _context.PhysicsTopics
                    .Include(t => t.Questions)
                    .Where(t => t.GradeLevel == gradeLevel && t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ToListAsync();

                var topicDtos = topics.Select(t => new
                {
                    topicId = t.TopicId,
                    topicName = t.TopicName,
                    description = t.Description,
                    gradeLevel = t.GradeLevel,
                    displayOrder = t.DisplayOrder,
                    isActive = t.IsActive,
                    createdAt = t.CreatedAt,
                    questionCount = t.Questions?.Count ?? 0
                }).ToList();

                return Ok(topicDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    error = "Internal server error", 
                    message = ex.Message 
                });
            }
        }
    }
} 