using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceProcess;

namespace w11tk
{
    internal class Program
    {
        static void controlService(string serviceName, bool status)
        {
            try
            {
                using (ServiceController sc = new ServiceController(serviceName))
                {
                    if (sc.Status == ServiceControllerStatus.Running || sc.Status == ServiceControllerStatus.Paused)
                    {
                        Console.WriteLine($"stopping {sc.DisplayName}...");
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }

                    Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\{serviceName}", "Start", status ? 4 : 2, RegistryValueKind.DWord);

                    if (status)
                        Console.WriteLine($"disabled {serviceName} successfully");
                    else
                        Console.WriteLine($"enabled {serviceName} successfully");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error while disabling {serviceName}: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            string outCmd = "", hosts = Environment.GetFolderPath(Environment.SpecialFolder.System) + @"\drivers\etc\hosts";

            if (!(new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
            {
                Console.WriteLine($"[-] no admin perms provided, please restart app as admin and try again");
                Console.WriteLine($"[*] press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1337);
            }

            Console.WriteLine("[*] you just ran w11tk utility that was designed to disable as much telemetry as possible on windows 11\n" +
                "[~] you'll be prompted now, press 'r' if you want to revert every change made by this utility, or just press enter to skip and continue");

            if (Console.ReadKey().Key == ConsoleKey.R)
            {
                Console.Clear();
                Console.WriteLine("[*] rev!");
                Console.WriteLine("[*] cleaning hosts file...");
                File.WriteAllText(hosts, ""); //clear hosts

                Console.WriteLine("[*] enabling registry keys...");

                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting", "Disabled", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 1, RegistryValueKind.DWord);

                Console.WriteLine("[*] enabling services..");

                controlService("DiagTrack", false);
                controlService("dmwappushservice", false);
                controlService("PimIndexMaintenanceSvc", false);
                controlService("UnistoreSvc", false);
                controlService("UserDataSvc", false);

                Console.WriteLine("[+] done!");
                Console.WriteLine("[*] press any key to exit...");
                Console.ReadKey();
                Environment.Exit(1337); 
            }

            IEnumerable<string> blocklist = System.IO.File.ReadLines("blocklist.txt");

            //format outCmd
            foreach (string block in blocklist)
            {
                outCmd += $"127.0.0.1\t{block}\n";
            }

            Console.WriteLine("[*] your 'hosts' file is going to be cleaned and overwritten right now, please make sure you don't have anything important in it");
            Console.WriteLine("[*] press any key to continue...");
            Console.ReadKey();

            File.WriteAllText(hosts, ""); //clear hosts before writing

            using (StreamWriter writer = new StreamWriter(hosts, true))
            {
                writer.WriteLine(outCmd);
            }

            Console.WriteLine($"[+] done, {blocklist.Count()} domains were blocked!");

            Console.WriteLine("[*] your windows registry is going to be modified right now, you may close this window if you don't want to tweak registry, otherwise...");
            Console.WriteLine("[*] press any key to continue...");
            Console.ReadKey();

            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\Windows Error Reporting", "Disabled", 1, RegistryValueKind.DWord);

            Console.WriteLine("[+] done!");

            Console.WriteLine("[*] some of your windows services are going to be disabled/deleted right now, you may close this window if you don't want to tweak windows services, otherwise...");
            Console.WriteLine("[*] press any key to continue...");
            Console.ReadKey();

            controlService("DiagTrack", true);
            controlService("dmwappushservice", true);
            controlService("PimIndexMaintenanceSvc", true);
            controlService("UnistoreSvc", true);
            controlService("UserDataSvc", true);

            Console.WriteLine("[+] done!");

            Console.WriteLine("[*] press any key to exit...");
            Console.ReadKey();
        }
    }
}
