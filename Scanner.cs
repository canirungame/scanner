using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace CanIRunGame
{
    static class Scanner
    {
        static readonly string[] SkipGpu = { "microsoft basic", "virtual", "remote", "parsec", "meta ", "displaylink", "indirect" };
        static readonly int[] LaptopChassis = { 8, 9, 10, 11, 14, 30, 31, 32 };
        static readonly int[] DesktopChassis = { 3, 4, 5, 6, 7, 13, 15, 16, 23, 24, 35, 36 };
        const string DisplayClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

        static List<ManagementBaseObject> Query(string wql)
        {
            List<ManagementBaseObject> rows = new List<ManagementBaseObject>();
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql))
                {
                    foreach (ManagementBaseObject o in searcher.Get()) rows.Add(o);
                }
            }
            catch (Exception)
            {
            }
            return rows;
        }

        static string Str(ManagementBaseObject o, string name)
        {
            object v = null;
            try { v = o[name]; } catch (Exception) { }
            return v == null ? "" : Regex.Replace(v.ToString().Trim(), @"\s+", " ");
        }

        static double Num(ManagementBaseObject o, string name)
        {
            object v = null;
            try { v = o[name]; } catch (Exception) { }
            if (v == null) return 0;
            try { return Convert.ToDouble(v); } catch (Exception) { return 0; }
        }

        static double Gb(double bytes)
        {
            return Math.Round(bytes / 1073741824.0, 1);
        }

        public static Dictionary<string, object> Collect()
        {
            Dictionary<string, object> specs = new Dictionary<string, object>();
            specs["cpu"] = ScanCpu();
            specs["ram_gb"] = ScanRamTotal();
            specs["ram"] = ScanRam();
            specs["gpus"] = ScanGpus();
            specs["storage"] = ScanStorage();
            specs["os"] = ScanOs();
            specs["display"] = ScanDisplay();
            specs["device_type"] = ScanDeviceType();
            return specs;
        }

        static Dictionary<string, object> ScanCpu()
        {
            List<ManagementBaseObject> rows = Query("SELECT Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed FROM Win32_Processor");
            Dictionary<string, object> cpu = new Dictionary<string, object>();
            cpu["name"] = rows.Count > 0 ? Str(rows[0], "Name") : "";
            cpu["cores"] = (int)rows.Sum(r => Num(r, "NumberOfCores"));
            cpu["threads"] = (int)rows.Sum(r => Num(r, "NumberOfLogicalProcessors"));
            cpu["max_mhz"] = rows.Count > 0 ? (int)Num(rows[0], "MaxClockSpeed") : 0;
            return cpu;
        }

        static double ScanRamTotal()
        {
            List<ManagementBaseObject> rows = Query("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            return rows.Count > 0 ? Gb(Num(rows[0], "TotalPhysicalMemory")) : 0;
        }

        static Dictionary<string, object> ScanRam()
        {
            List<ManagementBaseObject> rows = Query("SELECT Capacity,Speed,ConfiguredClockSpeed,SMBIOSMemoryType FROM Win32_PhysicalMemory");
            if (rows.Count == 0) rows = Query("SELECT Capacity,Speed,SMBIOSMemoryType FROM Win32_PhysicalMemory");
            Dictionary<string, object> ram = new Dictionary<string, object>();
            ram["sticks"] = rows.Count;
            int speed = 0, rated = 0;
            if (rows.Count > 0)
            {
                speed = rows.Min(r => { double c = Num(r, "ConfiguredClockSpeed"); return (int)(c > 0 ? c : Num(r, "Speed")); });
                rated = rows.Max(r => (int)Num(r, "Speed"));
            }
            ram["speed_mhz"] = speed;
            ram["rated_mhz"] = rated;
            string type = "";
            if (rows.Count > 0)
            {
                int code = (int)Num(rows[0], "SMBIOSMemoryType");
                if (code == 26) type = "DDR4";
                else if (code == 34) type = "DDR5";
                else if (code == 24) type = "DDR3";
            }
            ram["type"] = type;
            return ram;
        }

        static Dictionary<string, double> ReadRegistryVram()
        {
            Dictionary<string, double> result = new Dictionary<string, double>();
            try
            {
                using (RegistryKey root = Registry.LocalMachine.OpenSubKey(DisplayClass))
                {
                    if (root == null) return result;
                    foreach (string name in root.GetSubKeyNames())
                    {
                        if (!Regex.IsMatch(name, @"^\d{4}$")) continue;
                        using (RegistryKey key = root.OpenSubKey(name))
                        {
                            if (key == null) continue;
                            string desc = key.GetValue("DriverDesc") as string;
                            object mem = key.GetValue("HardwareInformation.qwMemorySize");
                            long bytes = 0;
                            if (mem is long) bytes = (long)mem;
                            else if (mem is byte[])
                            {
                                byte[] raw = new byte[8];
                                Array.Copy((byte[])mem, raw, Math.Min(8, ((byte[])mem).Length));
                                bytes = BitConverter.ToInt64(raw, 0);
                            }
                            if (desc != null && bytes > 0) result[desc.ToLowerInvariant()] = Gb(bytes);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            return result;
        }

        static List<object> ScanGpus()
        {
            Dictionary<string, double> registry = ReadRegistryVram();
            List<ManagementBaseObject> rows = Query("SELECT Name,AdapterRAM,DriverVersion,DriverDate FROM Win32_VideoController");
            List<object> gpus = new List<object>();
            foreach (ManagementBaseObject row in rows)
            {
                string name = Str(row, "Name");
                string lower = name.ToLowerInvariant();
                if (name.Length == 0 || SkipGpu.Any(s => lower.Contains(s))) continue;
                double vram = 0;
                if (!registry.TryGetValue(lower, out vram)) vram = Gb(Num(row, "AdapterRAM"));
                string date = "";
                string rawDate = Str(row, "DriverDate");
                if (rawDate.Length > 0)
                {
                    try { date = ManagementDateTimeConverter.ToDateTime(rawDate).ToString("yyyy-MM-dd"); } catch (Exception) { }
                }
                Dictionary<string, object> gpu = new Dictionary<string, object>();
                gpu["name"] = name;
                gpu["vram_gb"] = vram > 0 ? (object)vram : null;
                gpu["driver_version"] = Str(row, "DriverVersion");
                gpu["driver_date"] = date;
                gpus.Add(gpu);
            }
            return gpus;
        }

        static Dictionary<string, object> ScanStorage()
        {
            long bestFree = -1, bestTotal = 0;
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.Fixed || !drive.IsReady) continue;
                    if (drive.AvailableFreeSpace > bestFree)
                    {
                        bestFree = drive.AvailableFreeSpace;
                        bestTotal = drive.TotalSize;
                    }
                }
            }
            catch (Exception)
            {
            }
            Dictionary<string, object> storage = new Dictionary<string, object>();
            storage["free_gb"] = bestFree >= 0 ? (object)Gb(bestFree) : null;
            storage["total_gb"] = bestFree >= 0 ? (object)Gb(bestTotal) : null;
            return storage;
        }

        static Dictionary<string, object> ScanOs()
        {
            List<ManagementBaseObject> rows = Query("SELECT Caption,BuildNumber FROM Win32_OperatingSystem");
            string name = "Windows";
            string build = "";
            if (rows.Count > 0)
            {
                name = Str(rows[0], "Caption").Replace("Microsoft ", "");
                build = Str(rows[0], "BuildNumber");
                int number;
                if (int.TryParse(build, out number) && number >= 22000 && name.Contains("Windows 10")) name = name.Replace("Windows 10", "Windows 11");
            }
            Dictionary<string, object> os = new Dictionary<string, object>();
            os["name"] = name;
            os["build"] = build;
            return os;
        }

        static Dictionary<string, object> ScanDisplay()
        {
            List<ManagementBaseObject> rows = Query("SELECT CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate FROM Win32_VideoController");
            double width = 0, height = 0, hz = 0;
            foreach (ManagementBaseObject row in rows)
            {
                double w = Num(row, "CurrentHorizontalResolution");
                double h = Num(row, "CurrentVerticalResolution");
                if (w * h > width * height)
                {
                    width = w;
                    height = h;
                }
                hz = Math.Max(hz, Num(row, "CurrentRefreshRate"));
            }
            Dictionary<string, object> display = new Dictionary<string, object>();
            display["width"] = (int)width;
            display["height"] = (int)height;
            display["refresh_hz"] = (int)hz;
            return display;
        }

        static string ScanDeviceType()
        {
            HashSet<int> types = new HashSet<int>();
            foreach (ManagementBaseObject row in Query("SELECT ChassisTypes FROM Win32_SystemEnclosure"))
            {
                try
                {
                    ushort[] values = row["ChassisTypes"] as ushort[];
                    if (values != null) foreach (ushort v in values) types.Add(v);
                }
                catch (Exception)
                {
                }
            }
            bool battery = Query("SELECT Name FROM Win32_Battery").Count > 0;
            if (types.Any(t => LaptopChassis.Contains(t))) return "laptop";
            if (battery && !types.Any(t => DesktopChassis.Contains(t))) return "laptop";
            return "desktop";
        }

        public static Dictionary<string, object> Upload(string api, Dictionary<string, object> specs)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
            byte[] body = Encoding.UTF8.GetBytes(Json.Write(specs, false));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(api.TrimEnd('/') + "/api/scan");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.UserAgent = "CanIRunGame-Scanner/2.0";
            request.Timeout = 20000;
            request.ContentLength = body.Length;
            using (Stream stream = request.GetRequestStream()) stream.Write(body, 0, body.Length);
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return new JavaScriptSerializer().DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                }
            }
            catch (WebException ex)
            {
                string message = ex.Message;
                if (ex.Response != null)
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream(), Encoding.UTF8))
                        {
                            Dictionary<string, object> err = new JavaScriptSerializer().DeserializeObject(reader.ReadToEnd()) as Dictionary<string, object>;
                            if (err != null && err.ContainsKey("error")) message = Convert.ToString(err["error"]);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                throw new InvalidOperationException(message);
            }
        }
    }
}
