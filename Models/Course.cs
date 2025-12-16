using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CrudDemo.Models
{
	public class Course
	{
		public int Id { get; set; }

		[Required]
		public required string Title { get; set; }

	[Required]
	public required string Description { get; set; }

	[Range(0, 10000)]
	public decimal Price { get; set; } = 0; // 0 = gratuit

	public bool IsFree { get; set; } = true;

	public string CreatedBy { get; set; } = string.Empty;

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

		public ICollection<Module> Modules { get; set; } = new List<Module>();
	}

	public class Module
	{
		public int Id { get; set; }
		[Required]
		public int CourseId { get; set; }
		[Required]
		public required string Title { get; set; }
		public string? Description { get; set; }
		public int OrderIndex { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		[ForeignKey("CourseId")]
		public Course? Course { get; set; }
		public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
	}

	public class Lesson
	{
		public int Id { get; set; }
		[Required]
		public int ModuleId { get; set; }
		[Required]
		public required string Title { get; set; }
		[Required]
		public required string Description { get; set; }
		public string? VideoFileName { get; set; }
		public string? VideoPath { get; set; }
		public int OrderIndex { get; set; } = 1;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		[ForeignKey("ModuleId")]
		public Module? Module { get; set; }
		public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
	}

	public class Quiz
	{
		public int Id { get; set; }
		[Required]
		public int LessonId { get; set; }
		[Required]
		public required string Question { get; set; }
		public string? Description { get; set; }
		public int Points { get; set; } = 1;
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		[ForeignKey("LessonId")]
		public Lesson? Lesson { get; set; }
		public ICollection<QuizOption> Options { get; set; } = new List<QuizOption>();
	}

	public class QuizOption
	{
		public int Id { get; set; }
		[Required]
		public int QuizId { get; set; }
		[Required]
		public required string Text { get; set; }
		public bool IsCorrect { get; set; }
		[ForeignKey("QuizId")]
		public Quiz? Quiz { get; set; }
	}

	public class UserQuizResult
	{
		public int Id { get; set; }
		public required string UserId { get; set; }
		[Required]
		public int QuizId { get; set; }
		public bool IsCorrect { get; set; }
		public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
		[ForeignKey("QuizId")]
		public Quiz? Quiz { get; set; }
	}
}
