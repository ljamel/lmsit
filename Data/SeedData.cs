using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using CrudDemo.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CrudDemo.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            // S'assurer que la base de données est créée
            await context.Database.EnsureCreatedAsync();

            // ============================================================
            // 1. CRÉATION DES RÔLES
            // ============================================================
            const string adminRole = "Admin";
            const string userRole = "User";

            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(adminRole));
                Console.WriteLine("✓ Rôle Admin créé");
            }

            if (!await roleManager.RoleExistsAsync(userRole))
            {
                await roleManager.CreateAsync(new IdentityRole(userRole));
                Console.WriteLine("✓ Rôle User créé");
            }

            // ============================================================
            // 2. CRÉATION DE L'UTILISATEUR ADMIN
            // ============================================================
            const string adminEmail = "admin@ingenius.com";
            const string adminPassword = "Admin123!";

            // Optimisé: Utiliser AsQueryable() au lieu de chercher dans Users directement
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser 
                { 
                    UserName = adminEmail, 
                    Email = adminEmail, 
                    EmailConfirmed = true 
                };
                
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                    Console.WriteLine($"✓ Utilisateur Admin créé: {adminEmail} / {adminPassword}");
                }
                else
                {
                    Console.WriteLine($"✗ Erreur création admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, adminRole))
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                    Console.WriteLine("✓ Rôle Admin ajouté à l'utilisateur existant");
                }
            }

            // ============================================================
            // 3. CRÉATION D'UTILISATEURS DE TEST (avec abonnements)
            // ============================================================
            var testUsers = new[]
            {
                new { Email = "julien.r@test.com", Password = "Test123!", HasSubscription = true },
                new { Email = "amelie.d@test.com", Password = "Test123!", HasSubscription = true },
                new { Email = "marc.l@test.com", Password = "Test123!", HasSubscription = false }
            };

            foreach (var testUser in testUsers)
            {
                var user = await userManager.FindByEmailAsync(testUser.Email);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = testUser.Email,
                        Email = testUser.Email,
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(user, testUser.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, userRole);
                        
                        // Créer un abonnement actif si nécessaire
                        if (testUser.HasSubscription)
                        {
                            var subscription = new Subscription
                            {
                                UserId = testUser.Email,
                                StripeSubscriptionId = $"sub_test_{Guid.NewGuid().ToString().Substring(0, 8)}",
                                StripeCustomerId = $"cus_test_{Guid.NewGuid().ToString().Substring(0, 8)}",
                                Status = "active",
                                IsActive = true,
                                StartDate = DateTime.UtcNow
                            };
                            context.Subscriptions.Add(subscription);
                        }
                        
                        Console.WriteLine($"✓ Utilisateur test créé: {testUser.Email} (Abonnement: {testUser.HasSubscription})");
                    }
                }
            }

            await context.SaveChangesAsync();

            // ============================================================
            // 4. CRÉATION DE COURS D'EXEMPLE
            // ============================================================
            // Optimisé: AsNoTracking pour vérification lecture seule
            if (!await context.Courses.AsNoTracking().AnyAsync())
            {
                var courses = new[]
                {
                    new Course
                    {
                        Title = "Introduction à la Cybersécurité",
                        Description = "Découvrez les fondamentaux de la cybersécurité, les menaces courantes et les bonnes pratiques de protection.",
                        CreatedBy = adminEmail,
                        CreatedAt = DateTime.UtcNow,
                        IsFree = false
                    },
                    new Course
                    {
                        Title = "Hacking Éthique - Niveau Débutant",
                        Description = "Apprenez les bases du hacking éthique et du pentesting avec des exercices pratiques.",
                        CreatedBy = adminEmail,
                        CreatedAt = DateTime.UtcNow,
                        IsFree = false
                    },
                    new Course
                    {
                        Title = "Sécurité des Réseaux",
                        Description = "Maîtrisez la sécurisation des infrastructures réseau et la détection d'intrusions.",
                        CreatedBy = adminEmail,
                        CreatedAt = DateTime.UtcNow,
                        IsFree = false
                    }
                };

                context.Courses.AddRange(courses);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ {courses.Length} cours créés");

                // ============================================================
                // 5. CRÉATION DE MODULES ET LEÇONS
                // ============================================================
                var course1 = courses[0];
                
                var module1 = new Module
                {
                    CourseId = course1.Id,
                    Title = "Les Bases de la Sécurité",
                    Description = "Introduction aux concepts fondamentaux",
                    OrderIndex = 1
                };
                context.Modules.Add(module1);
                await context.SaveChangesAsync();

                var lessons = new[]
                {
                    new Lesson
                    {
                        ModuleId = module1.Id,
                        Title = "Qu'est-ce que la cybersécurité ?",
                        Description = "La cybersécurité est la pratique de protéger les systèmes, réseaux et programmes contre les attaques numériques.",
                        VideoPath = "/videos/intro-cybersecurity.mp4",
                        OrderIndex = 1
                    },
                    new Lesson
                    {
                        ModuleId = module1.Id,
                        Title = "Les types de menaces",
                        Description = "Découvrez les différents types de menaces : malware, phishing, ransomware, etc.",
                        VideoPath = "/videos/types-menaces.mp4",
                        OrderIndex = 2
                    },
                    new Lesson
                    {
                        ModuleId = module1.Id,
                        Title = "Les bonnes pratiques de sécurité",
                        Description = "Apprenez les bases pour sécuriser vos systèmes et protéger vos données.",
                        VideoPath = "/videos/bonnes-pratiques.mp4",
                        OrderIndex = 3
                    }
                };

                context.Lessons.AddRange(lessons);
                await context.SaveChangesAsync();
                Console.WriteLine($"✓ Module et {lessons.Length} leçons créés");

                // ============================================================
                // 6. CRÉATION DE QUIZ
                // ============================================================
                var quiz = new Quiz
                {
                    LessonId = lessons[0].Id,
                    Question = "Qu'est-ce qu'un firewall ?",
                    Points = 10
                };
                context.Quizzes.Add(quiz);
                await context.SaveChangesAsync();

                var quizOptions = new[]
                {
                    new QuizOption
                    {
                        QuizId = quiz.Id,
                        Text = "Un système de protection qui contrôle le trafic réseau",
                        IsCorrect = true
                    },
                    new QuizOption
                    {
                        QuizId = quiz.Id,
                        Text = "Un logiciel de navigation web",
                        IsCorrect = false
                    },
                    new QuizOption
                    {
                        QuizId = quiz.Id,
                        Text = "Un type de virus informatique",
                        IsCorrect = false
                    },
                    new QuizOption
                    {
                        QuizId = quiz.Id,
                        Text = "Un outil de cryptage de fichiers",
                        IsCorrect = false
                    }
                };

                context.QuizOptions.AddRange(quizOptions);
                await context.SaveChangesAsync();
                Console.WriteLine("✓ Quiz et options créés");
            }

            // ============================================================
            // RÉSUMÉ
            // ============================================================
            Console.WriteLine("\n═══════════════════════════════════════════════════════");
            Console.WriteLine("🎉 Seed Data initialisé avec succès!");
            Console.WriteLine("═══════════════════════════════════════════════════════");
            Console.WriteLine("\n📝 Comptes créés:");
            Console.WriteLine($"  Admin: admin@ingenius.com / Admin123!");
            Console.WriteLine($"  User1: julien.r@test.com / Test123! (avec abonnement)");
            Console.WriteLine($"  User2: amelie.d@test.com / Test123! (avec abonnement)");
            Console.WriteLine($"  User3: marc.l@test.com / Test123! (sans abonnement)");
            Console.WriteLine("\n📚 Contenu créé:");
            Console.WriteLine($"  - {context.Courses.Count()} cours");
            Console.WriteLine($"  - {context.Modules.Count()} modules");
            Console.WriteLine($"  - {context.Lessons.Count()} leçons");
            Console.WriteLine($"  - {context.Quizzes.Count()} quiz");
            Console.WriteLine("═══════════════════════════════════════════════════════\n");
        }
    }
}
