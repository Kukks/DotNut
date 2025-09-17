﻿using System.Buffers.Binary;
using System.Security.Cryptography;
using DotNut.NBitcoin.BIP39;
using NBip32Fast;

namespace DotNut.NUT13;

public static class Nut13
{
    public static byte[] DeriveBlindingFactor(this Mnemonic mnemonic, KeysetId keysetId, int counter) => 
        DeriveBlindingFactor(mnemonic.DeriveSeed(), keysetId, counter);
    

    public static StringSecret DeriveSecret(this Mnemonic mnemonic, KeysetId keysetId, int counter) => 
        DeriveSecret(mnemonic.DeriveSeed(), keysetId, counter);
    
    
    public static byte[] DeriveBlindingFactor(this byte[] seed, KeysetId keysetId, int counter)
    {
        switch (keysetId.ToString().Substring(0, 2))
        {
            case "00":
                return BIP32.Instance.DerivePath(GetNut13DerivationPath(keysetId, counter, false), seed).PrivateKey
                    .ToArray();
            case "01":
            {
                return DeriveHmac(seed, keysetId, counter, false);
            }
            default:
                throw new ArgumentException("Invalid keyset id prefix");
        }
    }
    public static StringSecret DeriveSecret(this byte[] seed, KeysetId keysetId, int counter)
    {
        switch (keysetId.GetVersion())
        {
            case 0x00:
                var key = BIP32.Instance.DerivePath(GetNut13DerivationPath(keysetId, counter, true), seed).PrivateKey; 
                return new StringSecret(Convert.ToHexString(key).ToLower());
            case 0x01:
            {
                var secretBytes = DeriveHmac(seed, keysetId, counter, true);
                return new StringSecret(Convert.ToHexString(secretBytes).ToLower());
            }
            default:
                throw new ArgumentException("Invalid keyset id prefix");
        }
                
    }
    
    public static byte[] DeriveHmac(byte[] seed, KeysetId keysetId, int counter, bool secretOrr)
    {
        byte[] counterBuffer = BitConverter.GetBytes((long)counter);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(counterBuffer);
        var message =  "Cashu_KDF_HMAC_SHA256"u8.ToArray()
            .Concat(Convert.FromHexString(keysetId.ToString()))
            .Concat(counterBuffer)
            .Append(secretOrr ? (byte)0x00 : (byte)0x01);
        
        using var hmac = new HMACSHA256(seed);
        return hmac.ComputeHash(message.ToArray());
    }
    
    public const string Purpose = "129372'";
    public static KeyPath GetNut13DerivationPath(KeysetId keysetId, int counter, bool secretOrr)
    { 
        return (KeyPath) KeyPath.Parse($"m/{Purpose}/0'/{GetKeysetIdInt(keysetId)}'/{counter}'/{(secretOrr?0:1)}")!;
    }
    
    public static long GetKeysetIdInt(KeysetId keysetId)
    {
        var keysetIdInt = long.Parse("0" + keysetId, System.Globalization.NumberStyles.HexNumber);
        var mod = (long )Math.Pow(2, 31) - 1;
        return keysetIdInt % mod;
    }
}