using System.ComponentModel.DataAnnotations;

namespace CrudDemo.Models
{
    public class CtfChallengePageViewModel
    {
        public List<CtfChallengeCardViewModel> Challenges { get; set; } = new();
        public List<CtfLessonOptionViewModel> LessonOptions { get; set; } = new();
        public bool IsAdmin { get; set; }
    }

    public class CtfChallengeCardViewModel
    {
        public int QuizId { get; set; }
        public int LessonId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Points { get; set; }
        public bool IsSolved { get; set; }
        public string? CurrentFlag { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string ModuleTitle { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
    }

    public class CtfLessonOptionViewModel
    {
        public int LessonId { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class CreateCtfChallengeRequest
    {
        [Required]
        public int LessonId { get; set; }

        [Required]
        [StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Flag { get; set; } = string.Empty;

        [Range(1, 5000)]
        public int Points { get; set; } = 100;
    }

    public class SubmitCtfFlagRequest
    {
        [Required]
        public int QuizId { get; set; }

        [Required]
        [StringLength(255)]
        public string Flag { get; set; } = string.Empty;
    }
}
