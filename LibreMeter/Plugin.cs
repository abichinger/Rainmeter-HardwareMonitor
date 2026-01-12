using System;
using System.Runtime.InteropServices;
using Rainmeter;
using System.Threading.Tasks;

namespace LibreHardwareMonitorPlugin
{
    public class Measure
    {
        private LibreHardwareMonitorClient _client;
        private string _sensorId;
        private API _api;

        internal Measure()
        {
            _client = new LibreHardwareMonitorClient();
        }

        internal void Reload(Rainmeter.API rm)
        {
            _api = rm;

            try
            {
                _sensorId = rm.ReadString("SensorId", "");

                if (string.IsNullOrEmpty(_sensorId))
                {
                    _api.Log(API.LogType.Error, "SensorId is not specified in the measure configuration.");
                }
            }
            catch (Exception ex)
            {
                _api.Log(API.LogType.Error, "Error during Reload: " + ex.Message);
            }
        }

        internal double Update()
        {
            if (string.IsNullOrEmpty(_sensorId))
            {
                _api.Log(API.LogType.Error, "SensorId is not set. Reload the measure.");
                return -1;
            }

            try
            {
                var task = Task.Run(async () => await _client.GetSensorAsync(_sensorId));
                var result = task.Result;

                if (!result.ok)
                {
                    _api.Log(API.LogType.Error, $"Failed to fetch sensor data: {result.error} (SensorId: {_sensorId})");
                    return -1;
                }

                return result.value ?? -1;
            }
            catch (AggregateException aggEx)
            {
                foreach (var ex in aggEx.InnerExceptions)
                {
                    _api.Log(API.LogType.Error, $"Error during Update (SensorId: {_sensorId}): {ex.Message}\n{ex.StackTrace}");
                }
                return -1;
            }
            catch (Exception ex)
            {
                _api.Log(API.LogType.Error, $"Error during Update (SensorId: {_sensorId}): {ex.Message}\n{ex.StackTrace}");
                return -1;
            }
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