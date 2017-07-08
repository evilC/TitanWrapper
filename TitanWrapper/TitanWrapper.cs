using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class TitanWrapper
{
    private IntPtr hModule;
    private bool Loaded;
    private Thread titanWatcher;
    private dynamic _callback;
    private Dictionary<int, Dictionary<string, dynamic>> buttonCallbacks = new Dictionary<int, Dictionary<string, dynamic>>();

    sbyte[] outputState = new sbyte[GCMAPIConstants.Output];
    OutputType outputType = OutputType.None;
    InputType inputType = InputType.None;

    GCMAPIStatus[] inputState = new GCMAPIStatus[30];

    public enum InputType
    {
        None,
        PS3 = 0x10,
        XB360 = 0x20,
        WII = 0x30,
        PS4 = 0x40,
        XB1 = 0x50
    };

    public enum OutputType
    {
        None, PS3, XB360, PS4, XB1
    }

    public static readonly Dictionary<OutputType, Dictionary<int, int>> ButtonMappings = new Dictionary<OutputType, Dictionary<int, int>>()
    {
        { OutputType.PS3, new Dictionary<int, int>()
            {
                { 1, 19 },
                { 2, 18 },
                { 3, 20 },
                { 4, 17 },
                { 5, 6 },
                { 6, 3 },
                { 7, 7 },
                { 8, 4 },
                { 9, 8 },
                { 10, 5 },
                { 11, 1 },
                { 12, 2 },
                { 13, 0 },
            }
        },

        { OutputType.XB360, new Dictionary<int, int>()
            {
                { 1, 19 },
                { 2, 18 },
                { 3, 20 },
                { 4, 17 },
                { 5, 6 },
                { 6, 3 },
                { 9, 8 },
                { 10, 5 },
                { 11, 1 },
                { 12, 2 },
                { 13, 0 },
            }
        },
    };

    public static readonly Dictionary<InputType, Dictionary<int, int>> ReverseButtonMappings = new Dictionary<InputType, Dictionary<int, int>>() {
        { InputType.PS3, new Dictionary<int, int>() },
        { InputType.XB360, new Dictionary<int, int>() },
    };

    public static readonly Dictionary<OutputType, InputType> outputToInputType = new Dictionary<OutputType, InputType>()
    {
        {OutputType.None, InputType.None },
        {OutputType.PS3, InputType.PS3 },
        {OutputType.PS4, InputType.PS4 },
        {OutputType.XB360, InputType.XB360 },
        {OutputType.XB1, InputType.XB1 },
    };

    public static readonly Dictionary<InputType, OutputType> inputToOutputType = new Dictionary<InputType, OutputType>()
    {
        {InputType.None, OutputType.None },
        {InputType.PS3, OutputType.PS3 },
        {InputType.PS4, OutputType.PS4 },
        {InputType.XB360, OutputType.XB360 },
        {InputType.XB1, OutputType.XB1 },
        {InputType.WII, OutputType.None },
    };

    GCDAPI_Load Load;
    GCDAPI_Unload Unload;
    GCAPI_IsConnected IsConnected;
    GCAPI_GetFWVer GetFWVer;
    GCAPI_Read Read;
    GCAPI_Write Write;
    GCAPI_GetTimeVal GetTimeVal;
    GCAPI_CalcPressTime CalcPressTime;

    public TitanWrapper()
    {
        String Working = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        hModule = LoadLibrary(Path.Combine(Working, "gcdapi.dll"));

        Console.WriteLine("Loaded DLL");

        Load = GetFunction<GCDAPI_Load>(hModule, "gcdapi_Load");
        Console.WriteLine((Load == null ? "Failed to obtain function '" : "Obtained function '") + "GCDAPI_Load" + "'");

        Unload = GetFunction<GCDAPI_Unload>(hModule, "gcdapi_Unload");
        Console.WriteLine((Unload == null ? "Failed to obtain function '" : "Obtained function '") + "GCDAPI_Unload" + "'");

        IsConnected = GetFunction<GCAPI_IsConnected>(hModule, "gcapi_IsConnected");
        Console.WriteLine((IsConnected == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_IsConnected" + "'");

        GetFWVer = GetFunction<GCAPI_GetFWVer>(hModule, "gcapi_GetFWVer");
        Console.WriteLine((GetFWVer == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_GetFWVer" + "'");

        Read = GetFunction<GCAPI_Read>(hModule, "gcapi_Read");
        Console.WriteLine((Read == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_Read" + "'");

        Write = GetFunction<GCAPI_Write>(hModule, "gcapi_Write");
        Console.WriteLine((Write == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_Write" + "'");

        GetTimeVal = GetFunction<GCAPI_GetTimeVal>(hModule, "gcapi_GetTimeVal");
        Console.WriteLine((GetTimeVal == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_GetTimeVal" + "'");

        CalcPressTime = GetFunction<GCAPI_CalcPressTime>(hModule, "gcapi_CalcPressTime");
        Console.WriteLine((CalcPressTime == null ? "Failed to obtain function '" : "Obtained function '") + "GCAPI_CalcPressTime" + "'");

        Loaded = Load();

        if (Loaded)
        {
            if (!IsConnected())
            {
                Console.WriteLine("Could not connect");
            }
            else
            {
                foreach (var type in ButtonMappings)
                {
                    foreach (var buttonMapping in type.Value)
                    {
                        try
                        {
                            var iType = outputToInputType[type.Key];
                            ReverseButtonMappings[iType][buttonMapping.Value] = buttonMapping.Key;
                        }
                        catch
                        {

                        }
                    }
                }

                Stopwatch watch = new Stopwatch();
                watch.Start();
                while ((outputType == OutputType.None || inputType == InputType.None) && watch.ElapsedMilliseconds < 3000)
                {
                    var report = GetReport();
                    inputType = GetInputType();
                    outputType = GetOutputType();
                    Thread.Sleep(10);
                }
                watch.Stop();
                
                Console.WriteLine(String.Format("Input Type is: {0}", InputTypeToString(inputType)));
                Console.WriteLine(String.Format("Output Type is: {0}", OutputTypeToString(outputType)));

                titanWatcher = new Thread(TitanWatcher);
                titanWatcher.Start();

            }
        }
        else
        {
            Console.WriteLine("GCDAP_Load failed");
        }
    }

    public InputType GetInputType()
    {
        var report = GetReport();
        return (InputType)report.Controller;
    }

    public string InputTypeToString(InputType type)
    {
        return Enum.GetName(typeof(InputType), type);
    }

    public OutputType GetOutputType()
    {
        var report = GetReport();
        return (OutputType)report.Console;
    }

    public string OutputTypeToString(OutputType type)
    {
        return Enum.GetName(typeof(OutputType), type);
    }

    private GCMAPIReport GetReport()
    {
        GCMAPIReport report = new GCMAPIReport();
        Read(ref report);
        return report;
    }

    public bool SetButton(int button, int state)
    {
        var slot = ButtonMappings[outputType][button];
        outputState[slot] = (sbyte)state;
        Write(outputState);
        return true;
    }

    public bool SubscribeButton(int button, dynamic callback, string guid = "0")
    {
        _callback = callback;
        if (!buttonCallbacks.ContainsKey(button))
        {
            buttonCallbacks[button] = new Dictionary<string, dynamic>();
        }
        buttonCallbacks[button][guid] = callback;
        return true;
    }

    // Destructor, fires on exit
    ~TitanWrapper()
    {
        if (Loaded)
        {
            Unload();
            Console.WriteLine("Unloaded API");
        }
        FreeLibrary(hModule);
        Console.WriteLine("Unloaded DLL");
    }

    private void TitanWatcher()
    {
        GCMAPIReport report = new GCMAPIReport();

        while (true)
        {
            try
            {
                if (!Read(ref report))
                {
                    if (!IsConnected())
                    {
                        //break;
                        throw new Exception();
                    }
                }

                for (byte slot = 0; slot < GCMAPIConstants.Input; slot++)
                {
                    sbyte value = report.Input[slot].Value;

                    if (value != inputState[slot].Value)
                    {
                        SlotChanged(slot, value);
                    }
                    //Console.WriteLine(String.Format("Index: {0}, Value: {1}", slot, value));
                }
            }
            catch
            {
                //break;
            }
            finally
            {
                Thread.Sleep(1);
            }
        }
    }

    private void SlotChanged(int slot, int value)
    {
        inputState[slot].Value = (sbyte)value;
        int button;
        try
        {
            button = ReverseButtonMappings[inputType][slot];
        }
        catch
        {
            return;
        }
        if (buttonCallbacks.ContainsKey(button))
        {
            foreach (var callback in buttonCallbacks[button])
            {
                callback.Value(value);
            }
        }

        //Console.WriteLine(String.Format("Slot {0} changed to: {1}", slot, value));
    }

    private static T GetFunction<T>(IntPtr hModule, String procName)
    {
        try
        {
            return (T)(object)Marshal.GetDelegateForFunctionPointer(GetProcAddress(hModule, procName), typeof(T));
        }
        catch
        {
            return default(T);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GCDAPI_Load();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GCDAPI_Unload();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GCAPI_IsConnected();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate ushort GCAPI_GetFWVer();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate ushort GPPAPI_DevicePID();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GCAPI_Read([In, Out] ref GCMAPIReport Report);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool GCAPI_Write(sbyte[] Output);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint GCAPI_GetTimeVal();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint GCAPI_CalcPressTime(uint Button);

#pragma warning disable 0649

    private struct GCMAPIConstants
    {
        public const int Input = 30;
        public const int Output = 36;
    }

    private struct GCMAPIStatus
    {
        public sbyte Value;
        public sbyte Previous;
        public int Holding;
    }

    private struct GCMAPIReport
    {
        public byte Console;
        public byte Controller;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] LED;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Rumble;
        public byte Battery;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = GCMAPIConstants.Input, ArraySubType = UnmanagedType.Struct)]
        public GCMAPIStatus[] Input;
    }

#pragma warning restore 0649

}

