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
                            string strCacheUrl = options.ContromCacheURL;
                            if (string.IsNullOrEmpty(strCacheUrl))
                                strCacheUrl = $"http://{options.ContromURL}/get/json/v1/{options.HouseID}/temps/";

                            var response = client.Execute(request);

                            JArray json = JArray.Parse(response.Content);

                            foreach (JObject floor in json)
                            {
                                string strFloorName = floor["etagenname"].Value<string>();

                                Console.WriteLine("Creating files for floor " + strFloorName);

                                sitemapFile.WriteLine($"Frame label=\"{strFloorName}\" {{");
                                string strFloorPrefix = new string(Escape(strFloorName).ToArray()) + "_";
                                JArray rooms = floor["raeume"].Value<JArray>();

                                foreach (var room in rooms)
                                {
                                    string strRoomName = room["name"].Value<string>();
                                    string strRoomID = room["id"].Value<string>();
                                    string strEscapedName = strFloorPrefix + new string(Escape(strRoomName).ToArray());

                                    Console.WriteLine("Creating entries for  " + strRoomName);

                                    itemsFile.WriteLine($"String Controme_Raw_{strEscapedName} {{http = \"<[{strCacheUrl}:10000:JSONPATH($..raeume[?(@.id=={strRoomID})].temperatur)]\"}}");
                                    itemsFile.WriteLine($"String Controme_Raw_{strEscapedName}_Soll {{http = \"<[{strCacheUrl}:10000:JSONPATH($..raeume[?(@.id=={strRoomID})].solltemperatur)]\"}}");

                                    itemsFile.WriteLine($"Group g{strEscapedName}Thermostat \"{strRoomName}\" (gFF) [ \"Thermostat\" ]");
                                    itemsFile.WriteLine($"Number Controme_Proxy_{strEscapedName} \"{strRoomName} [% .2f] °C\"  (g{strEscapedName}Thermostat) [ \"CurrentTemperature\" ]");
                                    itemsFile.WriteLine($"Number Controme_Proxy_{strEscapedName}_Soll \"{strRoomName} Soll[% .1f] °C\"  (g{strEscapedName}Thermostat) [ \"TargetTemperature\" ]");
                                    itemsFile.WriteLine();



                                    rulesFile.WriteLine($"rule \"Unpack JSON Value {strEscapedName}\" when System started or Item Controme_Raw_{strEscapedName} changed then ContromeUnpackJsonArray.apply(Controme_Proxy_{strEscapedName}, Controme_Raw_{strEscapedName}) end");
                                    rulesFile.WriteLine($"rule \"Unpack JSON Value {strEscapedName}_Soll\" when System started or Item Controme_Raw_{strEscapedName}_Soll changed then ContromeUnpackJsonArray.apply(Controme_Proxy_{strEscapedName}_Soll, Controme_Raw_{strEscapedName}_Soll) end");
                                    rulesFile.WriteLine($"rule \"Delegate Set - {strEscapedName}\" when Item Controme_Proxy_{strEscapedName}_Soll received command then executeCommandLine(\"curl -X POST -F user={options.Username} -F password={options.Password} -F soll=\"+receivedCommand.toString+\" http://{options.ContromURL}/set/json/v1/{options.HouseID}/soll/{strRoomID}/\") end ");
                                    rulesFile.WriteLine();


                                    sitemapFile.WriteLine($"Text item=Controme_Proxy_{strEscapedName} icon=\"TemperaturSensor\"");
                                    sitemapFile.WriteLine($"Setpoint item=Controme_Proxy_{strEscapedName}_Soll step=0.5 minValue=15 maxValue=26 icon=\"TemperaturSensor\"");
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
            writer.Write(@"import org.eclipse.smarthome.core.library.items.NumberItem
import org.eclipse.smarthome.core.library.items.StringItem
import org.eclipse.xtext.xbase.lib.Functions

val Functions.Function2 ContromeUnpackJsonArray = [
NumberItem RuleProxyItem, StringItem RuleReadJson |
	var jsonValue = RuleReadJson.state.toString;
    var unpackedValue = jsonValue.substring(1, jsonValue.length() - 1);
    postUpdate(RuleProxyItem, unpackedValue);
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

            writer.WriteLine("sitemap default label=\"Controme\" icon=\"heating\" {");

            return writer;
        }
    }
}
