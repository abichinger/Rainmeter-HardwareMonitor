using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Management;

namespace RainmeterOHM
{

    class WMIQuery{

        private string ns;
        private string table;
        private List<string> statements;

        internal WMIQuery(string ns, string table)
        {
            this.ns = ns;
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

        public ManagementObject GetAt(int index)
        {
            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(new ManagementScope(this.ns), new ObjectQuery(this.ToString())))
                using (ManagementObjectCollection moc = mos.Get())
                {
                    int i = 0;
                    foreach (ManagementObject m in moc)
                    {
                        try
                        {
                            if (index == i)
                                return m;

                            i++;
                        }
                        finally
                        {
                            m.Dispose();
                        }
                    }
                    return null;
                }
        }

        override
        public string ToString()
        {
            string where = (this.statements.Count > 0) ? "where "+ string.Join(" and ", this.statements.ToArray()) : "";
            return string.Format("SELECT * FROM {0} {1}", table, where);
        }

    }

    public class Measure
    {
        private const string DefaultNamespace = "OpenHardwareMonitor";
        private const string SensorClass = "Sensor";
        private const string HardwareClass = "Hardware";
        private const string wmi_root = "root";
        
        private string sensor_identifier;
        private string ns;

        API api;

        internal Measure()
        {

        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            api = rm;

            try
            {
                string hwType = rm.ReadString("HardwareType", "");
                string hwName = rm.ReadString("HardwareName", "");
                int hwIndex = rm.ReadInt("HardwareIndex", 0);

                string sType = rm.ReadString("SensorType", "");
                string sName = rm.ReadString("SensorName", "");
                int sIndex = rm.ReadInt("SensorIndex", 0);

                api.Log(API.LogType.Debug, String.Format("Hardware(type, name, index): ({0}, {1}, {2}), Sensor(type, name, index): ({3}, {4}, {5})", hwType, hwName, hwIndex, sType, sName, sIndex));

                this.ns = "root\\" + rm.ReadString("Namespace", DefaultNamespace);

                var bFoundNamespace = false;
                using (var nsClass = new ManagementClass(new ManagementScope(wmi_root), new ManagementPath("__namespace"), null))
                    using(var moCollection = nsClass.GetInstances())
                        foreach (var ns in moCollection)
                            if ((wmi_root + "\\" + ns["Name"].ToString()).ToLowerInvariant() == this.ns.ToLowerInvariant())
                                bFoundNamespace = true;

                if (!bFoundNamespace)
                {
                    api.Log(API.LogType.Error, "Cant find WMI namespace: " + this.ns);
                    return;
                }

                WMIQuery hwQuery = new WMIQuery(this.ns, HardwareClass);
                if (hwType.Length > 0)
                    hwQuery.Where("HardwareType", hwType);
                if (hwName.Length > 0)
                    hwQuery.Where("name", hwName);
                
                api.Log(API.LogType.Debug, "Hardware Query: " + hwQuery.ToString());

                string hardware_identifier;
                using (var hardware = hwQuery.GetAt(hwIndex))
                {
                    if (hardware == null)
                    {
                        api.Log(API.LogType.Error, "Cant find hardware");
                        this.sensor_identifier = null;
                        return;
                    }
                    hardware_identifier = (string)hardware.GetPropertyValue("Identifier");
                    api.Log(API.LogType.Debug, "Hardware Identifier: " + hardware_identifier.ToString());
                }

                WMIQuery sQuery = new WMIQuery(this.ns, SensorClass);
                sQuery.Where("Parent", hardware_identifier);
                if (sType.Length > 0)
                    sQuery.Where("SensorType", sType);
                if (sName.Length > 0)
                    sQuery.Where("name", sName);

                api.Log(API.LogType.Debug, "Sensor Query: " + sQuery.ToString());
                using (var sensor = sQuery.GetAt(sIndex))
                {
                    if (sensor == null)
                    {
                        api.Log(API.LogType.Error, "Cant find sensor");
                        this.sensor_identifier = null;
                        return;
                    }
                    this.sensor_identifier = sensor.GetPropertyValue("Identifier").ToString();
                    api.Log(API.LogType.Debug, "Sensor Identifier: " + sensor_identifier.ToString());
                }
            }
            catch (Exception ex)
            {
                api.Log(API.LogType.Error, "Fatal Error: " + ex.ToString());
            }
        }

        internal double Update()
        {
            double value = -1;

            if (this.sensor_identifier == null)
                return value;

            try {
                WMIQuery wmiQuery = new WMIQuery(this.ns, SensorClass);
                wmiQuery.Where("Identifier", this.sensor_identifier);
                using (var sensor = wmiQuery.GetAt(0))
                    if (sensor != null)
                        value = Double.Parse(sensor.GetPropertyValue("Value").ToString());
                       
            }
            catch (Exception ex)
            {
                api.Log(API.LogType.Error, "Fatal Error: " + ex.ToString());
            }

            return value;
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
