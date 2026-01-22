using System.Text.Json;
using DotNut.NBitcoin.BIP39;
using NBitcoin.Secp256k1;
using NNostr.Client;
using NNostr.Client.Protocols;
using SHA256 = System.Security.Cryptography.SHA256;

namespace DotNut.Nostr;

public class MintListBackupHandler
{
    private static readonly byte[] SEPARATOR = "cashu-mint-backup"u8.ToArray();
    private static readonly int EVT_KIND = 30078;

    private Mnemonic _mnemonic;
    private string? _client;
    private Uri[] _relays;

    public MintListBackupHandler(Mnemonic mnemonic, Uri[] relays, string? client = null)
    {
        this._mnemonic = mnemonic;
        this._relays = relays;
        this._client = client;
    }

    public async Task SendBackupEvent(MintListBackup data, CancellationToken ct = default)
    {
        var privkey = DeriveBackupPrivkey();
        var pubkey = privkey.Key.CreatePubKey().ToXOnlyPubKey();
        var plaintext = JsonSerializer.Serialize(data);
        var encrypted = NIP44.Encrypt(privkey, pubkey, plaintext);

        var nevent = new NostrEvent()
        {
            Kind = EVT_KIND,
            Content = encrypted,
            Tags = [new NostrEventTag { TagIdentifier = "d", Data = ["mint-list"] }],
            CreatedAt = data.Timestamp,
            PublicKey = pubkey.ToHex(),
        };
        if (_client != null)
        {
            nevent.Tags.Add(new NostrEventTag() { TagIdentifier = "client", Data = [_client] });
        }

        await nevent.ComputeIdAndSignAsync(privkey);
        foreach (var relay in this._relays)
        {
            using var client = new NostrClient(relay);
            await client.Connect(ct);
            await client.WaitUntilConnected(ct);
            await client.PublishEvent(nevent, ct);
            await client.Disconnect();
        }
    }

    public async Task<MintListBackup?> ReadBackupEvent(CancellationToken ct = default)
    {
        var privkey = DeriveBackupPrivkey();
        var pubkey = privkey.Key.CreatePubKey().ToXOnlyPubKey();

        NostrEvent? evt = null;
        foreach (var relay in this._relays)
        {
            using var client = new NostrClient(relay);
            await client.Connect(ct);
            await client.WaitUntilConnected(ct);

            var filter = new NostrSubscriptionFilter()
            {
                Authors = [pubkey.ToHex()],
                Kinds = [EVT_KIND],
            };
            var evts = await client.FetchEvents([filter], ct);

            if (evts != null && evts.Count != 0)
            {
                var latest = evts.OrderByDescending(e => e.CreatedAt).First();
                if (evt == null || latest.CreatedAt > evt.CreatedAt)
                {
                    evt = latest;
                }
            }
        }

        var encrypted = evt?.Content;
        if (encrypted == null)
        {
            return null;
        }
        var decrypted = NIP44.Decrypt(privkey, pubkey, encrypted);
        return JsonSerializer.Deserialize<MintListBackup>(decrypted);
    }

    private PrivKey DeriveBackupPrivkey()
    {
        var seed = _mnemonic.DeriveSeed();
        byte[] combinedData = [.. seed, .. SEPARATOR];
        var pkBytes = SHA256.HashData(combinedData);
        return ECPrivKey.Create(pkBytes);
    }
}
