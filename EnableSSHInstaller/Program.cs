/*
 * Copyright (c) 2024 Vasile Jianu
 * All rights reserved.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace EnableSSHInstaller
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                EnableOpenSSH();
                StartSSHService();
                ConfigureFirewall();
                await InstallAdditionalSoftwareAsync();  // Use async version of InstallAdditionalSoftware
                await InstallChocolateyAsync();  // Add Chocolatey installation here
                Console.WriteLine("SSH Setup and software installation completed successfully.");
                Console.WriteLine("Please reboot your PC to ensure all changes take effect.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        // Step 1: Enable OpenSSH using DISM
        static void EnableOpenSSH()
        {
            Process dismProcess = new Process();
            dismProcess.StartInfo.FileName = "dism.exe";
            dismProcess.StartInfo.Arguments = "/Online /Add-Capability /CapabilityName:OpenSSH.Server~~~~0.0.1.0";
            dismProcess.StartInfo.UseShellExecute = false;
            dismProcess.StartInfo.RedirectStandardOutput = true;
            dismProcess.StartInfo.CreateNoWindow = true;

            dismProcess.Start();
            string output = dismProcess.StandardOutput.ReadToEnd();
            dismProcess.WaitForExit();
            Console.WriteLine("DISM Output: " + output);
        }

        // Step 2: Start and configure the SSH service
        static void StartSSHService()
        {
            ServiceController sshdService = new ServiceController("sshd");

            if (sshdService.Status != ServiceControllerStatus.Running)
            {
                sshdService.Start();
                sshdService.WaitForStatus(ServiceControllerStatus.Running);
                Console.WriteLine("SSH service started.");
            }

            // Set SSH service to start automatically
            Process scProcess = new Process();
            scProcess.StartInfo.FileName = "sc.exe";
            scProcess.StartInfo.Arguments = "config sshd start=auto";
            scProcess.StartInfo.UseShellExecute = false;
            scProcess.StartInfo.RedirectStandardOutput = true;
            scProcess.StartInfo.CreateNoWindow = true;

            scProcess.Start();
            string output = scProcess.StandardOutput.ReadToEnd();
            scProcess.WaitForExit();

            Console.WriteLine("SSH service set to start automatically: " + output);
            Console.WriteLine("SSH service is running.");
        }

        // Step 3: Configure Firewall to allow SSH traffic
        static void ConfigureFirewall()
        {
            Process firewallProcess = new Process();
            firewallProcess.StartInfo.FileName = "netsh";
            firewallProcess.StartInfo.Arguments = "advfirewall firewall add rule name=\"SSH\" dir=in action=allow protocol=TCP localport=22";
            firewallProcess.StartInfo.UseShellExecute = false;
            firewallProcess.StartInfo.RedirectStandardOutput = true;
            firewallProcess.StartInfo.CreateNoWindow = true;

            firewallProcess.Start();
            string output = firewallProcess.StandardOutput.ReadToEnd();
            firewallProcess.WaitForExit();

            Console.WriteLine("Firewall rule added: " + output);
        }

        // Step 4: Install additional software like Python and 7-Zip
        static async Task InstallAdditionalSoftwareAsync()
        {
            string pythonInstallerUrl = "https://www.python.org/ftp/python/3.12.6/python-3.12.6-amd64.exe";
            string pythonInstallerPath = @"C:\Temp\python-installer.exe";

            // Increase HttpClient timeout to 600 seconds (10 minutes)
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(1);  // Set timeout to 10 minutes
                Console.WriteLine("Downloading Python installer...");
                using (HttpResponseMessage response = await client.GetAsync(pythonInstallerUrl))
                {
                    response.EnsureSuccessStatusCode();  // Ensure the response is successful

                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        if (!Directory.Exists(@"C:\Temp"))
                        {
                            Directory.CreateDirectory(@"C:\Temp");
                        }

                        using (FileStream fs = new FileStream(pythonInstallerPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fs);
                        }
                    }
                }
            }

            // Run Python installer
            Process pythonInstallProcess = new Process();
            pythonInstallProcess.StartInfo.FileName = pythonInstallerPath;
            pythonInstallProcess.StartInfo.Arguments = "/quiet InstallAllUsers=1 PrependPath=1";
            pythonInstallProcess.StartInfo.UseShellExecute = false;
            pythonInstallProcess.StartInfo.CreateNoWindow = true;

            pythonInstallProcess.Start();
            pythonInstallProcess.WaitForExit();

            Console.WriteLine("Python installed successfully.");
        }

        // Step 5: Install Chocolatey
        static async Task InstallChocolateyAsync()
        {
            string chocolateyInstallScriptUrl = "https://community.chocolatey.org/install.ps1";
            string installScriptPath = @"C:\Temp\install-chocolatey.ps1";

            // Download Chocolatey install script using HttpClient
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(1);  // Increase timeout for script download
                Console.WriteLine("Downloading Chocolatey installation script...");
                using (HttpResponseMessage response = await client.GetAsync(chocolateyInstallScriptUrl))
                using (Stream stream = await response.Content.ReadAsStreamAsync())
                {
                    if (!Directory.Exists(@"C:\Temp"))
                    {
                        Directory.CreateDirectory(@"C:\Temp");
                    }

                    using (FileStream fs = new FileStream(installScriptPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await stream.CopyToAsync(fs);
                    }
                }
            }

            // Run Chocolatey installation script using PowerShell
            Process chocoInstallProcess = new Process();
            chocoInstallProcess.StartInfo.FileName = "powershell.exe";
            chocoInstallProcess.StartInfo.Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{installScriptPath}\"";
            chocoInstallProcess.StartInfo.UseShellExecute = false;
            chocoInstallProcess.StartInfo.CreateNoWindow = true;

            chocoInstallProcess.Start();
            chocoInstallProcess.WaitForExit();

            Console.WriteLine("Chocolatey installed successfully.");

            // Refresh environment variables
            var envVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            string pathVar = (string)envVars["PATH"];
            string[] paths = pathVar.Split(Path.PathSeparator);
            string chocoPath = @"C:\ProgramData\chocolatey\bin";
            
            if (!paths.Contains(chocoPath))
            {
                pathVar += Path.PathSeparator + chocoPath;
                Environment.SetEnvironmentVariable("PATH", pathVar, EnvironmentVariableTarget.Process);
            }

            // Install 7-Zip using Chocolatey
            Process sevenZipInstallProcess = new Process();
            sevenZipInstallProcess.StartInfo.FileName = Path.Combine(chocoPath, "choco.exe");
            sevenZipInstallProcess.StartInfo.Arguments = "install 7zip -y";
            sevenZipInstallProcess.StartInfo.UseShellExecute = false;
            sevenZipInstallProcess.StartInfo.CreateNoWindow = true;
            sevenZipInstallProcess.StartInfo.RedirectStandardOutput = true;

            sevenZipInstallProcess.Start();
            string sevenZipOutput = sevenZipInstallProcess.StandardOutput.ReadToEnd();
            sevenZipInstallProcess.WaitForExit();

            Console.WriteLine("7-Zip installation output: " + sevenZipOutput);
            Console.WriteLine("7-Zip installed successfully.");
        }
    }
}
