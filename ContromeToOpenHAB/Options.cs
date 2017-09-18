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
        [Option('a', "addr", Required = true, HelpText = "The IP-Address oder DNS name of the Controme-Mini-Server (e.g 192.168.1.100 or contromeServer)")]
        public string ContromURL { get; set; }

        [Option('u', "user",Required =true , HelpText = "The UserName openHAB will use to set Values")]
        public string Username { get; set; }

        [Option('p', "password", Required =true, HelpText = "The Password for the User (Hint: the password is stored in plain text in the config-File)")]
        public string Password { get; set; }

        [Option('h', "houseid", Required = false,DefaultValue =1,  HelpText = "The House-ID in the Controme Server to use, default is 1")]
        public int HouseID { get; set; }

        [Option('o', "output", Required = false, DefaultValue = "", HelpText = "Target directory to create the openHAB files in.")]
        public string OutputDir { get; set; }

        [Option('c',"cacheUrl",Required =false,DefaultValue ="controme",HelpText ="The HTTP-Cache-Entry to point to the Controme-Mini-Server. Set to empty to disable.")]
        public string ContromCacheURL { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
