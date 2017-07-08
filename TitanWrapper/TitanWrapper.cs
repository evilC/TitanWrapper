using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TitanWrapper.TitanOneApi;

namespace TitanWrapper
{
    public class Wrapper
    {

        #region Fields and Properties

        private TitanOneApi.TitanOne titanOneApi;

        #region Callbacks
        private Dictionary<int, Dictionary<string, dynamic>> buttonCallbacks = new Dictionary<int, Dictionary<string, dynamic>>();
        private Dictionary<int, Dictionary<string, dynamic>> axisCallbacks = new Dictionary<int, Dictionary<string, dynamic>>();
        #endregion

        #region Identifier <--> Index Mappings
        #region Buttons
        private static readonly Dictionary<TitanOne.OutputType, Dictionary<int, int>> ButtonMappings = new Dictionary<TitanOne.OutputType, Dictionary<int, int>>()
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

            { TitanOne.OutputType.PS4, new Dictionary<int, int>()
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
                    { 14, 27 },
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

            { TitanOne.OutputType.XB1, new Dictionary<int, int>()
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

        private static readonly Dictionary<TitanOne.InputType, Dictionary<int, int>> ReverseButtonMappings = new Dictionary<TitanOne.InputType, Dictionary<int, int>>() {
            { TitanOne.InputType.PS3, new Dictionary<int, int>() },
            { TitanOne.InputType.PS4, new Dictionary<int, int>() },
            { TitanOne.InputType.XB360, new Dictionary<int, int>() },
            { TitanOne.InputType.XB1, new Dictionary<int, int>() },
        };

        #endregion

        #region Axes
        private static readonly Dictionary<TitanOne.OutputType, Dictionary<int, int>> AxisMappings = new Dictionary<TitanOne.OutputType, Dictionary<int, int>>()
        {
            { TitanOne.OutputType.PS3, new Dictionary<int, int>()
                {
                    { 1, 11 },
                    { 2, 12 },
                    { 3, 9 },
                    { 4, 10 },
                    { 5, 7 },
                    { 6, 4 },
                }
            },

            { TitanOne.OutputType.PS4, new Dictionary<int, int>()
                {
                    { 1, 11 },
                    { 2, 12 },
                    { 3, 9 },
                    { 4, 10 },
                    { 5, 7 },
                    { 6, 4 },
                    { 7, 21 },
                    { 8, 22 },
                    { 9, 23 },
                    { 10, 28 },
                    { 11, 29 },
                }
            },

            { TitanOne.OutputType.XB360, new Dictionary<int, int>()
                {
                    { 1, 11 },
                    { 2, 12 },
                    { 3, 9 },
                    { 4, 10 },
                    { 5, 7 },
                    { 6, 4 },
                }
            },

            { TitanOne.OutputType.XB1, new Dictionary<int, int>()
                {
                    { 1, 11 },
                    { 2, 12 },
                    { 3, 9 },
                    { 4, 10 },
                    { 5, 7 },
                    { 6, 4 },
                }
            },
        };

        private static readonly Dictionary<TitanOne.InputType, Dictionary<int, int>> ReverseAxisMappings = new Dictionary<TitanOne.InputType, Dictionary<int, int>>() {
            { TitanOne.InputType.PS3, new Dictionary<int, int>() },
            { TitanOne.InputType.PS4, new Dictionary<int, int>() },
            { TitanOne.InputType.XB360, new Dictionary<int, int>() },
            { TitanOne.InputType.XB1, new Dictionary<int, int>() },
        };
        #endregion
        #endregion
        #endregion

        #region Public Methods

        #region Constructors and Destructors
        public Wrapper()
        {
            titanOneApi = new TitanOne(new Action<int, int>(IdentifierChanged));
            if (!titanOneApi.Init())
            {
                throw new Exception("Could not load gcdapi.dll and it's functions");
            }
            
            if (!titanOneApi.IsConnected)
            {
                // ToDo: Does not seem to detect cable not plugged in
                throw new Exception("Could not connect to Titan One device");
            }

            CreateReverseMappings();

            var inputType = titanOneApi.CurrentInputType;
            var outputType = titanOneApi.CurrentOutputType;

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
        #endregion

        #region Input / Output manipulation
        public bool SetButton(int button, int state)
        {
            var identifier = ButtonMappings[titanOneApi.CurrentOutputType][button];
            titanOneApi.SetOutputIdentifier(identifier, state);
            return true;
        }

        public bool SetAxis(int axis, int state)
        {
            var identifier = AxisMappings[titanOneApi.CurrentOutputType][axis];
            titanOneApi.SetOutputIdentifier(identifier, state);
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

        public bool SubscribeAxis(int axis, dynamic callback, string guid = "0")
        {
            if (!axisCallbacks.ContainsKey(axis))
            {
                axisCallbacks[axis] = new Dictionary<string, dynamic>();
            }
            axisCallbacks[axis][guid] = callback;
            return true;
        }
        #endregion

        #endregion

        #region Private Methods

        #region Input handling
        private void IdentifierChanged(int identifier, int value)
        {
            int button;
            try
            {
                button = ReverseButtonMappings[titanOneApi.CurrentInputType][identifier];
                if (buttonCallbacks.ContainsKey(button))
                {
                    foreach (var callback in buttonCallbacks[button])
                    {
                        callback.Value(value);
                    }
                }
            }
            catch { }

            int axis;
            try
            {
                axis = ReverseAxisMappings[titanOneApi.CurrentInputType][identifier];
                if (axisCallbacks.ContainsKey(axis))
                {
                    foreach (var callback in axisCallbacks[axis])
                    {
                        callback.Value(value);
                    }
                }
            }
            catch { }

            //Console.WriteLine(String.Format("Identifier {0} changed to: {1}", identifier, value));
        }
        #endregion

        #region Setup
        private void CreateReverseMappings()
        {
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

            foreach (var type in AxisMappings)
            {
                foreach (var axisMapping in type.Value)
                {
                    try
                    {
                        var iType = TitanOne.outputToInputType[type.Key];
                        ReverseAxisMappings[iType][axisMapping.Value] = axisMapping.Key;
                    }
                    catch
                    {

                    }
                }

            }
        }
        #endregion

        #region Debugging
        private string InputTypeToString(TitanOne.InputType type)
        {
            return Enum.GetName(typeof(TitanOne.InputType), type);
        }

        private string OutputTypeToString(TitanOne.OutputType type)
        {
            return Enum.GetName(typeof(TitanOne.OutputType), type);
        }
        #endregion

        #endregion

    }

}