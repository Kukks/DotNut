using DotNut.NBitcoin.BIP39;
using DotNut.Nostr;
using NBitcoin.Secp256k1;
using NNostr.Client;

namespace DotNut.Tests.Unit;

public class Nut27Tests
{
    [Fact]
    public void DerivesKeysCorrectly()
    {
        var mnemonic = "half depart obvious quality work element tank gorilla view sugar picture humble";
        var correctPrivkey = "e7ca79469a270b36617e4227ff2f068d3bcbb6b072c8584190b0203597c53c0d";
        var correctPubKey = "0767277aaed200af7a8843491745272fc1ad2c7bfe340225e6f34f3a9a273aed";
        var handler = new MintListBackupHandler(new(mnemonic), []);
        var privKey = handler.DeriveBackupPrivkey();
        Assert.Equal(correctPrivkey, privKey.ToString());
        Assert.Equal(correctPubKey, privKey.Key.CreatePubKey().ToXOnlyPubKey().ToHex());
    }
}