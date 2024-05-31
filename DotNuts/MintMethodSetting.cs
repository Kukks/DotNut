using System.Text.Json;
using System.Text.Json.Serialization;

public class MintMethodSetting
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("unit")] public List<Proof> Unit { get; set; }
    [JsonPropertyName("min_amount")] public int? Min { get; set; }
    [JsonPropertyName("max_amount")] public int? Max { get; set; }
}

public class MeltMethodSetting
{
    [JsonPropertyName("method")] public string Method { get; set; }
    [JsonPropertyName("unit")] public List<Proof> Unit { get; set; }
    [JsonPropertyName("min_amount")] public int? Min { get; set; }
    [JsonPropertyName("max_amount")] public int? Max { get; set; }
}

//
// {
//     "name": "Bob's Cashu mint",
//     "pubkey": "0283bf290884eed3a7ca2663fc0260de2e2064d6b355ea13f98dec004b7a7ead99",
//     "version": "Nutshell/0.15.0",
//     "description": "The short mint description",
//     "description_long": "A description that can be a long piece of text.",
//     "contact": [
//     ["email", "contact@me.com"],
//     ["twitter", "@me"],
//     ["nostr" ,"npub..."]
//         ],  
//     "motd": "Message to display to users.",  
//     "nuts": {
//         "4": {
//             "methods": [
//             {
//                 "method": "bolt11",
//                 "unit": "sat",
//                 "min_amount": 0,
//                 "max_amount": 10000        
//             }
//             ],
//             "disabled": false
//         },
//         "5": {
//             "methods": [
//             {
//                 "method": "bolt11",
//                 "unit": "sat",
//                 "min_amount": 100,
//                 "max_amount": 10000        
//             },
//             "disabled": false
//                 ]
//         },
//         "7": {"supported": true},
//         "8": {"supported": true},
//         "9": {"supported": true},
//         "10": {"supported": true},
//         "12": {"supported": true}
//     }
// }
public class GetInfoResponse
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("pubkey")] public string Pubkey { get; set; }
    [JsonPropertyName("version")] public string Version { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("description_long")] public string DescriptionLong { get; set; }
    [JsonPropertyName("contact")] public List<List<string>> Contact { get; set; }
    [JsonPropertyName("motd")] public string Motd { get; set; }
    [JsonPropertyName("nuts")] public Dictionary<string, JsonDocument> Nuts { get; set; }
}