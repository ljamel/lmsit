using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    public class LessonEngagement
    {
        public int Id { get; set; }

        [Required]
        public required string UserId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public int LessonId { get; set; }

        public DateTime EngagedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [ForeignKey("LessonId")]
        public Lesson? Lesson { get; set; }
    }
}
