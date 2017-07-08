using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var tw = new TitanWrapper.Wrapper();

            tw.SubscribeButton(1, new Action<int>((value) => {
                Console.WriteLine("Button 1 Value: " + value);
                tw.SetButton(1, value);
            }));

            tw.SubscribeAxis(1, new Action<int>((value) => {
                Console.WriteLine("Axis 1 Value: " + value);
                tw.SetAxis(1, value);
            }));
        }
    }
}
