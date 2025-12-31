using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    public class Comment
    {
        public int Id { get; set; }
        
        [Required]
        public int CourseId { get; set; }
        
        [Required]
        public required string UserId { get; set; }
        
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public required string Content { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }
    }
}
