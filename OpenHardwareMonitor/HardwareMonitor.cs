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
            value = value.Replace(@"\", @"\\").Replace(@"'", @"\'");
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
        private const string wmiROOT = "root";
        
        private string sensorID;
        private string ns;
        private string sensorValueName;

        API api;

        internal Measure()
        {

        }

        internal void Reload(Rainmeter.API rm)
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

                this.sensorValueName = rm.ReadString("SensorValueName", "Value");

                api.Log(API.LogType.Debug, String.Format("Hardware(type, name, index): ({0}, {1}, {2}), Sensor(type, name, index): ({3}, {4}, {5})", hwType, hwName, hwIndex, sType, sName, sIndex));

                this.ns = wmiROOT + "\\" + rm.ReadString("Namespace", DefaultNamespace);

                WMIQuery hwQuery = new WMIQuery(this.ns, HardwareClass);
                if (hwType.Length > 0)
                    hwQuery.Where("HardwareType", hwType);
                if (hwName.Length > 0)
                    hwQuery.Where("name", hwName);
                
                api.Log(API.LogType.Debug, "Hardware Query: " + hwQuery.ToString());

                string hardwareID;
                using (var hardware = hwQuery.GetAt(hwIndex))
                {
                    if (hardware == null)
                    {
                        api.Log(API.LogType.Error, "can't find hardware -> check hardware filter, check if OHM/LHM is running");
                        this.sensorID = null;
                        return;
                    }
                    hardwareID = (string)hardware.GetPropertyValue("Identifier");
                    api.Log(API.LogType.Debug, "Hardware Identifier: " + hardwareID.ToString());
                }

                WMIQuery sQuery = new WMIQuery(this.ns, SensorClass);
                sQuery.Where("Parent", hardwareID);
                if (sType.Length > 0)
                    sQuery.Where("SensorType", sType);
                if (sName.Length > 0)
                    sQuery.Where("name", sName);

                api.Log(API.LogType.Debug, "Sensor Query: " + sQuery.ToString());
                using (var sensor = sQuery.GetAt(sIndex))
                {
                    if (sensor == null)
                    {
                        api.Log(API.LogType.Error, "can't find sensor -> check sensor filter");
                        this.sensorID = null;
                        return;
                    }

                    bool propertyFound = false;
                    foreach(PropertyData prop in sensor.Properties){
                        if (prop.Name == this.sensorValueName) {
                            propertyFound = true;
                        }
                    }
                    if (propertyFound == false) {
                        api.Log(API.LogType.Error, "sensor has no value named: " + this.sensorValueName);
                        return;
                    }

                    this.sensorID = sensor.GetPropertyValue("Identifier").ToString();
                    api.Log(API.LogType.Debug, "Sensor Identifier: " + sensorID.ToString());
                }
            }
            catch (Exception ex)
            {
                api.Log(API.LogType.Error, "Fatal Error: " + ex.Message);
                api.Log(API.LogType.Debug, ex.ToString());
            }
        }

        internal double Update()
        {
            double value = -1;

            if (this.sensorID == null)
                this.Reload(this.api);

            if (this.sensorID == null)
                return value;

            try {
                WMIQuery wmiQuery = new WMIQuery(this.ns, SensorClass);
                wmiQuery.Where("Identifier", this.sensorID);
                using (var sensor = wmiQuery.GetAt(0))
                    if (sensor != null)
                        value = Double.Parse(sensor.GetPropertyValue(this.sensorValueName).ToString());
            }
            catch (Exception ex)
            {
                api.Log(API.LogType.Error, "Fatal Error: " + ex.Message);
                api.Log(API.LogType.Debug, ex.ToString());
            }

            return value;
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
            measure.Reload(new Rainmeter.API(rm));
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
