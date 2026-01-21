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
        return request.Transports.Any(t =>
            t.Type == "nostr" &&
            t.Tags?.Any(tag => tag.Key == "n" && tag.Value.Any(v => v == "17")) == true);

    }

    public async Task SendPayment(PaymentRequest request, PaymentRequestPayload payload,
        CancellationToken cancellationToken = default)
    { 
        var nostrTransport = request.Transports.FirstOrDefault(t => 
            t.Type == "nostr" &&
            t.Tags?.Any(tag => tag.Key == "n" && tag.Value.Any(v => v == "17")) == true);
        if (nostrTransport is null)
        {
            throw new InvalidOperationException("No NIP17 nostr transport found.");
        }
        var nprofileStr = nostrTransport.Target;
        
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