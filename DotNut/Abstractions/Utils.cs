using System.Security.Cryptography;
using DotNut.NUT13;

namespace DotNut.Abstractions;

public static class Utils
{
    /// <summary>
    /// Function mapping payment amount to keyset supported amounts in order to create swap payload. Always tries to fit the biggest proof.
    /// </summary>
    /// <param name="paymentAmount">Amount that has to be covered.</param>
    /// <param name="keyset">Mints keyset></param>
    /// <returns>List of ulong proof amounts for given keyset</returns>
    public static List<ulong> SplitToProofsAmounts(ulong paymentAmount, Keyset keyset)
    {
        var outputAmounts = new List<ulong>();
        var possibleValues = keyset.Keys.OrderByDescending(x => x).ToList();
        foreach (var value in possibleValues)
        {
            while (paymentAmount >= value)
            {
                outputAmounts.Add(value);
                paymentAmount -= value;
            }

            if (paymentAmount == 0)
            {
                break;
            }
        }

        return outputAmounts;
    }
    
    /// <summary>
    /// Creates blank outputs (see nut-08)
    /// </summary>
    /// <param name="amount">Amount that blank outputs have to cover</param>
    /// <param name="keysetId">Active keyset id which will sign outputs</param>
    /// <param name="keys">Keys for given KeysetId</param>
    /// <returns>Blank Outputs</returns>
    public static List<OutputData> CreateBlankOutputs(ulong amount, KeysetId keysetId, Keyset keys, NBitcoin.BIP39.Mnemonic? mnemonic = null, int? counter = null)
    {
        if (amount == 0)
        {
            throw new ArgumentException("Cannot create blank outputs zero amount.");
        }

        var count = CalculateNumberOfBlankOutputs(amount);

        // Amount is set for 1, they're blank. Mint will automatically set their amount and sign each by pk corresponding to value
        var amounts = Enumerable.Repeat((ulong)1, count).ToList();
        return CreateOutputs(amounts, keysetId, keys, mnemonic, counter);
    }
    
    /// <summary>
    /// Calculates amount of blank outputs needed by mint to return overpaid fees
    /// </summary>
    /// <param name="amountToCover">Amount of tokens that has to be covered by mint.</param>
    /// <returns>Integer amount of blank outputs needed</returns>
    public static int CalculateNumberOfBlankOutputs(ulong amountToCover)
    {
        if (amountToCover == 0)
        {
            return 0;
        }

        return Math.Max(
            Convert.ToInt32(
                Math.Ceiling(
                    Math.Log2(amountToCover)
                )
            ), 1);
    }
    
    
    /// <summary>
    /// Creates outputs (secrets, proof messages and blinding factors). Outputs should have valid amounts. 
    /// </summary>
    /// <param name="amounts">Amounts for each output (e.g. [1,2,4,8]</param>
    /// <param name="keysetId">ID of keyset we want to receive the proofs</param>
    /// <param name="keys">Keyset for given ID</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static List<OutputData> CreateOutputs(
        List<ulong> amounts,
        KeysetId keysetId,
        Keyset keys,
        NBitcoin.BIP39.Mnemonic? mnemonic = null,
        int? counter = null)
    {
        if (amounts.Any(a => !keys.Keys.Contains(a)))
            throw new ArgumentException("Invalid amounts");

        var outputs = new List<OutputData>(amounts.Count);
        

        if (mnemonic is not null && counter is { } c)
        {
            for (var i = 0; i < amounts.Count; i++)
            {
                var secret = mnemonic.DeriveSecret(keysetId, c + i);
                var r = new PrivKey(mnemonic.DeriveBlindingFactor(keysetId, c + i));
                var B_ = Cashu.ComputeB_(secret.ToCurve(), r);
                var output = new OutputData
                {
                    BlindedMessage = new BlindedMessage { Amount = amounts[i], B_ = B_, Id = keysetId },
                    BlindingFactor = r,
                    Secret = secret
                };
                outputs.Add(output);
            }
            return outputs;
        }
        
        foreach (var amount in amounts)
        {
            var secret = RandomSecret();
            var r = RandomPrivkey();
            var B_ = Cashu.ComputeB_(secret.ToCurve(), r);
            var output = new OutputData
            {
                BlindedMessage = new BlindedMessage { Amount = amount, B_ = B_, Id = keysetId },
                BlindingFactor = r,
                Secret = secret
            };
            outputs.Add(output);
        }
        return outputs;
    }

    
    /// <summary>
    /// Create P2Pk / HTLC outputs.
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="keysetId"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static OutputData CreateNut10Output(
        ulong amount,
        KeysetId keysetId,
        P2PkBuilder builder
    )
    {
        // ugliest hack ever
        Nut10Secret secret;
        if (builder is HTLCBuilder htlc)
        {
            secret = new Nut10Secret("HTLC", htlc.Build());
        }
        else
        {
            secret = new Nut10Secret("P2PK", builder.Build());
        }

        var r = RandomPrivkey();
        var B_ = Cashu.ComputeB_(secret.ToCurve(), r);
        return new OutputData
        {
            BlindedMessage = new BlindedMessage() { Amount = amount, B_ = B_, Id = keysetId },
            BlindingFactor = r,
            Secret = secret
        };
    }
    /// <summary>
    /// Creates P2Pk / HTLC Blinded Outputs
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="keysetId"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static OutputData CreateNut10BlindedOutput(ulong amount, KeysetId keysetId, P2PkBuilder builder)
    {
        // ugliest hack ever
        Nut10Secret secret;
        PubKey E;
        if (builder is HTLCBuilder htlc)
        {
            secret = new Nut10Secret("HTLC", htlc.BuildBlinded(keysetId, out var e));
            E = e;
        }
        else
        {
            secret = new Nut10Secret("P2PK", builder.BuildBlinded(keysetId, out var e));
            E = e;
        }

        var r = RandomPrivkey();
        var B_ = Cashu.ComputeB_(secret.ToCurve(), r);
        return new OutputData
        {
            BlindedMessage = new BlindedMessage() { Amount = amount, B_ = B_, Id = keysetId },
            BlindingFactor = r,
            Secret = secret,
            P2BkE = E
        };
    }
    /// <summary>
    /// Creates P2Pk / HTLC Blinded Outputs with specified ephemeral sender keypair. 
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="keysetId"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static OutputData CreateNut10BlindedOutput(ulong amount, KeysetId keysetId, P2PkBuilder builder, PrivKey e)
    {
        // ugliest hack ever
        Nut10Secret secret;
        if (builder is HTLCBuilder htlc)
        {
            secret = new Nut10Secret("HTLC", htlc.BuildBlinded(keysetId, e));
        }
        else
        {
            secret = new Nut10Secret("P2PK", builder.BuildBlinded(keysetId, e));
        }

        var r = RandomPrivkey();
        var B_ = Cashu.ComputeB_(secret.ToCurve(), r);
        return new OutputData
        {
            BlindedMessage = new BlindedMessage() { Amount = amount, B_ = B_, Id = keysetId },
            BlindingFactor = r,
            Secret = secret,
            P2BkE = e.Key.CreatePubKey()
        };
    }
    
    /// <summary>
    ///  Method creating proofs, from provided promises (blinded signatures)
    /// </summary>
    /// <param name="promise">Blinded Signature</param>
    /// <param name="r">Blinding factor</param>
    /// <param name="secret">Yeah, secret</param>
    /// <param name="amountPubkey">Key, corresponding to proof amount</param>
    /// <returns>Valid proof</returns>
    public static Proof ConstructProofFromPromise(
        BlindSignature promise,
        PrivKey r,
        ISecret secret,
        PubKey amountPubkey,
        PubKey? P2PkE = null)
    {

        //unblind signature
        var C = Cashu.ComputeC(promise.C_, r, amountPubkey);

        DLEQProof? dleq = null;
        if (promise.DLEQ is not null)
        {
            dleq = new DLEQProof
            {
                E = promise.DLEQ.E,
                S = promise.DLEQ.S,
                R = r
            };
        }

        return new Proof
        {
            Id = promise.Id,
            Amount = promise.Amount,
            Secret = secret,
            C = C,
            DLEQ = dleq,
            P2PkE = P2PkE
        };
    }

    public static List<Proof> ConstructProofsFromPromises(
        List<BlindSignature> promises,
        List<OutputData> outputs,
        Keyset keys
        )
    {
        List<Proof> proofs = new List<Proof>();
        for (int i = 0; i < promises.Count; i++)
        {
            if (!keys.TryGetValue(promises[i].Amount, out var key))
            {
                throw new ArgumentException($"Provided keyset doesn't contain PubKey for amount {promises[i].Amount}" );
            }
            var proof = ConstructProofFromPromise(
                promises[i],
                outputs[i].BlindingFactor,
                outputs[i].Secret,
                key,
                outputs[i].P2BkE
            );
            proofs.Add(proof);
        }
        return proofs;
    }

    public static ulong SumProofs(List<Proof> proofs)
    {
        return proofs.Aggregate(0UL, (current, proof) => current + proof.Amount);
    }

    
    public static ISecret RandomSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return new StringSecret(Convert.ToHexString(bytes));
    }

    public static PrivKey RandomPrivkey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return new PrivKey(Convert.ToHexString(bytes));
    }
}