using DotNut.ApiModels;

namespace DotNut;

public static class FeeHelper
{

    public static int ComputeFee(this IEnumerable<Proof> proofsToSpend, Dictionary<KeysetId, int> keysetFees)
    {
        var sum = 0;
        foreach (var proof in proofsToSpend)
        {
            if (keysetFees.TryGetValue(proof.Id, out var fee))
            {
                sum += fee;
            }
        }

        return (sum + 999) / 1000;
    }
}