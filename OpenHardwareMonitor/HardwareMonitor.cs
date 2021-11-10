using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Management;

namespace RainmeterOHM
{

    class WMIQuery{

        private string table;
        private List<string> statements;

        internal WMIQuery(string table)
        {
            this.table = table;
            this.statements = new List<string>();
        }

        public void Where(string property, string value)
        {
            this.Where(property, string.Format("'{0}'", value), "=");
        }

        public void Where(string property, int value)
        {
            this.Where(property, value+"", "=");
        }

        public void Where(string property, string value, string op="=") 
        {
            this.statements.Add(string.Format("{0}{1}{2}", property, op, value));
        }
        
        override
        public string ToString()
        {
            string where = (this.statements.Count > 0) ? "where "+ string.Join(" and ", this.statements.ToArray()) : "";
            return string.Format("SELECT * FROM {0} {1}", table, where);
        }

    }

    public enum HardwareType
    {
        Mainboard,
        SuperIO,
        CPU,
        GpuNvidia,
        GpuAti,
        TBalancer,
        Heatmaster,
        HDD,
    }

    public enum SensorType
    {
        Voltage,
        Clock,
        Temperature,
        Load,
        Fan,
        Flow,
        Control,
        Level
    }

    public class Measure
    {
        private const string OpenHardwareMonitorNamespace = "root\\OpenHardwareMonitor";
        private const string SensorClass = "Sensor";
        private const string HardwareClass = "Hardware";
        

        
        private string sensor_identifier;

        internal Measure()
        {

        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            Rainmeter.API api = (Rainmeter.API)rm;

            string hwType = rm.ReadString("HardwareType", "");
            string hwName = rm.ReadString("HardwareName", "");
            int hwIndex = rm.ReadInt("HardwareIndex", 0);

            string sType = rm.ReadString("SensorType", "");
            string sName = rm.ReadString("SensorName", "");
            int sIndex = rm.ReadInt("SensorIndex", 0);

            api.Log(API.LogType.Debug, String.Format("Hardware(type, name, index): ({0}, {1}, {2}), Sensor(type, name, index): ({3}, {4}, {5})", hwType, hwName, hwIndex, sType, sName, sIndex));

            ManagementScope scope = new ManagementScope(OpenHardwareMonitorNamespace);
            scope.Connect();

            WMIQuery hwQuery = new WMIQuery(HardwareClass);
            if(hwType.Length > 0)
            {
                hwQuery.Where("HardwareType", hwType);
            }
            if (hwName.Length > 0)
            {
                hwQuery.Where("name", hwName);
            }
            string hwQueryStr = hwQuery.ToString();
            api.Log(API.LogType.Debug, "Hardware Query: "+hwQueryStr);
            ManagementObjectSearcher hwSearcher = new ManagementObjectSearcher(scope, new ObjectQuery(hwQueryStr));
            ManagementObject hardware = getObjectAt(hwSearcher.Get(), hwIndex);

            if(hardware == null)
            {
                api.Log(API.LogType.Warning, "Hardware not found");
                this.sensor_identifier = null;
                return;
            }

            string hardware_identifier = (string) hardware.GetPropertyValue("Identifier");

            WMIQuery sQuery = new WMIQuery(SensorClass);
            sQuery.Where("Parent", hardware_identifier);
            if (sType.Length > 0)
            {
                sQuery.Where("SensorType", sType);
            }
            if (sName.Length > 0)
            {
                sQuery.Where("name", sName);
            }
            string sQueryStr = sQuery.ToString();
            api.Log(API.LogType.Debug, "Sensor Query: " + sQueryStr);
            ManagementObjectSearcher sSearcher = new ManagementObjectSearcher(scope, new ObjectQuery(sQueryStr));
            ManagementObject sensor = getObjectAt(sSearcher.Get(), sIndex);

            if (sensor == null)
            {
                api.Log(API.LogType.Warning, "Sensor not found");
                this.sensor_identifier = null;
                return;
            }
            this.sensor_identifier = sensor.GetPropertyValue("Identifier").ToString();

        }

        internal ManagementObject getObjectAt(ManagementObjectCollection moc, int index)
        {
            int i = 0;
            foreach (ManagementObject m in moc)
            {
                if (index == i)
                {
                    return m;
                }
                i++;
            }
            return null;
        }

        internal double Update()
        {
            ManagementScope scope = new ManagementScope(OpenHardwareMonitorNamespace);
            scope.Connect();

            WMIQuery wmiQuery = new WMIQuery(SensorClass);
            wmiQuery.Where("Identifier", this.sensor_identifier);
            ObjectQuery query = new ObjectQuery(wmiQuery.ToString());
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

            foreach (ManagementObject sensor in searcher.Get())
            {
                return Double.Parse(sensor.GetPropertyValue("Value").ToString());
            }

            return -1;
        }

        internal string GetString()
        {
            /*switch (Type)
            {
                case MeasureType.String:
                    return string.Format("{0}.{1} (Build {2})", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, Environment.OSVersion.Version.Build);
            }*/

            // MeasureType.Major, MeasureType.Minor, and MeasureType.Number are
            // numbers. Therefore, null is returned here for them. This is to
            // inform Rainmeter that it can treat those types as numbers.

            return null;
        }
    }


    public static class Plugin
    {
#if DLLEXPORT_GETSTRING
        static IntPtr StringBuffer = IntPtr.Zero;
#endif

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();
            
#if DLLEXPORT_GETSTRING
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
#endif
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }
        
#if DLLEXPORT_GETSTRING
        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }
#endif

#if DLLEXPORT_EXECUTEBANG
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
#endif
    }
}
