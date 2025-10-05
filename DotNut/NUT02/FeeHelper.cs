using DotNut.ApiModels;

namespace DotNut;

public static class FeeHelper
{

    public static ulong ComputeFee(this IEnumerable<Proof> proofsToSpend, Dictionary<KeysetId, ulong> keysetFees)
    {
        ulong sum = 0;
        foreach (var proof in proofsToSpend)
        {
            if (keysetFees.TryGetValue(proof.Id, out var fee))
            {
                sum += fee;
            }
        }

        return (sum + 999) / 1000;
    }

    public static ulong Sum(this IEnumerable<ulong> ul)
    {
        return ul.Aggregate((x, y) => x + y);
    }
}