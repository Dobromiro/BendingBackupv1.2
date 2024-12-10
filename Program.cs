using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;

class Program
{
    // Importowanie funkcji WinAPI do zarządzania dyskami sieciowymi
    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string lpName, uint dwFlags, bool fForce);

    static void Main(string[] args)
    {
        string networkPath = @"\\{IP ADDRESS}\BendingBackup";
        string username = "BendingMC";
        string password = "zaq12wsx";
        string localDrive = "Z:";
        string sourceFolder = @"D:\BendCheckerP1\System";

        // Ścieżka pliku line_name.txt w folderze programu
        string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string lineNameFile = Path.Combine(currentDirectory, "line_name.txt");
        string lineName = "UnknownLine"; // Domyślna nazwa linii

        try
        {
            // Odczyt nazwy linii z pliku
            if (File.Exists(lineNameFile))
            {
                lineName = File.ReadAllText(lineNameFile).Trim();
                Console.WriteLine($"Odczytano nazwę linii: {lineName}");
            }
            else
            {
                Console.WriteLine($"Nie znaleziono pliku z nazwą linii ({lineNameFile}). Używana będzie domyślna nazwa.");
            }

            // Tworzenie nazwy pliku z datą, godziną i nazwą linii
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string zipFileName = $"BendBackup_{lineName}_{timestamp}.zip";
            string zipFilePath = Path.Combine(Path.GetTempPath(), zipFileName);

            // Mapowanie dysku sieciowego
            Console.WriteLine("Mapowanie dysku sieciowego...");
            if (!MapDrive(localDrive, networkPath, username, password))
            {
                Console.WriteLine("Nie udało się zmapować dysku sieciowego.");
                return;
            }

            // Tworzenie archiwum ZIP
            Console.WriteLine("Tworzenie archiwum ZIP...");
            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine("Folder źródłowy nie istnieje.");
                return;
            }

            ZipFile.CreateFromDirectory(sourceFolder, zipFilePath);
            Console.WriteLine($"Archiwum ZIP utworzone: {zipFilePath}");

            // Kopiowanie archiwum na dysk sieciowy
            string destinationPath = Path.Combine(localDrive + @"\", zipFileName);
            Console.WriteLine("Kopiowanie pliku na dysk sieciowy...");
            File.Copy(zipFilePath, destinationPath, true);
            Console.WriteLine($"Plik skopiowany: {destinationPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd: {ex.Message}");
        }
        finally
        {
            // Rozłączanie dysku sieciowego
            Console.WriteLine("Odłączanie dysku sieciowego...");
            DisconnectDrive(localDrive);
        }
    }

    // Funkcja mapująca dysk
    private static bool MapDrive(string driveLetter, string networkPath, string username, string password)
    {
        ProcessStartInfo psi = new ProcessStartInfo("net", $"use {driveLetter} {networkPath} /user:{username} {password}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Dysk {driveLetter} został pomyślnie zmapowany.");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Błąd mapowania dysku {driveLetter}: {error}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd podczas mapowania dysku {driveLetter}: {ex.Message}");
            return false;
        }
    }

    // Funkcja rozłączająca dysk
    private static void DisconnectDrive(string driveLetter)
    {
        int result = WNetCancelConnection2(driveLetter, 0, true);
        if (result == 0)
        {
            Console.WriteLine($"Dysk {driveLetter} został pomyślnie odłączony.");
        }
        else
        {
            Console.WriteLine($"Nie udało się odłączyć dysku {driveLetter}.");
        }
    }
}
