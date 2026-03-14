using CrudDemo.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CrudDemo.Controllers
{
    [Authorize]
    public class PythonChallengesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PythonChallengesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!await HasPaidAccessAsync())
            {
                TempData["Error"] = "Cette section nécessite un abonnement payant.";
                context.Result = RedirectToAction("SubscriptionCheckout", "Payment");
                return;
            }

            await next();
        }

        // Challenge 1: Inverser la liste
        public IActionResult Exercise1()
        {
            return View("Exercise8");
        }

        [HttpPost]
        public IActionResult Exercise1(string code)
        {
            var result = ExecutePythonCode(code);

            string cleanOutput = result.output.Trim();
            if (result.success && cleanOutput == "Alice")
            {
                TempData["ShowPromoPopup"] = true;
                return RedirectToAction("Exercise2");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise8");
        }

        // Challenge 2: Trier du plus grand au plus petit
        public IActionResult Exercise2()
        {
            ViewBag.ShowPromoPopup = TempData["ShowPromoPopup"] != null;
            return View("Exercise9");
        }

        [HttpPost]
        public IActionResult Exercise2(string code)
        {
            var result = ExecutePythonCode(code);
            
            string cleanOutput = result.output.Trim();
            if (result.success && cleanOutput == "12")
            {
                return RedirectToAction("Exercise3");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise9");
        }

        // Challenge 3: Médiane
        public IActionResult Exercise3()
        {
            return View("Exercise10");
        }

        [HttpPost]
        public IActionResult Exercise3(string code)
        {
            var result = ExecutePythonCode(code);
            
            string cleanOutput = result.output.Trim();
            if (result.success && cleanOutput == "20")
            {
                return RedirectToAction("Exercise4");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise10");
        }

        // Challenge 4: Moyenne
        public IActionResult Exercise4()
        {
            return View("Exercise1");
        }

        [HttpPost]
        public IActionResult Exercise4(string code)
        {
            var result = ExecutePythonCode(code);

            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[7, 6, 5, 4, 3, 2, 1]")
            {
                return RedirectToAction("Exercise5");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise1");
        }

        // Challenge 5: Supprimer les doublons
        public IActionResult Exercise5()
        {
            return View("Exercise2");
        }

        [HttpPost]
        public IActionResult Exercise5(string answer)
        {
            var result = ExecutePythonCode(answer);
            
            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[8, 7, 6, 5, 4, 3, 2, 1]")
            {
                return RedirectToAction("Exercise6");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise2");
        }

        // Challenge 6: Valeurs supérieures à la moyenne
        public IActionResult Exercise6()
        {
            return View("Exercise3");
        }

        [HttpPost]
        public IActionResult Exercise6(string answer)
        {
            var result = ExecutePythonCode(answer);
            
            string cleanOutput = result.output.Trim();
            if (result.success && (cleanOutput == "7" || cleanOutput == "7.0"))
            {
                return RedirectToAction("Exercise7");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise3");
        }

        // Challenge 7: Moyenne des notes par âge (describe)
        public IActionResult Exercise7()
        {
            return View("Exercise4");
        }

        [HttpPost]
        public IActionResult Exercise7(string code)
        {
            var result = ExecutePythonCode(code);

            if (result.success && double.TryParse(result.output.Trim(), out double value))
            {
                if (Math.Abs(value - 18.222222) < 0.01)
                {
                    return RedirectToAction("Exercise8");
                }
            }

            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise4");
        }

        // Challenge 8: Variables de base
        public IActionResult Exercise8()
        {
            return View("Exercise5");
        }

        [HttpPost]
        public IActionResult Exercise8(string code)
        {
            var result = ExecutePythonCode(code);

            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[1, 2, 3, 4, 5, 7]")
            {
                return RedirectToAction("Exercise9");
            }

            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise5");
        }

        // Challenge 9: Calcul simple
        public IActionResult Exercise9()
        {
            return View("Exercise6");
        }

        [HttpPost]
        public IActionResult Exercise9(string code)
        {
            var result = ExecutePythonCode(code);

            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[32, 87, 90, 77, 88]")
            {
                return RedirectToAction("Success");
            }

            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View("Exercise6");
        }

        // Page de succès
        public IActionResult Success()
        {
            return View();
        }

        // Exécuter le code Python
        private (bool success, string output, string error) ExecutePythonCode(string code)
        {
            try
            {
                // Valider le code avant exécution
                var validationResult = ValidatePythonCode(code);
                if (!validationResult.isValid)
                {
                    return (false, "", validationResult.error);
                }

                // Créer un dossier temporaire isolé pour le script Python
                string tempDir = Path.Combine(Path.GetTempPath(), $"python_challenge_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);
                string tempFile = Path.Combine(tempDir, "script.py");
                string scriptWithNumexpr = BuildScriptWithNumexpr(code);
                System.IO.File.WriteAllText(tempFile, scriptWithNumexpr);

                // Préparer le processus Python
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "python3",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = tempDir,
                };

                psi.ArgumentList.Add("-I");
                psi.ArgumentList.Add(tempFile);
                psi.Environment["PYTHONPATH"] = string.Empty;
                psi.Environment["PYTHONHOME"] = string.Empty;
                psi.Environment["HOME"] = tempDir;

                using (Process? process = Process.Start(psi))
                {
                    if (process == null)
                        return (false, "", "Erreur: impossible de démarrer le processus Python");

                    bool exited = process.WaitForExit(5000);
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Nettoyer le dossier temporaire
                    try
                    {
                        if (Directory.Exists(tempDir))
                        {
                            Directory.Delete(tempDir, recursive: true);
                        }
                    }
                    catch
                    {
                    }

                    if (!exited)
                    {
                        process.Kill();
                        return (false, "", "Timeout: le script a pris trop de temps à s'exécuter");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        if (error.Contains("No module named 'numexpr'", StringComparison.OrdinalIgnoreCase) ||
                            error.Contains("No module named \"numexpr\"", StringComparison.OrdinalIgnoreCase))
                        {
                            return (false, output, "Le module Python 'numexpr' est requis pour ces exercices. Installe-le côté serveur avec: pip install numexpr");
                        }

                        return (false, output, error);
                    }

                    return (true, output, "");
                }
            }
            catch (Exception ex)
            {
                return (false, "", $"Erreur d'exécution: {ex.Message}");
            }
        }

        private static string BuildScriptWithNumexpr(string userCode)
        {
            string userCodeBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(userCode));

            const string bootstrap = @"import base64
import numexpr as ne

def ne_eval(expression, variables=None):
    if variables is None:
        variables = {}
    return ne.evaluate(expression, local_dict=variables)

SAFE_BUILTINS = {
    'print': print,
    'len': len,
    'range': range,
    'sum': sum,
    'min': min,
    'max': max,
    'sorted': sorted,
    'list': list,
    'dict': dict,
    'set': set,
    'tuple': tuple,
    'int': int,
    'float': float,
    'str': str,
    'bool': bool,
    'abs': abs,
    'round': round,
    'enumerate': enumerate,
    'zip': zip,
    'map': map,
    'filter': filter,
    'any': any,
    'all': all,
}

RUNTIME_GLOBALS = {
    '__builtins__': SAFE_BUILTINS,
    'ne': ne,
    'ne_eval': ne_eval,
}

USER_CODE_B64 = '__USER_CODE_B64__'
USER_CODE = base64.b64decode(USER_CODE_B64).decode('utf-8', errors='replace')

exec(compile(USER_CODE, '<user_code>', 'exec'), RUNTIME_GLOBALS, {})

";

            return bootstrap.Replace("__USER_CODE_B64__", userCodeBase64);
        }

        // Valider le code Python - blocage des opérations dangereuses
        private (bool isValid, string error) ValidatePythonCode(string code)
        {
            // List des modules/fonctions dangereuses à bloquer
            string[] blockedKeywords = new[]
            {
                // Modules d'accès système
                "import os",
                "from os",
                "import sys",
                "from sys",
                "import subprocess",
                "from subprocess",
                "import importlib",
                "from importlib",
                "import pathlib",
                "from pathlib",
                "import shutil",
                "from shutil",
                "import tempfile",
                "from tempfile",
                "import glob",
                "from glob",
                "import socket",
                "from socket",
                "import urllib",
                "from urllib",
                "import requests",
                "from requests",
                "import pickle",
                "from pickle",
                "import marshal",
                "from marshal",
                "import ctypes",
                "from ctypes",
                
                // Fonctions dangereuses
                "__import__",
                "exec(",
                "eval(",
                "compile(",
                "open(",
                "input(",
                "raw_input(",
                "file(",
                "getattr(",
                "setattr(",
                "delattr(",
                "hasattr(",
                "dir(",
                "vars(",
                "locals(",
                "globals(",
                "__dict__",
                "__builtins__",
                "__class__",
                "__bases__",
                "__subclasses__",
                "__mro__",
                
                // Accès à modules
                "os.",
                "sys.",
                "subprocess.",
                "pathlib.",
                "shutil.",
                "socket.",
                "importlib.",
                "urllib.",
                "ctypes.",
                "pickle.",
                
                // Appels système
                ".system(",
                ".popen(",
                ".call(",
                ".run(",
                ".execv",
                ".fork(",
                ".spawn",
                
                // Accès avancé
                "sys.modules",
                "__loader__",
                "__spec__",
                "__code__",
                "__globals__",
                "__closure__",
                
                // Contournements par concaténation
                "' + '",
                "\" + \"",
                "f'",
                "f\"",
                ".format(",
                ".join(",
                
                // Module d'accès aux fichiers
                "pathlib",
                "Path(",
                ".iterdir(",
                ".glob(",
                ".rglob(",
                ".read_text(",
                ".write_text(",
                ".unlink(",
                ".rmdir(",
                ".mkdir(",
            };

            string codeLower = code.ToLower();

            // Interdire globalement tout import utilisateur : les libs nécessaires
            // sont préchargées côté sandbox (numexpr -> ne)
            if (Regex.IsMatch(code, @"^\s*(from|import)\s+", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                return (false, "❌ Les imports utilisateur sont désactivés pour ces exercices. Utilise numexpr via 'ne' déjà disponible.");
            }

            // Bloquer les dunder pour éviter l'introspection/contournements
            if (Regex.IsMatch(code, @"__\w+__|__\w+", RegexOptions.IgnoreCase))
            {
                return (false, "❌ Accès aux attributs internes Python interdit pour des raisons de sécurité.");
            }
            
            foreach (string keyword in blockedKeywords)
            {
                if (codeLower.Contains(keyword.ToLower()))
                {
                    return (false, $"❌ Code non autorisé: '{keyword}' est interdit pour des raisons de sécurité.");
                }
            }

            // Vérifier que le code ne contient pas trop de caractères spéciaux suspects
            int specialChars = code.Count(c => c == '_' || c == '`' || c == '~' || c == '@');
            if (specialChars > 20)
            {
                return (false, "❌ Code suspecté: trop de caractères spéciaux. Utilise du code Python standard.");
            }

            return (true, "");
        }

        private async Task<bool> HasPaidAccessAsync()
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            var userEmail = User.Identity?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return false;
            }

            var subscription = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.UserId == userEmail)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (subscription == null)
            {
                return false;
            }

            if (subscription.IsActive && subscription.Status == "active")
            {
                return true;
            }

            if (subscription.Status == "canceled")
            {
                var accessUntil = subscription.EndDate ?? subscription.StartDate.AddMonths(1);
                return DateTime.UtcNow <= accessUntil;
            }

            return false;
        }
    }
}
