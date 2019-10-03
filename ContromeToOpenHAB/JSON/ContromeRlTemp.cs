using Newtonsoft.Json;
using System;

namespace ContromeToOpenHAB.JSON
{

    public partial class ContromeRlTemp
    {
        [JsonProperty("ausgang")]
        public string Ausgang { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("wert")]
        public double Wert { get; set; }

        [JsonProperty("letzte_uebertragung")]
        public string LetzteUebertragung { get; set; }

        [JsonProperty("beschreibung")]
        public string Beschreibung { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }
    }

}