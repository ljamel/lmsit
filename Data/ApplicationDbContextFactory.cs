using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CrudDemo.Data
{
    /// <summary>
    /// Factory pour créer le DbContext au moment du design (migrations EF Core)
    /// Nécessaire quand on utilise AddDbContextPool
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Créer les options du DbContext
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            
            // Utiliser une chaîne de connexion par défaut pour les migrations
            var connectionString = "Server=127.0.0.1;Port=3307;Database=NomDeTaBase;User=root;Password=StrongPass123!;";
            
            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
