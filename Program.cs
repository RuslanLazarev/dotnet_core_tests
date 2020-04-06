using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace HardwareDemo
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var modMetrics = new ModuleMetrics();
            var metrics = modMetrics.GetMemoryMetrics();

            var driveMetrics = modMetrics.GetDriveMetrics(0);

            Console.WriteLine("Total: " + metrics.Total);
            Console.WriteLine("Used : " + metrics.Used);
            //Console.WriteLine("Free : " + metrics.Free);
            Console.WriteLine();
            Console.WriteLine("Total: " + driveMetrics.TotalSize);
            Console.WriteLine("Used : " + driveMetrics.UsedSpace);
            //Console.WriteLine("Free : " + driveMetrics.FreeSpace);
            Console.WriteLine();

            //PerformanceCounter total_cpu;
            var cpuUsage = GetCpuUsageLinux();

            Console.WriteLine($"CPU load: {cpuUsage}%");

        }
        private static async Task<double> GetCpuUsageForProcess()
        {
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            await Task.Delay(500);

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            return cpuUsageTotal * 100;
        }

        private static double GetCpuUsageLinux()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "/bin/bash";
            info.Arguments = "top";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine(memory);

            return 0;
        }

        private static double GetCpuUsage()
        {
            var output = "";
            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "cpu get loadpercentage";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }
            var lines = output.Trim().Split("\n");

            var cpuPercentage = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);
            return Math.Round(double.Parse(cpuPercentage[0]), 0); ;

        }

    }
    public class ModuleMetrics
    {
        public MemoryMetrics GetMemoryMetrics()
        {
            bool isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            if (isUnix)
                return GetUnixMetrics();

            return GetWindowsMetrics();
        }
        private MemoryMetrics GetWindowsMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            metrics.Used = metrics.Total - metrics.Free;
            return metrics;
        }

        private MemoryMetrics GetUnixMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo("free -m");
            info.FileName = "/bin/bash";
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = double.Parse(memory[1]);
            metrics.Used = double.Parse(memory[2]);
            metrics.Free = double.Parse(memory[3]);

            return metrics;
        }

        public DriveMetrics GetDriveMetrics(int driveNumber)
        {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            var metrics = new DriveMetrics();
            metrics.TotalSize = (double)allDrives[driveNumber].TotalSize;
            metrics.FreeSpace = (double)allDrives[driveNumber].AvailableFreeSpace;
            metrics.UsedSpace = metrics.TotalSize - metrics.FreeSpace;

            return metrics;
            
        }

        

    }
    public class MemoryMetrics
    {
        public double Total;
        public double Used;
        public double Free;
    }
    public class DriveMetrics
    {
        public double TotalSize;
        public double UsedSpace;
        public double FreeSpace;
    }

}
