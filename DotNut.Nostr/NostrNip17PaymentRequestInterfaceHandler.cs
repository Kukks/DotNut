using System.Security.Cryptography;
using System.Text.Json;
using NBitcoin.Secp256k1;
using NNostr.Client;
using NNostr.Client.Protocols;

namespace DotNut.Nostr;

public class NostrNip17PaymentRequestInterfaceHandler : PaymentRequestInterfaceHandler
{
    public static void Register()
    {
        PaymentRequestTransportInitiator.Handlers.Add(new NostrNip17PaymentRequestInterfaceHandler());
    }

    public bool CanHandle(PaymentRequest request)
    {
        return request.Transports.Any(t => t.Type == "nostr" && t.Tags.Any( t => t.Key == "n" && t.Value.Any(t=>t=="17")));
    }

    public async Task SendPayment(PaymentRequest request, PaymentRequestPayload payload,
        CancellationToken cancellationToken = default)
    { 
        var nprofileStr = request.Transports.First(t => t.Type == "nostr" && t.Tags.Any( t => t.Key == "n" && t.Value.Any(t=>t=="17"))).Target;
        var nprofile = (NIP19.NosteProfileNote) NIP19.FromNIP19Note(nprofileStr);
        using var  client = new CompositeNostrClient(nprofile.Relays.Select(r => new Uri(r)).ToArray());
        await client.Connect(cancellationToken);
        var ephemeralKey = ECPrivKey.Create(RandomNumberGenerator.GetBytes(32));
        var msg = new NostrEvent()
        {
            Kind = 14,
            Content = JsonSerializer.Serialize(payload),
            CreatedAt = DateTimeOffset.Now,
            PublicKey = ephemeralKey.CreateXOnlyPubKey().ToHex(),
            Tags = new(),
        };
        msg.Id = msg.ComputeId();
        
        var giftWrap = await NIP17.Create(msg, ephemeralKey,ECXOnlyPubKey.Create(Convert.FromHexString(nprofile.PubKey)), null);
        await client.SendEventsAndWaitUntilReceived(new []{giftWrap}, cancellationToken);

    }
}