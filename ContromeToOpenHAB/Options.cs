using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContromeToOpenHAB
{
    internal class Options
    {
        [Option('a', "addr", Required = true, HelpText = "The IP-Address oder DNS name of the Controm Mini-Server (e.g 192.168.1.100 or contromeServer)")]
        public string ContromURL { get; set; }

        [Option('u', "user",Required =true , HelpText = "The UserName openHAB wil use to set Values")]
        public string Username { get; set; }

        [Option('p', "password", Required =true, HelpText = "The Password for the User (Hint: the password is stored in plain text in the Config-File)")]
        public string Password { get; set; }

        [Option('h', "houseid", Required = false,DefaultValue =1,  HelpText = "The House-ID in the controme Server to use, default is 1")]
        public int HouseID { get; set; }

        [Option('o', "output", Required = false, DefaultValue = "", HelpText = "Target directory to create the openHAB files in.")]
        public string OutputDir { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
