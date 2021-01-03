using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ContromeToOpenHAB
{
    internal class Options
    {
        [Option('a', "addr", Required = true, HelpText = "The IP-Address oder DNS name of the Controme-Mini-Server (e.g 192.168.1.100 or contromeServer)")]
        public string ContromeURL { get; set; }

        [Option('u', "user",Required =true , HelpText = "The UserName openHAB will use to set Values")]
        public string Username { get; set; }

        [Option('p', "password", Required =true, HelpText = "The Password for the User (Hint: the password is stored in plain text in the config-File)")]
        public string Password { get; set; }

        [Option('h', "houseid", Required = false,Default=1,  HelpText = "The House-ID in the Controme Server to use, default is 1")]
        public int HouseID { get; set; }

        [Option('o', "output", Required = false, Default= "", HelpText = "Target directory to create the openHAB files in.")]
        public string OutputDir { get; set; }

        [Option('r', "relay", Required = false, Default= true, HelpText = "Generates relay states")]
        public bool ReleyStates { get; set; } = true;


        [Option('t', "TempSensorIds", HelpText = "List of external temp-sensor-ids. Matching is done by string start.")]
        public IEnumerable<string> ExternalTempSensorIds { get; set; }

        [Option('f', "HumiditySensorIds", HelpText = "List of external temp-sensor-ids. Matching is done by string start.")]
        public IEnumerable<string> ExternalHumiditySensorIds { get; set; }

    }
}
