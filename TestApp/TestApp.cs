using System;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var titan = new TitanWrapper.Wrapper();

            titan.SubscribeButton(1, new Action<int>((value) => {
                Console.WriteLine("Button 1 Value: " + value);
                titan.SetButton(1, value);
            }));

            titan.SubscribeAxis(1, new Action<int>((value) => {
                Console.WriteLine("Axis 1 Value: " + value);
                titan.SetAxis(1, value);
            }));
        }
    }
}
