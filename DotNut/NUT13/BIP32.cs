using System.Numerics;
using System.Security.Cryptography;
using NBip32Fast;
using NBip32Fast.Interfaces;
using NBitcoin.Secp256k1;

namespace DotNut.NUT13;

public class BIP32 : IHdKeyAlgo
{
    public static readonly IHdKeyAlgo Instance = new BIP32();
    private static readonly byte[] CurveBytes = "Bitcoin seed"u8.ToArray();

    private static readonly BigInteger N = Cashu.N;

    private BIP32() { }

    public HdKey GetMasterKeyFromSeed(ReadOnlySpan<byte> seed)
    {
        var seedCopy = new Span<byte>(seed.ToArray());
        while (true)
        {
            HMACSHA512.HashData(CurveBytes, seedCopy, seedCopy);
            var key = seedCopy[..32];
            var keyInt = new BigInteger(key, true, true);
            if (keyInt > N || keyInt.IsZero)
                continue;
            return new HdKey(key, seedCopy[32..]);
        }
    }

    public HdKey Derive(HdKey parent, KeyPathElement index)
    {
        Span<byte> hash = index.Hardened
            ? IHdKeyAlgo.Bip32Hash(parent.ChainCode, index, 0x00, parent.PrivateKey)
            : IHdKeyAlgo.Bip32Hash(parent.ChainCode, index, GetPublic(parent.PrivateKey));

        var parentKey = new BigInteger(parent.PrivateKey, true, true);

        while (true)
        {
            var key = hash[..32];
            var cc = hash[32..];
            key.Reverse();
            var keyInt = new BigInteger(key, true);
            var res = BigInteger.Add(keyInt, parentKey) % N;

            if (keyInt > N || res.IsZero)
            {
                hash = IHdKeyAlgo.Bip32Hash(parent.ChainCode, index, 0x01, cc);
                continue;
            }

            var keyBytes = res.ToByteArray(true, true);
            if (keyBytes.Length < 32)
            {
                var paddedKey = new byte[32];
                keyBytes.CopyTo(paddedKey, 32 - keyBytes.Length);
                keyBytes = paddedKey;
            }
            return new HdKey(keyBytes, cc);
        }
    }

    public byte[] GetPublic(ReadOnlySpan<byte> privateKey)
    {
        return ECPrivKey.Create(privateKey).CreatePubKey().ToBytes();
    }
}
