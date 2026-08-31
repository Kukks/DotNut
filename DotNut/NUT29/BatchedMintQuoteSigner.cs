using DotNut.ApiModels;

namespace DotNut;

public static class BatchedMintQuoteSigner
{
    /// <summary>
    /// Signs the locked quotes of a batch. Each signature is an ordinary NUT-20 signature over
    /// one quote id and the batch's whole <c>outputs</c> array — the outputs are one consolidated
    /// set, not partitioned per quote.
    /// </summary>
    /// <param name="keysByQuoteId">
    /// Keys for the locked quotes, by quote id. Quotes missing from this get a null signature,
    /// as the spec requires for unlocked quotes.
    /// </param>
    public static PostBatchedMintRequest Sign(
        this PostBatchedMintRequest request,
        IReadOnlyDictionary<string, PrivKey> keysByQuoteId
    )
    {
        var outputs = request.Outputs.ToList();
        var signatures = new string?[request.QuoteIds.Length];
        var anyLocked = false;

        for (var i = 0; i < request.QuoteIds.Length; i++)
        {
            if (!keysByQuoteId.TryGetValue(request.QuoteIds[i], out var key))
            {
                continue;
            }

            signatures[i] = key.SignMintQuote(request.QuoteIds[i], outputs);
            anyLocked = true;
        }

        // The field may be left out entirely when no quote is locked.
        request.Signatures = anyLocked ? signatures : null;
        return request;
    }
}
