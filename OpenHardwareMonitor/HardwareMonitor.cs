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

        public ManagementObjectCollection Get()
        {
            string query = this.ToString();
            ManagementScope scope = new ManagementScope(this.ns);
            scope.Connect();
            using (ManagementObjectSearcher mos = new ManagementObjectSearcher(scope, new ObjectQuery(query)))
            {
                return mos.Get();
            }
        }

        public ManagementObject GetAt(int i)
        {
            using(ManagementObjectCollection moc = this.Get())
            {
                return getObjectAt(moc, i);
            }
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
                m.Dispose();
                i++;
            }
            return null;
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
        
        private string sensor_identifier;
        private string ns;

        internal Measure()
        {

        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            Rainmeter.API api = (Rainmeter.API)rm;

            this.ns = "root\\" + rm.ReadString("Namespace", DefaultNamespace);

            string hwType = rm.ReadString("HardwareType", "");
            string hwName = rm.ReadString("HardwareName", "");
            int hwIndex = rm.ReadInt("HardwareIndex", 0);

            string sType = rm.ReadString("SensorType", "");
            string sName = rm.ReadString("SensorName", "");
            int sIndex = rm.ReadInt("SensorIndex", 0);

            api.Log(API.LogType.Debug, String.Format("Hardware(type, name, index): ({0}, {1}, {2}), Sensor(type, name, index): ({3}, {4}, {5})", hwType, hwName, hwIndex, sType, sName, sIndex));

            WMIQuery hwQuery = new WMIQuery(this.ns, HardwareClass);
            if(hwType.Length > 0)
            {
                hwQuery.Where("HardwareType", hwType);
            }
            if (hwName.Length > 0)
            {
                hwQuery.Where("name", hwName);
            }
            api.Log(API.LogType.Debug, "Hardware Query: " + hwQuery.ToString());
            ManagementObject hardware = hwQuery.GetAt(hwIndex);

            if(hardware == null)
            {
                api.Log(API.LogType.Warning, "Hardware not found");
                this.sensor_identifier = null;
                return;
            }
            string hardware_identifier = (string) hardware.GetPropertyValue("Identifier");
            hardware.Dispose();

            WMIQuery sQuery = new WMIQuery(this.ns, SensorClass);
            sQuery.Where("Parent", hardware_identifier);
            if (sType.Length > 0)
            {
                sQuery.Where("SensorType", sType);
            }
            if (sName.Length > 0)
            {
                sQuery.Where("name", sName);
            }
            api.Log(API.LogType.Debug, "Sensor Query: " + sQuery.ToString());
            ManagementObject sensor = sQuery.GetAt(sIndex);

            if (sensor == null)
            {
                api.Log(API.LogType.Warning, "Sensor not found");
                this.sensor_identifier = null;
                return;
            }
            this.sensor_identifier = sensor.GetPropertyValue("Identifier").ToString();
            sensor.Dispose();
        }

        internal double Update()
        {
            double value = -1;

            WMIQuery wmiQuery = new WMIQuery(this.ns, SensorClass);
            wmiQuery.Where("Identifier", this.sensor_identifier);
            ManagementObject sensor = wmiQuery.GetAt(0);

            if(sensor != null)
            {
                value = Double.Parse(sensor.GetPropertyValue("Value").ToString());
                sensor.Dispose();
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
