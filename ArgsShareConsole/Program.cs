using ArgsShare.Models.ArgsControllers;
using System;
using System.Threading;

namespace ArgsShareConsole
{
    public class Program
    {
        private static void Main(string[] args)
        {
            string prefix = "ArgsControllerTester";
            var argsController = new ArgsController(prefix, prefix + "File");

            if (args.Length > 0)
                argsController.AddArg(args[0]);

            if (!argsController.IsOwner)
                return;

            argsController.ArgAddAsync += ArgsController_ArgAddAsync;
            argsController.BeginFollow();

            Console.ReadLine();
        }

        private static void ArgsController_ArgAddAsync(object sender, string e)
        {
            Console.WriteLine(e);
        }
    }
}
