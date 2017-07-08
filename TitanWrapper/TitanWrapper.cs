using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TitanWrapper.TitanOneApi;

namespace TitanWrapper
{
    public class Wrapper
    {
        TitanOneApi.TitanOne titanOneApi;

        private bool Loaded;
        private Thread titanWatcher;
        private Dictionary<int, Dictionary<string, dynamic>> buttonCallbacks = new Dictionary<int, Dictionary<string, dynamic>>();

        private TitanOne.InputType inputType = TitanOne.InputType.None;
        private TitanOne.GCMAPIStatus[] inputState = new TitanOne.GCMAPIStatus[30];

        private TitanOne.OutputType outputType = TitanOne.OutputType.None;
        private sbyte[] outputState = new sbyte[TitanOne.GCMAPIConstants.Output];

        public static readonly Dictionary<TitanOne.OutputType, Dictionary<int, int>> ButtonMappings = new Dictionary<TitanOne.OutputType, Dictionary<int, int>>()
        {
            { TitanOne.OutputType.PS3, new Dictionary<int, int>()
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

            { TitanOne.OutputType.XB360, new Dictionary<int, int>()
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

        public static readonly Dictionary<TitanOneApi.TitanOne.InputType, Dictionary<int, int>> ReverseButtonMappings = new Dictionary<TitanOne.InputType, Dictionary<int, int>>() {
            { TitanOne.InputType.PS3, new Dictionary<int, int>() },
            { TitanOne.InputType.XB360, new Dictionary<int, int>() },
        };

        public static readonly Dictionary<TitanOne.OutputType, TitanOne.InputType> outputToInputType = new Dictionary<TitanOne.OutputType, TitanOne.InputType>()
        {
            {TitanOne.OutputType.None, TitanOne.InputType.None },
            {TitanOne.OutputType.PS3, TitanOne.InputType.PS3 },
            {TitanOne.OutputType.PS4, TitanOne.InputType.PS4 },
            {TitanOne.OutputType.XB360, TitanOne.InputType.XB360 },
            {TitanOne.OutputType.XB1, TitanOne.InputType.XB1 },
        };

        public static readonly Dictionary<TitanOne.InputType, TitanOne.OutputType> inputToOutputType = new Dictionary<TitanOne.InputType, TitanOne.OutputType>()
        {
            {TitanOne.InputType.None, TitanOne.OutputType.None },
            {TitanOne.InputType.PS3, TitanOne.OutputType.PS3 },
            {TitanOne.InputType.PS4, TitanOne.OutputType.PS4 },
            {TitanOne.InputType.XB360, TitanOne.OutputType.XB360 },
            {TitanOne.InputType.XB1, TitanOne.OutputType.XB1 },
            {TitanOne.InputType.WII, TitanOne.OutputType.None },
        };

        public Wrapper()
        {
            titanOneApi = new TitanOneApi.TitanOne();
            titanOneApi.Init();
            Loaded = titanOneApi.Load();

            if (Loaded)
            {
                if (!titanOneApi.IsConnected())
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
                    while ((outputType == TitanOne.OutputType.None || inputType == TitanOne.InputType.None) && watch.ElapsedMilliseconds < 3000)
                    {
                        var report = titanOneApi.GetReport();
                        inputType = titanOneApi.GetInputType();
                        outputType = titanOneApi.GetOutputType();
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

        public string InputTypeToString(TitanOne.InputType type)
        {
            return Enum.GetName(typeof(TitanOne.InputType), type);
        }

        public string OutputTypeToString(TitanOne.OutputType type)
        {
            return Enum.GetName(typeof(TitanOne.OutputType), type);
        }

        public bool SetButton(int button, int state)
        {
            var slot = ButtonMappings[outputType][button];
            outputState[slot] = (sbyte)state;
            titanOneApi.Write(outputState);
            return true;
        }

        public bool SubscribeButton(int button, dynamic callback, string guid = "0")
        {
            if (!buttonCallbacks.ContainsKey(button))
            {
                buttonCallbacks[button] = new Dictionary<string, dynamic>();
            }
            buttonCallbacks[button][guid] = callback;
            return true;
        }

        // Destructor, fires on exit
        ~Wrapper()
        {
            if (Loaded)
            {
                titanOneApi.Unload();
                Console.WriteLine("Unloaded API");
            }
            titanOneApi.UnloadDll();
            Console.WriteLine("Unloaded DLL");
        }

        private void TitanWatcher()
        {
            TitanOne.GCMAPIReport report = new TitanOne.GCMAPIReport();

            while (true)
            {
                try
                {
                    if (!titanOneApi.Read(ref report))
                    {
                        if (!titanOneApi.IsConnected())
                        {
                            //break;
                            throw new Exception();
                        }
                    }

                    for (byte slot = 0; slot < TitanOne.GCMAPIConstants.Input; slot++)
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


    }

}