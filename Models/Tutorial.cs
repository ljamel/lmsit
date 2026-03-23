using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
    public class TutorialCategory
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public string Slug { get; set; } = string.Empty;

        public int OrderIndex { get; set; } = 0;

        public ICollection<Tutorial> Tutorials { get; set; } = new List<Tutorial>();
    }

    public class Tutorial
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Summary { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public TutorialCategory? Category { get; set; }

        public string AuthorId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsPublished { get; set; } = false;

        public string Slug { get; set; } = string.Empty;

        // Thumbnail URL optionnel
        [StringLength(500)]
        public string? ThumbnailUrl { get; set; }
    }
}
