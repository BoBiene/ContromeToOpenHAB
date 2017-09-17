using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContromeToOpenHAB
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();

            if (!CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            Uri contromeBaseUrl = new Uri($"http://{options.ContromURL}/get/json/v1/");

            RestClient client = new RestClient(contromeBaseUrl);
          
        }
    }
}
