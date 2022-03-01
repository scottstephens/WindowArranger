using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowArranger
{
    internal class ConsoleProgram
    {
        public void Run(string[] args)
        {
            var parse_result = ParseArguments(args);
            if (!parse_result.Success)
                ExitWithError(parse_result.Message);

            var pargs = parse_result.Value;

            if (pargs.Mode == LayoutEnum.Auto)
            {
                var displays = Helpers.GetDisplayMonitors().ToList();
                var displays2 = Helpers.GetDisplayDevices().ToList();

                try
                {
                    if (displays.Count == 1)
                        LayoutEngine.DoLayout(LayoutEnum.Laptop);
                    else if (displays.Count == 2)
                        LayoutEngine.DoLayout(LayoutEnum.LaptopAnd4K);
                    else
                        ExitWithError("Auto mode only works with one or two screens.");
                }
                catch(Exception e)
                {
                    ExitWithError(e.ToString());
                }
            }

            //this.Layout(this.LayoutTest);
        }

        private ParseResult<Arguments> ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                return ParseResult<Arguments>.CreateSuccess(new Arguments { Mode = LayoutEnum.Auto });
            }
            else if (args.Length == 1)
            {
                if (!Enum.TryParse<LayoutEnum>(args[0], out var mode))
                    return ParseResult<Arguments>.CreateFailure($"Unrecognized layout: {args[0]}");
                else
                    return ParseResult<Arguments>.CreateSuccess(new Arguments { Mode = mode });
            }
            else //if (args.Length > 1)
            {
                return ParseResult<Arguments>.CreateFailure("Requires exactly one argument.");
            }
        }

        private void ExitWithError(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            Environment.Exit(-1);
        }

        private class Arguments
        {
            public LayoutEnum Mode;
        }

        private class ParseResult<T> where T : class
        {
            public bool Success;
            public string Message;
            public T Value;

            private ParseResult(bool success, T value, string message)
            {
                this.Success = success;
                this.Value = value;
                this.Message = message;
            }

            public static ParseResult<T> CreateSuccess(T value)
            {
                return new ParseResult<T>(true, value, null);
            }

            public static ParseResult<T> CreateFailure(string message)
            {
                return new ParseResult<T>(false, null, message);
            }
        }
    }
}
