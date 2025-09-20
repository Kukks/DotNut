using System.Data.Common;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNut.ApiModels;
using DotNut.ApiModels.Info;

namespace DotNut.Abstractions;

public class MintInfo
{
    private readonly GetInfoResponse _mintInfo;
    private readonly ProtectedEndpoints? _protectedEndpoints;

    public MintInfo(GetInfoResponse info)
    {
        _mintInfo = info;
        
        if (info.Nuts?.TryGetValue(22, out var nut22Json) == true)
        {
            try
            {
                var nut22 = JsonSerializer.Deserialize<Nut22>(nut22Json.RootElement.GetRawText());
                if (nut22?.ProtectedEndpoints != null)
                {
                    _protectedEndpoints = new ProtectedEndpoints
                    {
                        Cache = new Dictionary<string, bool>(),
                        ApiReturn = nut22.ProtectedEndpoints.Select(o => new ProtectedEndpoint
                        {
                            Method = o.Method,
                            Regex = new System.Text.RegularExpressions.Regex(o.Path)
                        }).ToArray()
                    };
                }
            }
            catch (JsonException)
            {
                // Ignore parsing errors for NUT-22
            }
        }
    }
    
    /// <summary>
    /// Checks support for NUTs 4 and 5 (mint/melt operations)
    /// </summary>
    public SwapInfo IsSupportedMintMelt(int nutNumber)
    {
        if (nutNumber != 4 && nutNumber != 5)
            throw new ArgumentException("Only NUT 4 and 5 are supported by this method", nameof(nutNumber));
            
        return CheckMintMelt(nutNumber);
    }

    /// <summary>
    /// Checks support for generic NUTs (7, 8, 9, 10, 11, 12, 14, 20)
    /// </summary>
    public GenericNut IsSupportedGeneric(int nutNumber)
    {
        var supportedNuts = new[] { 7, 8, 9, 10, 11, 12, 14, 20 };
        if (!supportedNuts.Contains(nutNumber))
            throw new ArgumentException($"NUT {nutNumber} is not supported by this method", nameof(nutNumber));
            
        return CheckGenericNut(nutNumber);
    }

    /// <summary>
    /// Checks support for NUT 17 (WebSocket)
    /// </summary>
    public WebSocketSupportResult IsSupportedWebSocket()
    {
        return CheckNut17();
    }

    /// <summary>
    /// Checks support for NUT 15 (MPP)
    /// </summary>
    public MppSupport IsSupportedMpp()
    {
        return CheckNut15();
    }

    /// <summary>
    /// Determines if an endpoint requires blind authentication token based on NUT-22
    /// </summary>
    public bool RequiresBlindAuthToken(string path)
    {
        if (_protectedEndpoints == null)
            return false;

        if (_protectedEndpoints.Cache.TryGetValue(path, out var cachedValue))
            return cachedValue;

        var isProtectedEndpoint = _protectedEndpoints.ApiReturn
            .Any(e => e.Regex.IsMatch(path));
        
        _protectedEndpoints.Cache[path] = isProtectedEndpoint;
        return isProtectedEndpoint;
    }

    private GenericNut CheckGenericNut(int nutNumber)
    {
        if (_mintInfo.Nuts?.TryGetValue(nutNumber, out var nutJson) == true)
        {
            try
            {
                var nut = JsonSerializer.Deserialize<GenericNut>(nutJson.RootElement.GetRawText());
                return new GenericNut { Supported = nut?.Supported == true };
            }
            catch (JsonException)
            {
                return new GenericNut { Supported = false };
            }
        }
        return new GenericNut { Supported = false };
    }

    private SwapInfo CheckMintMelt(int nutNumber)
    {
        if (_mintInfo.Nuts?.TryGetValue(nutNumber, out var nutJson) == true)
        {
            try
            {
                var nut = JsonSerializer.Deserialize<SwapInfo>(nutJson.RootElement.GetRawText());
                if (nut?.Methods != null && nut.Methods.Length > 0 && nut.Disabled != true)
                {
                    return new SwapInfo 
                    { 
                        Disabled = false, 
                        Methods = nut.Methods 
                    };
                }
                return new SwapInfo 
                { 
                    Disabled = true, 
                    Methods = nut?.Methods ?? [] 
                };
            }
            catch (JsonException)
            {
                return new SwapInfo 
                { 
                    Disabled = true, 
                    Methods = [] 
                };
            }
        }
        return new SwapInfo 
        { 
            Disabled = true, 
            Methods = [] 
        };
    }

    private WebSocketSupportResult CheckNut17()
    {
        if (_mintInfo.Nuts?.TryGetValue(17, out var nutJson) == true)
        {
            try
            {
                var nut = JsonSerializer.Deserialize<WebSocketNut>(nutJson.RootElement.GetRawText());
                if (nut?.Supported != null && nut.Supported.Length > 0)
                {
                    return new WebSocketSupportResult 
                    { 
                        Supported = true, 
                        Params = nut.Supported 
                    };
                }
            }
            catch (JsonException)
            {
                // Ignore parsing errors
            }
        }
        return new WebSocketSupportResult { Supported = false };
    }

    private MppSupport CheckNut15()
    {
        if (_mintInfo.Nuts?.TryGetValue(15, out var nutJson) == true)
        {
            try
            {
                var nut = JsonSerializer.Deserialize<MmpInfo>(nutJson.RootElement.GetRawText());
                if (nut?.Methods != null && nut.Methods.Length > 0)
                {
                    return new MppSupport 
                    { 
                        Supported = true, 
                        Methods = nut.Methods 
                    };
                }
            }
            catch (JsonException)
            {
                // Ignore parsing errors
            }
        }
        return new MppSupport() { Supported = false };
    }
    
    public bool SupportsBolt12Description
    {
        get
        {
            if (_mintInfo.Nuts?.TryGetValue(4, out var nut4Json) == true)
            {
                try
                {
                    var nut4 = JsonSerializer.Deserialize<MintMeltNut>(nut4Json.RootElement.GetRawText());
                    return nut4?.Methods?.Any(method => 
                        method.Method == "bolt12" && method.Options?.Description == true) == true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }
            return false;
        }
    }

    
    public string? Contact => _mintInfo.Contact?.FirstOrDefault()?.ToString();
    public string? Description => _mintInfo.Description;
    public string? DescriptionLong => _mintInfo.DescriptionLong;
    public string? Name => _mintInfo.Name;
    public string? Pubkey => _mintInfo.Pubkey;
    public Dictionary<int, JsonDocument>? Nuts => _mintInfo.Nuts;
    public string? Version => _mintInfo.Version;
    public string? Motd => _mintInfo.Motd;
}

// Supporting classes for different NUT types
public class GenericNut
{
    [JsonPropertyName("supported")]
    public bool Supported { get; set; }
}

public class MintMeltNut
{
    [JsonPropertyName("methods")]
    public SwapInfo.SwapMethod[]? Methods { get; set; }
    
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }
}

public class WebSocketNut
{
    [JsonPropertyName("supported")]
    public WebSocketSupport[]? Supported { get; set; }
}

public class Nut22
{
    [JsonPropertyName("protected_endpoints")]
    public ProtectedEndpointSpec[]? ProtectedEndpoints { get; set; }
}

public class ProtectedEndpointSpec
{
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

// Internal classes for protected endpoints caching
internal class ProtectedEndpoints
{
    public Dictionary<string, bool> Cache { get; set; } = new();
    public ProtectedEndpoint[] ApiReturn { get; set; } = Array.Empty<ProtectedEndpoint>();
}

internal class ProtectedEndpoint
{
    public string Method { get; set; } = string.Empty;
    public System.Text.RegularExpressions.Regex Regex { get; set; } = null!;
}

public class WebSocketSupportResult
{
    public bool Supported { get; set; }
    public WebSocketSupport[]? Params { get; set; }
}

public class MppSupport : MmpInfo
{
    public bool Supported { get; set; }
}
