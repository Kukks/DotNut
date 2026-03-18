using DotNut.NBitcoin.BIP39;
using DotNut.NUT13;
using NBip32Fast;
using NBitcoin.Secp256k1;

namespace DotNut.Tests.Unit;

public class Nut13Tests
{
        internal readonly struct TestCase(in string path, in string keyHex, in string ccHex)
    {
        internal static readonly ReadOnlyMemory<byte> Seed = Convert.FromHexString(
            "e4a964f4973ce5750a6a5a5126e8258442c197b2e71b683ccba58688f21242eae1b0f12bee21d6e983d4a5c61f081bf3f0669546eb576dec1b22ec8d481b00fb"
        );

        internal readonly ReadOnlyMemory<byte> Key = Convert.FromHexString(keyHex);
        internal readonly ReadOnlyMemory<byte> ChainCode = Convert.FromHexString(ccHex);

        internal readonly KeyPath Path = path;
    }

    private static readonly TestCase Case1SecP256K1 = new(
        "m/0'/0/0",
        "6144c1daf8222d6dab77e7a20c2f338519b83bd1423602c56c7dfb5e9ea99c02",
        "55b36970e7ab8434f9b04f1c2e52da7422d2bce7e284ca353419dddfa2e34bdb"
    );

    [Fact]
    public void Bip32Test()
    {
        var masterKeyFromSeed = BIP32.Instance.GetMasterKeyFromSeed(TestCase.Seed.Span);

        Assert.Equal(
            "5A876CC4B4AB2F6717951AEE7F97AB69844DBFFFF7074E6E6F71D2BA04BD6EC9",
            Convert.ToHexString(masterKeyFromSeed.ChainCode)
        );
        Assert.Equal(
            "8D18D3F0CF9D74B53A935D97E8DE85955ED9F6EEFC6D6D45F0C169031A11B669",
            Convert.ToHexString(masterKeyFromSeed.PrivateKey)
        );

        Assert.Equal(
            "026cf0d14fcfa930347e7da26281319ac5959d02f1b6331812261efdb7e347788b",
            ECPrivKey.Create(masterKeyFromSeed.PrivateKey).CreatePubKey().ToHex()
        );

        var der1 = BIP32.Instance.DerivePath(Case1SecP256K1.Path, TestCase.Seed.Span);
        Assert.True(der1.PrivateKey.SequenceEqual(Case1SecP256K1.Key.Span));
        Assert.True(der1.ChainCode.SequenceEqual(Case1SecP256K1.ChainCode.Span));
    }

    [Fact]
    public void OldDerivationTests()
    {
        var keysetId = new KeysetId("009a1f293253e41e");

        Assert.Equal(864559728, Nut13.GetKeysetIdInt(keysetId));
        var path = "m/129372'/0'/864559728'/{counter}'";
        var mnemonicPhrase =
            "half depart obvious quality work element tank gorilla view sugar picture humble";
        var mnemonic = new Mnemonic(mnemonicPhrase);
        Assert.Equal(
            "dd44ee516b0647e80b488e8dcc56d736a148f15276bef588b37057476d4b2b25780d3688a32b37353d6995997842c0fd8b412475c891c16310471fbc86dcbda8",
            Convert.ToHexString(mnemonic.DeriveSeed()).ToLowerInvariant()
        );

        Assert.Equal(
            "m/129372'/0'/864559728'/0'/0",
            Nut13.GetNut13DerivationPath(keysetId, 0, true)
        );
        Assert.Equal(
            "m/129372'/0'/864559728'/0'/1",
            Nut13.GetNut13DerivationPath(keysetId, 0, false)
        );

        Assert.Equal(
            "485875df74771877439ac06339e284c3acfcd9be7abf3bc20b516faeadfe77ae",
            mnemonic.DeriveSecret(keysetId, 0).Secret
        );
        Assert.Equal(
            "8f2b39e8e594a4056eb1e6dbb4b0c38ef13b1b2c751f64f810ec04ee35b77270",
            mnemonic.DeriveSecret(keysetId, 1).Secret
        );
        Assert.Equal(
            "bc628c79accd2364fd31511216a0fab62afd4a18ff77a20deded7b858c9860c8",
            mnemonic.DeriveSecret(keysetId, 2).Secret
        );
        Assert.Equal(
            "59284fd1650ea9fa17db2b3acf59ecd0f2d52ec3261dd4152785813ff27a33bf",
            mnemonic.DeriveSecret(keysetId, 3).Secret
        );
        Assert.Equal(
            "576c23393a8b31cc8da6688d9c9a96394ec74b40fdaf1f693a6bb84284334ea0",
            mnemonic.DeriveSecret(keysetId, 4).Secret
        );

        Assert.Equal(
            "ad00d431add9c673e843d4c2bf9a778a5f402b985b8da2d5550bf39cda41d679",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 0)).ToLowerInvariant()
        );
        Assert.Equal(
            "967d5232515e10b81ff226ecf5a9e2e2aff92d66ebc3edf0987eb56357fd6248",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 1)).ToLowerInvariant()
        );
        Assert.Equal(
            "b20f47bb6ae083659f3aa986bfa0435c55c6d93f687d51a01f26862d9b9a4899",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 2)).ToLowerInvariant()
        );
        Assert.Equal(
            "fb5fca398eb0b1deb955a2988b5ac77d32956155f1c002a373535211a2dfdc29",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 3)).ToLowerInvariant()
        );
        Assert.Equal(
            "5f09bfbfe27c439a597719321e061e2e40aad4a36768bb2bcc3de547c9644bf9",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 4)).ToLowerInvariant()
        );
    }

    [Fact]
    public void Nut13HMACTests()
    {
        KeysetId keysetId = new KeysetId(
            "015ba18a8adcd02e715a58358eb618da4a4b3791151a4bee5e968bb88406ccf76a"
        );
        Mnemonic mnemonic = new Mnemonic(
            "half depart obvious quality work element tank gorilla view sugar picture humble"
        );

        Assert.Equal(
            "db5561a07a6e6490f8dadeef5be4e92f7cebaecf2f245356b5b2a4ec40687298",
            mnemonic.DeriveSecret(keysetId, 0).Secret
        );
        Assert.Equal(
            "b70e7b10683da3bf1cdf0411206f8180c463faa16014663f39f2529b2fda922e",
            mnemonic.DeriveSecret(keysetId, 1).Secret
        );
        Assert.Equal(
            "78a7ac32ccecc6b83311c6081b89d84bb4128f5a0d0c5e1af081f301c7a513f5",
            mnemonic.DeriveSecret(keysetId, 2).Secret
        );
        Assert.Equal(
            "094a2b6c63bfa7970bc09cda0e1cfc9cd3d7c619b8e98fabcfc60aea9e4963e5",
            mnemonic.DeriveSecret(keysetId, 3).Secret
        );
        Assert.Equal(
            "5e89fc5d30d0bf307ddf0a3ac34aa7a8ee3702169dafa3d3fe1d0cae70ecd5ef",
            mnemonic.DeriveSecret(keysetId, 4).Secret
        );

        Assert.Equal(
            "6d26181a3695e32e9f88b80f039ba1ae2ab5a200ad4ce9dbc72c6d3769f2b035",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 0)).ToLowerInvariant()
        );
        Assert.Equal(
            "bde4354cee75545bea1a2eee035a34f2d524cee2bb01613823636e998386952e",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 1)).ToLowerInvariant()
        );
        Assert.Equal(
            "f40cc1218f085b395c8e1e5aaa25dccc851be3c6c7526a0f4e57108f12d6dac4",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 2)).ToLowerInvariant()
        );
        Assert.Equal(
            "099ed70fc2f7ac769bc20b2a75cb662e80779827b7cc358981318643030577d0",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 3)).ToLowerInvariant()
        );
        Assert.Equal(
            "5550337312d223ba62e3f75cfe2ab70477b046d98e3e71804eade3956c7b98cf",
            Convert.ToHexString(mnemonic.DeriveBlindingFactor(keysetId, 4)).ToLowerInvariant()
        );
    }
}