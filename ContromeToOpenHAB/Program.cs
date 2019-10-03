using ContromeToOpenHAB.JSON;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
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
            try
            {
                using (StreamWriter rulesFile = CreateRules(options))
                {
                    using (StreamWriter itemsFile = CreateConfigFile(options, "items\\controme.items"))
                    {
                        using (StreamWriter sitemapFile = CreateSitemap(options))
                        {


                            Uri contromeBaseUrl = new Uri($"http://{options.ContromURL}/");

                            RestRequest request = new RestRequest("get/json/v1/{HouseID}/{Method}");
                            request.AddUrlSegment("HouseID", options.HouseID.ToString());
                            request.AddUrlSegment("Method", "temps");

                            RestClient client = new RestClient(contromeBaseUrl);
                            string strCacheUrlTemp = options.ContromeTempCacheURL;
                            if (string.IsNullOrEmpty(strCacheUrlTemp))
                                strCacheUrlTemp = $"http://{options.ContromURL}/get/json/v1/{options.HouseID}/temps/";

                            string strCacheUrlRelay = options.ContromeRelayCacheURL;
                            if (string.IsNullOrEmpty(strCacheUrlRelay))
                                strCacheUrlRelay = $"http://{options.ContromURL}/get/json/v1/{options.HouseID}/outs/";

                            var responseTemps = client.Execute(request);
                            JArray jsonTemps = JArray.Parse(responseTemps.Content);

                            IRestResponse responseRelays = null;
                            JArray jsonRelays = null;
                            if (options.ReleyStates)
                            {
                                request = new RestRequest("get/json/v1/{HouseID}/{Method}");
                                request.AddUrlSegment("HouseID", options.HouseID.ToString());
                                request.AddUrlSegment("Method", "outs");
                                responseRelays = client.Execute(request);
                                jsonRelays = JArray.Parse(responseRelays.Content);

                            }
                            else { }

                            
                             
                            foreach (JObject floor in jsonTemps)
                            {
                                string strFloorName = floor["etagenname"].Value<string>();

                                Console.WriteLine("Creating files for floor " + strFloorName);

                                sitemapFile.WriteLine($"Frame label=\"{strFloorName}\" {{");
                                string strFloorPrefix = new string(Escape(strFloorName).ToArray()) + "_";
                                JArray rooms = floor["raeume"].Value<JArray>();

                                Dictionary<string, JToken> relayState = null;

                                if (options.ReleyStates)
                                {

                                    relayState = jsonRelays
                                        .Where((o) => o["etagenname"].Value<string>() == strFloorName)
                                        .SelectMany((o) => o["raeume"].Value<JArray>())
                                        .ToDictionary((o) => o["id"].Value<string>());
                                }
                                else { }

                                foreach (var room in rooms)
                                {
                                    string strRoomName = room["name"].Value<string>();
                                    
                                    string strRoomID = room["id"].Value<string>();

                                    request = new RestRequest("get/json/v1/{HouseID}/{Method}/{Room}");
                                    request.AddUrlSegment("HouseID", options.HouseID.ToString());
                                    request.AddUrlSegment("Method", "rltemps");
                                    request.AddUrlSegment("Room", strRoomID);


                                    var ContromeRlTempResponse = client.Execute<List<ContromeRlTemp>>(request);

                                    string strEscapedName = strFloorPrefix + new string(Escape(strRoomName).ToArray());

                                    Console.WriteLine("Creating entries for  " + strRoomName);

                                    itemsFile.WriteLine($"Number Controme_Raw_{strEscapedName} {{http = \"<[{strCacheUrlTemp}:10000:JSONPATH($..raeume[?(@.id=={strRoomID})].temperatur)]\"}}");
                                    itemsFile.WriteLine($"Number Controme_Raw_{strEscapedName}_Soll {{http = \"<[{strCacheUrlTemp}:10000:JSONPATH($..raeume[?(@.id=={strRoomID})].solltemperatur)]\"}}");

                                    int rlTempSensor = 1;
                                    foreach(var rlTemp in ContromeRlTempResponse.Data)
                                    {
                                        itemsFile.WriteLine($"Number Controme_{strEscapedName}_RL_{rlTempSensor} \"{strRoomName} Rücklauf #{rlTempSensor} [% .2f] °C\" {{http = \"<[{strCacheUrlTemp}:10000:JSONPATH($..raeume[?(@.id=={strRoomID})]..sensoren[?(@.name=='{rlTemp.Name}')].wert)]\"}}");
                                        rlTempSensor += 1;
                                    }


                                    itemsFile.WriteLine($"Group g{strEscapedName}Thermostat \"{strRoomName}\" (gFF) [ \"Thermostat\" ]");
                                    itemsFile.WriteLine($"Number Controme_Proxy_{strEscapedName} \"{strRoomName} [% .2f] °C\"  (g{strEscapedName}Thermostat) [ \"CurrentTemperature\" ]");
                                    itemsFile.WriteLine($"Number Controme_Proxy_{strEscapedName}_Soll \"{strRoomName} Soll[% .1f] °C\"  (g{strEscapedName}Thermostat) [ \"TargetTemperature\" ]");
                                    itemsFile.WriteLine();

                                    rulesFile.WriteLine($"rule \"Unpack JSON Value {strEscapedName}\" when System started or Item Controme_Raw_{strEscapedName} changed then ContromeUnpackJsonArray.apply(Controme_Proxy_{strEscapedName}, Controme_Raw_{strEscapedName}) end");
                                    rulesFile.WriteLine($"rule \"Unpack JSON Value {strEscapedName}_Soll\" when System started or Item Controme_Raw_{strEscapedName}_Soll changed then ContromeUnpackJsonArray.apply(Controme_Proxy_{strEscapedName}_Soll, Controme_Raw_{strEscapedName}_Soll) end");
                                    rulesFile.WriteLine($"rule \"Delegate Set - {strEscapedName}\" when Item Controme_Proxy_{strEscapedName}_Soll received command then executeCommandLine(\"curl -X POST -F user={options.Username} -F password={options.Password} -F soll=\"+receivedCommand.toString+\" http://{options.ContromURL}/set/json/v1/{options.HouseID}/soll/{strRoomID}/\") end ");

                                    sitemapFile.WriteLine($"Text item=Controme_Proxy_{strEscapedName} icon=\"TemperaturSensor\"");
                                    sitemapFile.WriteLine($"Setpoint item=Controme_Proxy_{strEscapedName}_Soll step=0.5 minValue=15 maxValue=26 icon=\"TemperaturSensor\"");

                                    if (options.ReleyStates && relayState.ContainsKey(strRoomID))
                                    {
                                        var outs = relayState[strRoomID];

                                        var ausgang = outs["ausgang"];

                                        foreach (JProperty token in ausgang)
                                        {
                                            Console.Write("Ausgang {0} = {1}", token.Name, token.Value.Value<int>());
                                            var strJpath = $"$..raeume[?(@.id=={strRoomID})].ausgang.['{token.Name}']";
                                            var strRelayName = $"{strEscapedName}_Relay_{token.Name.Trim()}";
                                            var strRawRelayItemName = $"Controme_Raw_" + strRelayName;
                                            var strProxyRelayItemName = $"Controme_Proxy_" + strRelayName;

                                            itemsFile.WriteLine($"Number {strRawRelayItemName} {{http = \"<[{strCacheUrlRelay}:10000:JSONPATH({strJpath})]\"}}");
                                            itemsFile.WriteLine($"Switch {strProxyRelayItemName} \"{strRoomName} Ausgang {token.Name.Trim()} [%s]\" <fire>");

                                            rulesFile.WriteLine($"rule \"Unpack JSON Value {strRelayName}\" when System started or Item {strRawRelayItemName} changed then ContromeUnpackJsonArraySwitch.apply({strProxyRelayItemName}, {strRawRelayItemName}) end");
                                            itemsFile.WriteLine();

                                            sitemapFile.WriteLine($"Text item={strProxyRelayItemName}");

                                        }

                                    }
                                    else { }

                                    rulesFile.WriteLine();



                                   }

                                sitemapFile.WriteLine("}");
                            }

                            sitemapFile.WriteLine("}");
                        }
                    }
                }
                DirectoryInfo confDir = new DirectoryInfo(Path.Combine(options.OutputDir, "conf"));
                Console.WriteLine("Created config files at " + confDir.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.ToString());
            }
        }

   
        private static IEnumerable<char> Escape(string strRoomName)
        {
            foreach (char c in strRoomName)
                if (!char.IsWhiteSpace(c))
                    if (char.IsLetterOrDigit(c))
                    {
                        switch (c)
                        {
                            case 'ü':
                                yield return 'u';
                                yield return 'e';
                                break;
                            case 'Ü':
                                yield return 'U';
                                yield return 'e';
                                break;
                            case 'ä':
                                yield return 'a';
                                yield return 'e';
                                break;
                            case 'Ä':
                                yield return 'A';
                                yield return 'e';
                                break;
                            case 'Ö':
                                yield return 'O';
                                yield return 'e';
                                break;
                            case 'ö':
                                yield return 'o';
                                yield return 'e';
                                break;
                            case 'ß':
                                yield return 's';
                                yield return 's';
                                break;
                            default:
                                yield return c;
                                break;
                        }
                    }
                    else
                        yield return '_';
        }

        private static StreamWriter CreateRules(Options options)
        {
            var writer = CreateConfigFile(options, "rules\\controme.rules");
            writer.Write(@"val Functions$Function2<NumberItem,StringItem,String> ContromeUnpackJsonArray = [
 RuleProxyItem,  RuleReadJson |
	var jsonValue = RuleReadJson.state.toString;
        postUpdate(RuleProxyItem, jsonValue);
]");
            writer.WriteLine();
            writer.Write(@"
val Functions$Function2<SwitchItem,StringItem,String> ContromeUnpackJsonArraySwitch = [
 RuleProxyItem,  RuleReadJson |
	var jsonValue = RuleReadJson.state.toString;
    if(jsonValue == ""1"")
    {
                RuleProxyItem.sendCommand(ON);
            }
    else
    {
                RuleProxyItem.sendCommand(OFF);
            }
]");
            writer.WriteLine();
            writer.WriteLine();
            return writer;
        }
        private static StreamWriter CreateConfigFile(Options options, string strName)
        {
            string strPath = Path.Combine(options.OutputDir, "conf\\" + strName);
            FileInfo f = new FileInfo(strPath);
            if (!f.Directory.Exists)
                f.Directory.Create();

            Stream s = File.Open(strPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            return new StreamWriter(s);
        }


        private static StreamWriter CreateSitemap(Options options)
        {
            var writer = CreateConfigFile(options, "sitemaps\\controme.sitemap");

            writer.WriteLine("sitemap controme label=\"Controme\" icon=\"heating\" {");

            return writer;
        }
    }
}
