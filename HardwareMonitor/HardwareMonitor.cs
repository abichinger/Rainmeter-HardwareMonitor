using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;
using OpenHardwareMonitor.Hardware;
using System.Text.RegularExpressions;

namespace HardwareMonitor
{
    public class Measure
    {

        private HardwareType ht = HardwareType.CPU;
        private SensorType st = SensorType.Clock;
        private String hName = null, sName = null;
        private Computer computer;

        internal Measure()
        {
            this.computer = new Computer() { MainboardEnabled = true, CPUEnabled = true, GPUEnabled = true, HDDEnabled = true, RAMEnabled = true };
        }

        internal void Reload(Rainmeter.API rm, ref double maxValue)
        {
            string type = rm.ReadString("Type", "").Trim();

            string hPattern = "\"[^\"]*\"";
            string sPattern = "\"[^\"]*\"$";

            hName = Regex.Match(type, hPattern).Value;
            sName = Regex.Match(type, sPattern).Value;
            if (hName != null && hName.Equals(sName))
                hName = null;
            if (sName.Length <= 0)
                sName = null;
            if (hName != null)
                hName = Regex.Replace(hName, "\"", "");
            if (sName != null)
                sName = Regex.Replace(sName, "\"", "");

            type = Regex.Replace(type, hPattern, "");

            foreach (string s in type.Split(' '))
            {
                switch (s.ToLowerInvariant())
                {
                    case "cpu":
                        ht = HardwareType.CPU;
                        break;
                    case "gpu":
                        ht = HardwareType.GpuNvidia;
                        break;
                    case "ram":
                        ht = HardwareType.RAM;
                        break;
                    case "hdd":
                        ht = HardwareType.HDD;
                        break;
                    case "mainboard":
                        ht = HardwareType.Mainboard;
                        break;
                    case "load":
                        st = SensorType.Load;
                        break;
                    case "clock":
                        st = SensorType.Clock;
                        break;
                    case "temp":
                        st = SensorType.Temperature;
                        break;
                    case "fan":
                        st = SensorType.Fan;
                        break;
                    case "power":
                        st = SensorType.Power;
                        break;
                    case "control":
                        st = SensorType.Control;
                        break;
                    case "data":
                        st = SensorType.Data;
                        break;
                    case "voltage":
                        st = SensorType.Voltage;
                        break;
                }
            }

            Rainmeter.API.Log(API.LogType.Notice, ht + " " + hName + " " + st + " " + sName);
        }

        internal double Update()
        {
            computer.Open();

            foreach (var hardware in computer.Hardware)
            {
                if (hardware.HardwareType == ht || (hardware.HardwareType == HardwareType.GpuAti && ht == HardwareType.GpuNvidia))
                {
                    if (hName == null || hardware.Name.ToLower().Equals(hName.ToLower()))
                    {
                        ISensor resSensor = null;
                        hardware.Update();
                        foreach (IHardware subHardware in hardware.SubHardware)
                        {
                            subHardware.Update();
                            if((resSensor = findSensor(subHardware)) != null)
                            {
                                return (double)resSensor.Value;
                            }
                        }
                        if ((resSensor = findSensor(hardware)) != null)
                        {
                            return (double)resSensor.Value;
                        }
                    }
                }
            }

            return -1;
        }

        internal ISensor findSensor(IHardware hardware)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == st)
                {
                    if (sName == null || sensor.Name.ToLower().Equals(sName.ToLower()))
                    {
                        return sensor;
                    }
                }
            }
            return null;
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
