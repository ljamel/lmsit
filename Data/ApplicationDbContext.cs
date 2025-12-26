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
	public DbSet<Payment> Payments { get; set; }
	public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
	public DbSet<Subscription> Subscriptions { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Relations avec cascade delete
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

			// ============================================================
			// INDEX POUR OPTIMISER LES REQUÊTES ET RÉDUIRE LA CONSOMMATION CPU
			// ============================================================
			
			// Index sur Subscriptions pour les vérifications d'abonnement actif
			modelBuilder.Entity<Subscription>()
				.HasIndex(s => new { s.UserId, s.IsActive, s.Status })
				.HasDatabaseName("IX_Subscriptions_UserId_IsActive_Status");

			// Index sur CourseEnrollments pour les vérifications d'inscription
			modelBuilder.Entity<CourseEnrollment>()
				.HasIndex(e => new { e.UserId, e.CourseId, e.IsActive })
				.HasDatabaseName("IX_CourseEnrollments_UserId_CourseId_IsActive");

			// Index sur Payments pour recherche par utilisateur
			modelBuilder.Entity<Payment>()
				.HasIndex(p => new { p.UserId, p.CreatedAt })
				.HasDatabaseName("IX_Payments_UserId_CreatedAt");

			// Index sur Payments pour recherche par StripePaymentIntentId
			modelBuilder.Entity<Payment>()
				.HasIndex(p => p.StripePaymentIntentId)
				.HasDatabaseName("IX_Payments_StripePaymentIntentId");

			// Index sur UserQuizResults pour les résultats par utilisateur et quiz
			modelBuilder.Entity<UserQuizResult>()
				.HasIndex(r => new { r.UserId, r.QuizId })
				.HasDatabaseName("IX_UserQuizResults_UserId_QuizId");

			// Index sur Courses pour tri par date de création
			modelBuilder.Entity<Course>()
				.HasIndex(c => c.CreatedAt)
				.HasDatabaseName("IX_Courses_CreatedAt");

			// Index sur Modules pour ordre d'affichage
			modelBuilder.Entity<Module>()
				.HasIndex(m => new { m.CourseId, m.OrderIndex })
				.HasDatabaseName("IX_Modules_CourseId_OrderIndex");

			// Index sur Lessons pour ordre d'affichage
			modelBuilder.Entity<Lesson>()
				.HasIndex(l => new { l.ModuleId, l.OrderIndex })
				.HasDatabaseName("IX_Lessons_ModuleId_OrderIndex");

			// Index sur Quizzes pour recherche par leçon
			modelBuilder.Entity<Quiz>()
				.HasIndex(q => q.LessonId)
				.HasDatabaseName("IX_Quizzes_LessonId");

			// Index sur QuizOptions pour recherche par quiz
			modelBuilder.Entity<QuizOption>()
				.HasIndex(o => o.QuizId)
				.HasDatabaseName("IX_QuizOptions_QuizId");
		}
	}
}
