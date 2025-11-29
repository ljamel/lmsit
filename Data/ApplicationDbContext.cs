using CrudDemo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CrudDemo.Data
{
	public class ApplicationDbContext : IdentityDbContext<IdentityUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
		{
		}

		public DbSet<Produit> Produits { get; set; }
		public DbSet<Course> Courses { get; set; }
		public DbSet<Module> Modules { get; set; }
		public DbSet<Lesson> Lessons { get; set; }
		public DbSet<Quiz> Quizzes { get; set; }
		public DbSet<QuizOption> QuizOptions { get; set; }
		public DbSet<UserQuizResult> UserQuizResults { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Course>()
				.HasMany(c => c.Modules)
				.WithOne(m => m.Course)
				.HasForeignKey(m => m.CourseId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Module>()
				.HasMany(m => m.Lessons)
				.WithOne(l => l.Module)
				.HasForeignKey(l => l.ModuleId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Lesson>()
				.HasMany(l => l.Quizzes)
				.WithOne(q => q.Lesson)
				.HasForeignKey(q => q.LessonId)
				.OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<Quiz>()
				.HasMany(q => q.Options)
				.WithOne(o => o.Quiz)
				.HasForeignKey(o => o.QuizId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
