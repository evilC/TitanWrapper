using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TitanWrapper.TitanOneApi;

namespace TitanWrapper
{
    public class Wrapper
    {
        TitanOneApi.TitanOne titanOneApi;

        private Dictionary<int, Dictionary<string, dynamic>> buttonCallbacks = new Dictionary<int, Dictionary<string, dynamic>>();

        private TitanOne.InputType inputType = TitanOne.InputType.None;

        private TitanOne.OutputType outputType = TitanOne.OutputType.None;

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

        public static readonly Dictionary<TitanOne.InputType, Dictionary<int, int>> ReverseButtonMappings = new Dictionary<TitanOne.InputType, Dictionary<int, int>>() {
            { TitanOne.InputType.PS3, new Dictionary<int, int>() },
            { TitanOne.InputType.XB360, new Dictionary<int, int>() },
        };

        public Wrapper()
        {
            titanOneApi = new TitanOne(new Action<int, int>(SlotChanged));
            if (!titanOneApi.Init())
            {
                throw new Exception("Could not load gcdapi.dll and it's functions");
            }
            
            if (!titanOneApi.IsConnected())
            {
                // ToDo: Does not seem to detect cable not plugged in
                throw new Exception("Could not connect to Titan One device");
            }

            foreach (var type in ButtonMappings)
            {
                foreach (var buttonMapping in type.Value)
                {
                    try
                    {
                        var iType = TitanOne.outputToInputType[type.Key];
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

            if (inputType == TitanOne.InputType.None && outputType == TitanOne.OutputType.None)
            {
                throw new Exception("No input or output devices detected");
            }
            if (inputType != TitanOne.InputType.None)
            {
                Console.WriteLine(String.Format("Input Type is: {0}", InputTypeToString(inputType)));
            }
            if (outputType != TitanOne.OutputType.None)
            {
                Console.WriteLine(String.Format("Output Type is: {0}", OutputTypeToString(outputType)));
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
            titanOneApi.SetOutputSlot(slot, state);
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

        private void SlotChanged(int slot, int value)
        {
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