using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace CrudDemo.Controllers
{
    public class PythonChallengesController : Controller
    {
        // Challenge 1: Inverser la liste
        public IActionResult Exercise1()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise1(string code)
        {
            var result = ExecutePythonCode(code);
            
            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[7, 6, 5, 4, 3, 2, 1]")
            {
                return RedirectToAction("Exercise2");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 2: Trier du plus grand au plus petit
        public IActionResult Exercise2()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise2(string code)
        {
            var result = ExecutePythonCode(code);
            
            string cleanOutput = System.Text.RegularExpressions.Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[8, 7, 6, 5, 4, 3, 2, 1]")
            {
                return RedirectToAction("Exercise3");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 3: Médiane
        public IActionResult Exercise3()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise3(string code)
        {
            var result = ExecutePythonCode(code);
            
            string cleanOutput = result.output.Trim();
            if (result.success && (cleanOutput == "7" || cleanOutput == "7.0"))
            {
                return RedirectToAction("Exercise4");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 4: Moyenne
        public IActionResult Exercise4()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise4(string code)
        {
            var result = ExecutePythonCode(code);
            
            if (result.success && double.TryParse(result.output.Trim(), out double value))
            {
                if (Math.Abs(value - 18.222222) < 0.01)
                {
                    return RedirectToAction("Exercise5");
                }
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 5: Supprimer les doublons
        public IActionResult Exercise5()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise5(string answer)
        {
            var result = ExecutePythonCode(answer);
            
            string cleanOutput = System.Text.RegularExpressions.Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[1, 2, 3, 4, 5, 7]")
            {
                return RedirectToAction("Exercise6");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 6: Valeurs supérieures à la moyenne
        public IActionResult Exercise6()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise6(string answer)
        {
            var result = ExecutePythonCode(answer);
            
            string cleanOutput = Regex.Replace(result.output.Trim(), @"\s+", " ");
            if (result.success && cleanOutput == "[32, 87, 90, 77, 88]")
            {
                return RedirectToAction("Exercise7");
            }
            
            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
        }

        // Challenge 7: Moyenne des notes par âge (describe)
        public IActionResult Exercise7()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Exercise7(string code)
        {
            var result = ExecutePythonCode(code);

            if (result.success)
            {
                string output = result.output.Trim();

                bool has21 = Regex.IsMatch(output, @"\b21\b\D+9(\.0+)?\b", RegexOptions.Multiline);
                bool has42 = Regex.IsMatch(output, @"\b42\b\D+12\.5\b", RegexOptions.Multiline);
                bool has84 = Regex.IsMatch(output, @"\b84\b\D+14(\.0+)?\b", RegexOptions.Multiline);

                if (has21 && has42 && has84)
                {
                    return RedirectToAction("Success");
                }
            }

            ViewBag.Error = result.success ? "Résultat incorrect. Essayez encore!" : result.error;
            ViewBag.Output = result.output;
            return View();
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

                // Créer un fichier temporaire pour le script Python
                string tempFile = Path.Combine(Path.GetTempPath(), $"script_{Guid.NewGuid()}.py");
                System.IO.File.WriteAllText(tempFile, code);

                // Préparer le processus Python
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = tempFile,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null)
                        return (false, "", "Erreur: impossible de démarrer le processus Python");

                    bool exited = process.WaitForExit(5000);
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    // Nettoyer le fichier temporaire
                    try { System.IO.File.Delete(tempFile); } catch { }

                    if (!exited)
                    {
                        process.Kill();
                        return (false, "", "Timeout: le script a pris trop de temps à s'exécuter");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
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
    }
}
