using CommandLine;
using ContromeToOpenHAB.JSON;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace ContromeToOpenHAB
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<Options>(args)
                 .WithParsed<Options>(Run);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.ToString());
            }
        }


        static void Run(Options options)
        {

            try
            {
                using (StreamWriter thingsFile = CreateConfigFile(options, Path.Combine("things", "controme.things")))
                {
                    using (StreamWriter itemsFile = CreateConfigFile(options, Path.Combine("items", "controme.items")))
                    {
                        using (StreamWriter sitemapFile = CreateSitemap(options))
                        {

                            Uri contromeBaseUrl = new Uri($"http://{options.ContromeURL}/");

                            RestRequest request = new RestRequest("get/json/v1/{HouseID}/{Method}");
                            request.AddUrlSegment("HouseID", options.HouseID.ToString());
                            request.AddUrlSegment("Method", "temps");

                            string strGetTemps = $"get/json/v1/{options.HouseID}/temps/";
                            string strSetTargetTemp = $"set/json/v1/{options.HouseID}/soll/";
                            string strGetOuts = $"get/json/v1/{options.HouseID}/outs/";


                            RestClient client = new RestClient(contromeBaseUrl);

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
                            string thingId = $"http:url:Controme_{GetDeterministicHashCode(options.ContromeURL):X}";
                            thingsFile.WriteLine($"Thing {thingId} \"Controme\" [ baseURL=\"{contromeBaseUrl.AbsoluteUri}\", refresh=3600,commandMethod=\"POST\", contentType=\"application/x-www-form-urlencoded\" ] {{");
                            thingsFile.WriteLine("\tChannels:");
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
                                    string strJpath;
                                    string strRoomID = room["id"].Value<string>();

                                    request = new RestRequest("get/json/v1/{HouseID}/{Method}/{Room}");
                                    request.AddUrlSegment("HouseID", options.HouseID.ToString());
                                    request.AddUrlSegment("Method", "rltemps");
                                    request.AddUrlSegment("Room", strRoomID);


                                    var ContromeRlTempResponse = client.Execute<List<ContromeRlTemp>>(request);

                                    string strEscapedName = strFloorPrefix + new string(Escape(strRoomName).ToArray());

                                    Console.WriteLine("Creating entries for  " + strRoomName);
                                    strJpath = $"$..raeume[?(@.id=={strRoomID})].temperatur";
                                    if (IsJPathValid(jsonTemps, strJpath))
                                    {
                                        thingsFile.WriteLine($"\t\tType number : {strEscapedName} \"{strRoomName}\" [stateExtension=\"{strGetTemps}\", stateTransformation=\"JSONPATH:{strJpath}\", mode=\"READONLY\" ] ");
                                    }
                                    else
                                        Debugger.Break();
                                    strJpath = $"$..raeume[?(@.id=={strRoomID})].solltemperatur";
                                    if (IsJPathValid(jsonTemps, strJpath))
                                    {
                                        thingsFile.WriteLine($"\t\tType number : {strEscapedName}_setpoint \"{strRoomName} soll\" [stateExtension=\"{strGetTemps}\", stateTransformation=\"JSONPATH:{strJpath}\", commandExtension=\"{strSetTargetTemp}{strRoomID}/\",commandTransformation=\"REGEX:s/(.+)/soll=$1&user={Uri.EscapeUriString(options.Username)}&password={Uri.EscapeUriString(options.Password)}/g\" ] ");
                                        //itemsFile.WriteLine($"Number Controme_Raw_{strEscapedName}_Soll {{http = \"<[{strCacheUrlTemp}:10000:JSONPATH({strJpath})]\"}}");
                                    }
                                    else
                                        Debugger.Break();


                                    itemsFile.WriteLine($"Group g{strEscapedName}Thermostat \"{strRoomName}\" <heating> [\"RadiatorControl\"] {{alexa = \"Endpoint.Thermostat\"}}");
                                    itemsFile.WriteLine($"Number:Temperature Controme_{strEscapedName} \"{strRoomName}\" <temperature> (g{strEscapedName}Thermostat) [\"Point\",\"Temperature\"] {{channel=\"{thingId}:{strEscapedName}\",alexa=\"TemperatureSensor.temperature\" }}");
                                    itemsFile.WriteLine($"Number:Temperature Controme_{strEscapedName}_setpoint \"{strRoomName} Soll\" <temperature> (g{strEscapedName}Thermostat) [\"Point\",\"Temperature\"] {{channel=\"{thingId}:{strEscapedName}_setpoint\",alexa=\"TemperatureSensor.targetSetpoint\" }}");


                                    var rlIds = ContromeRlTempResponse.Data.Select((r) => r.Name).ToHashSet();

                                    var sensors = room["sensoren"] as JArray;
                                    if (sensors != null)
                                    {
                                        int nRL = 1;
                                        int nRoom = 0;
                                        int nExternalTemp = 1;
                                        int nExternalHum = 1;
                                        for (int i = 0; i < sensors.Count; ++i)
                                        {
                                            var sensor = sensors[i];
                                            var sensorId = (sensor["name"] as JValue).Value<string>();

                                            string strSensorDescription = (sensor["beschreibung"] as JValue).Value<string>();
                                            string strSensorName = $"sensor{i}";
                                            bool blnIsExternalTemp = options.ExternalTempSensorIds.Any(externalSensorId => sensorId.StartsWith(externalSensorId, StringComparison.InvariantCultureIgnoreCase));
                                            bool blnIsExternalHumidity = options.ExternalHumiditySensorIds.Any(externalSensorId => sensorId.StartsWith(externalSensorId, StringComparison.InvariantCultureIgnoreCase));
                                            string strUnit = "Temperature";
                                            if (rlIds.Contains(sensorId))
                                            {
                                                strSensorDescription = $"{strRoomName} Rücklauf #{nRL}";
                                                strSensorName = $"RL_{nRL++}";

                                            }
                                            else if ((sensor["raumtemperatursensor"] as JValue).Value<bool>())
                                            {
                                                strSensorDescription = $"{strRoomName} Raumtemperatur";
                                                if (nRoom++ > 0)
                                                    strSensorName = $"room{nRoom}";
                                                else
                                                    strSensorName = "room";
                                            }
                                            else if (blnIsExternalTemp)
                                            {
                                                strSensorDescription = $"{strRoomName} external Temp";
                                                strSensorName = $"ext_temp_{nExternalTemp++}";
                                            }
                                            else if (blnIsExternalHumidity)
                                            {
                                                strSensorDescription = $"{strRoomName} external Humidity";
                                                strSensorName = $"ext_humi_{nExternalHum++}";
                                                strUnit = "Humidity";
                                            }
                                            else if (!string.IsNullOrWhiteSpace(strSensorDescription))
                                            {
                                                //keep
                                            }
                                            else
                                            {
                                                strSensorDescription = $"{strRoomName} sensor #{i}";
                                            }

                                            strJpath = $"$..raeume[?(@.id=={strRoomID})]..sensoren[?(@.name=='{sensorId}')].wert";

                                            thingsFile.Write($"\t\tType number : {strEscapedName}_{strSensorName} \"{strSensorDescription}\" [stateExtension=\"{strGetTemps}\", stateTransformation=\"JSONPATH:{strJpath}\"");
                                            if (blnIsExternalTemp)
                                            {
                                                thingsFile.Write($", commandExtension=\"set/{sensorId}/temperatur/%2$\", commandTransformation=\"JS:ContromeValue.js\" ");
                                            }
                                            else if(blnIsExternalHumidity)
                                            {
                                                thingsFile.Write($", commandExtension=\"set/{sensorId}/%2$.2f\" ");
                                            }
                                            else
                                            {
                                                thingsFile.Write(", mode=\"READONLY\" ");
                                            }

                                            thingsFile.WriteLine("] ");

                                            itemsFile.WriteLine($"Number:{((blnIsExternalHumidity) ? "Dimensionless" : "Temperature")} Controme_{strEscapedName}_{strSensorName} \"{strSensorDescription}\" <{strUnit.ToLower()}> (g{strEscapedName}Thermostat) [\"Measurement\",\"{strUnit}\"] {{channel=\"{thingId}:{strEscapedName}_{strSensorName}\"}}");
                                        }
                                    }
                                    else { }



                                    
                                    

                                    sitemapFile.WriteLine($"Text item=Controme_{strEscapedName} icon=\"TemperaturSensor\"");
                                    sitemapFile.WriteLine($"Setpoint item=Controme_{strEscapedName}_Soll step=0.5 minValue=15 maxValue=26 icon=\"TemperaturSensor\"");

                                    if (options.ReleyStates && relayState.ContainsKey(strRoomID))
                                    {
                                        var outs = relayState[strRoomID];

                                        var ausgang = outs["ausgang"];

                                        foreach (JProperty token in ausgang)
                                        {
                                            Console.WriteLine("Ausgang {0} = {1}", token.Name, token.Value.Value<int>());
                                            strJpath = $"$..raeume[?(@.id=={strRoomID})].ausgang.['{token.Name}']";
                                            var strRelayName = $"{strEscapedName}_Relay_{token.Name.Trim()}";

                                            if (IsJPathValid(jsonRelays, strJpath))
                                            {
                                                thingsFile.WriteLine($"\t\tType contact : {strRelayName} \"{strRoomName} Ausgang {token.Name.Trim()}\" [stateExtension=\"{strGetOuts}\", stateTransformation=\"JSONPATH:{strJpath}\",openValue=\"1\",closedValue=\"0\", mode=\"READONLY\" ] ");
                                                

                                                itemsFile.WriteLine($"Contact Controme_{strRelayName} \"{strRoomName} Ausgang {token.Name.Trim()}\" <fire> (g{strEscapedName}Thermostat) [\"OpenState\"] {{channel=\"{thingId}:{strRelayName}\"}}");

                                                sitemapFile.WriteLine($"Text item=Controme_{strRelayName}");
                                            }
                                            else
                                            {
                                                //Missing... 
                                            }
                                        }

                                    }
                                    else { }

                                    itemsFile.WriteLine();
                                }

                                sitemapFile.WriteLine("}");
                            }

                            sitemapFile.WriteLine("}");
                        }
                        thingsFile.WriteLine("}");
                    }
                }
                CreateJSValueTransform(options);
                DirectoryInfo confDir = new DirectoryInfo(Path.Combine(options.OutputDir, "conf"));
                Console.WriteLine("Created config files at " + confDir.FullName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.ToString());
            }
        }

        private static bool IsJPathValid(JToken json, string strJPath) => json.SelectTokens(strJPath).Any();

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
        private static void CreateJSValueTransform(Options options)
        {
            using(var file =CreateConfigFile(options,Path.Combine("transform","ContromeValue.js")))
            {
                file.Write(@"(function(T) {
    //https://community.openhab.org/t/controme-smart-heat/26797/22
    var temp_coarse= Math.floor(T*2)/2;     // T is true measured float temperature value
    var temp_fine= T-temp_coarse;
    var value = temp_coarse+ Math.floor(2.0*temp_fine + Math.floor(temp_fine*16) *6.25)/100;
    return value.toFixed(2);
})(value)");
            }
        }
        private static StreamWriter CreateConfigFile(Options options, string strName)
        {
            string strPath = Path.Combine(options.OutputDir, Path.Combine("conf", strName));
            FileInfo f = new FileInfo(strPath);
            if (!f.Directory.Exists)
                f.Directory.Create();

            Stream s = File.Open(strPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            return new StreamWriter(s);
        }


        private static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
        private static StreamWriter CreateSitemap(Options options)
        {
            var writer = CreateConfigFile(options, Path.Combine("sitemaps", "controme.sitemap"));

            writer.WriteLine("sitemap controme label=\"Controme\" icon=\"heating\" {");

            return writer;
        }
    }
}
