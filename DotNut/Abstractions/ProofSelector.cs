using System.Diagnostics;

namespace DotNut.Abstractions;

// Borrowed from cashu-ts 
// see https://github.com/cashubtc/cashu-ts/pull/314
public class ProofSelector : IProofSelector
{
    private class ProofWithFee
    {
        public Proof Proof { get; set; }
        public double ExFee { get; set; }
        public ulong PpkFee { get; set; }

        public ProofWithFee(Proof proof, double exFee, ulong ppkFee)
        {
            Proof = proof;
            ExFee = exFee;
            PpkFee = ppkFee;
        }
    }

    private class Timer
    {
        private readonly Stopwatch _stopwatch;

        public Timer()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public long Elapsed() => _stopwatch.ElapsedMilliseconds;
    }

    private readonly Dictionary<KeysetId, ulong> _keysetFees;

    /// <summary>
    /// Creates a new ProofSelector instance.
    /// </summary>
    /// <param name="keysetFees">Dictionary mapping keyset IDs to their per-proof-per-thousand fees</param>
    /// <param name="logger">Optional logger action for debug information</param>
    public ProofSelector(Dictionary<KeysetId, ulong> keysetFees)
    {
        _keysetFees = keysetFees ?? throw new ArgumentNullException(nameof(keysetFees));
    }

    /// <summary>
    /// Gets the fee per thousand for a specific proof.
    /// </summary>
    /// <param name="proof">The proof to get fee for</param>
    /// <returns>Fee per thousand units</returns>
    private ulong GetProofFeePPK(Proof proof)
    {
        return _keysetFees.TryGetValue(proof.Id, out var fee) ? fee : 0;
    }

    public async Task<SendResponse> SelectProofsToSend(List<Proof> proofs, ulong amountToSend, bool includeFees = false, CancellationToken ct = default)
    {
        // Init vars
        const int MAX_TRIALS = 60; // 40-80 is optimal (per RGLI paper)
        const double MAX_OVRPCT = 0; // Acceptable close match overage (percent)
        const ulong MAX_OVRAMT = 0; // Acceptable close match overage (absolute)
        const long MAX_TIMEMS = 1000; // Halt new trials if over time (in ms)
        const int MAX_P2SWAP = 5000; // Max number of Phase 2 improvement swaps
        const bool exactMatch = false; // Allows close match (> amountToSend + fee)
        
        var timer = new Timer(); // start the clock
        List<ProofWithFee>? bestSubset = null;
        double bestDelta = double.PositiveInfinity;
        ulong bestAmount = 0;
        ulong bestFeePPK = 0;

        /*
         * Helper Functions.
         */
        
        // Calculate net amount after fees
        double SumExFees(ulong amount, ulong feePPK)
        {
            return amount - (includeFees ? Math.Ceiling(feePPK / 1000.0) : 0);
        }

        // Shuffle array for randomization
        List<T> ShuffleArray<T>(IEnumerable<T> array)
        {
            var shuffled = array.ToList();
            var random = new Random();
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            return shuffled;
        }

        // Performs a binary search on a sorted (ascending) array of ProofWithFee objects by exFee.
        // If lessOrEqual=true, returns the rightmost index where exFee <= value
        // If lessOrEqual=false, returns the leftmost index where exFee >= value
        int? BinarySearchIndex(List<ProofWithFee> arr, double value, bool lessOrEqual)
        {
            int left = 0, right = arr.Count - 1;
            int? result = null;
            
            while (left <= right)
            {
                int mid = (left + right) / 2;
                double midValue = arr[mid].ExFee;
                
                if (lessOrEqual ? midValue <= value : midValue >= value)
                {
                    result = mid;
                    if (lessOrEqual) 
                        left = mid + 1;
                    else 
                        right = mid - 1;
                }
                else
                {
                    if (lessOrEqual) 
                        right = mid - 1;
                    else 
                        left = mid + 1;
                }
            }
            return lessOrEqual ? result : (left < arr.Count ? left : null);
        }

        // Insert into array of ProofWithFee objects sorted by exFee
        void InsertSorted(List<ProofWithFee> arr, ProofWithFee obj)
        {
            double value = obj.ExFee;
            int left = 0, right = arr.Count;
            
            while (left < right)
            {
                int mid = (left + right) / 2;
                if (arr[mid].ExFee < value) 
                    left = mid + 1;
                else 
                    right = mid;
            }
            arr.Insert(left, obj);
        }

        // "Delta" is the excess over amountToSend including fees
        // plus a tiebreaker to favour lower PPK keysets
        // NB: Solutions under amountToSend are invalid (delta: Infinity)
        double CalculateDelta(ulong amount, ulong feePPK)
        {
            double netSum = SumExFees(amount, feePPK);
            if (netSum < amountToSend) 
                return double.PositiveInfinity; // no good
            return amount + feePPK / 1000.0 - amountToSend;
        }

        /*
         * Pre-processing.
         */
        ulong totalAmount = 0;
        ulong totalFeePPK = 0;
        var proofWithFees = proofs.Select(p =>
        {
            ulong ppkfee = GetProofFeePPK(p);
            double exFee = includeFees ? p.Amount - ppkfee / 1000.0 : p.Amount;
            var obj = new ProofWithFee(p, exFee, ppkfee);
            
            // Sum all economical proofs (filtered below)
            if (!includeFees || exFee > 0)
            {
                totalAmount += p.Amount;
                totalFeePPK += ppkfee;
            }
            return obj;
        }).ToList();

        // Filter uneconomical proofs (totals computed above)
        var spendableProofs = includeFees
            ? proofWithFees.Where(obj => obj.ExFee > 0).ToList()
            : proofWithFees;

        // Sort by exFee ascending
        spendableProofs.Sort((a, b) => a.ExFee.CompareTo(b.ExFee));

        // Remove proofs too large to be useful and adjust totals
        // Exact Match: Keep proofs where exFee <= amountToSend
        // Close Match: Keep proofs where exFee <= nextBiggerExFee
        if (spendableProofs.Count > 0)
        {
            int endIndex;
            if (exactMatch)
            {
                var rightIndex = BinarySearchIndex(spendableProofs, amountToSend, true);
                endIndex = rightIndex != null ? rightIndex.Value + 1 : 0;
            }
            else
            {
                var biggerIndex = BinarySearchIndex(spendableProofs, amountToSend, false);
                if (biggerIndex != null)
                {
                    double nextBiggerExFee = spendableProofs[biggerIndex.Value].ExFee;
                    var rightIndex = BinarySearchIndex(spendableProofs, nextBiggerExFee, true);
                    if (rightIndex == null)
                    {
                        throw new InvalidOperationException("Unexpected null rightIndex in binary search");
                    }
                    endIndex = rightIndex.Value + 1;
                }
                else
                {
                    // Keep all proofs if all exFee < amountToSend
                    endIndex = spendableProofs.Count;
                }
            }

            // Adjust totals for removed proofs
            for (int i = endIndex; i < spendableProofs.Count; i++)
            {
                totalAmount -= spendableProofs[i].Proof.Amount;
                totalFeePPK -= spendableProofs[i].PpkFee;
            }
            spendableProofs = spendableProofs.Take(endIndex).ToList();
        }

        // Validate using precomputed totals
        double totalNetSum = SumExFees(totalAmount, totalFeePPK);
        if (amountToSend <= 0 || amountToSend > totalNetSum)
        {
            return new SendResponse { Keep = proofs, Send = new List<Proof>() };
        }

        // Max acceptable amount for non-exact matches
        double maxOverAmount = Math.Min(
            Math.Ceiling(amountToSend * (1 + MAX_OVRPCT / 100)),
            Math.Min(amountToSend + MAX_OVRAMT, totalNetSum));

        /*
         * RGLI algorithm: Runs multiple trials (up to MAX_TRIALS) Each trial starts with randomized
         * greedy subset (S) and then tries to improve that subset to get a valid solution. NOTE: Fees
         * are dynamic, based on number of proofs (PPK), so we perform all calculations based on net
         * amounts.
         */
        for (int trial = 0; trial < MAX_TRIALS; trial++)
        {
            // PHASE 1: Randomized Greedy Selection
            // Add proofs up to amountToSend (after adjusting for fees)
            // for exact match or the first amount over target otherwise
            var S = new List<ProofWithFee>();
            ulong amount = 0;
            ulong feePPK = 0;
            
            foreach (var obj in ShuffleArray(spendableProofs))
            {
                ulong newAmount = amount + obj.Proof.Amount;
                ulong newFeePPK = feePPK + obj.PpkFee;
                double netSum = SumExFees(newAmount, newFeePPK);
                
                if (exactMatch && netSum > amountToSend) 
                    break;
                    
                S.Add(obj);
                amount = newAmount;
                feePPK = newFeePPK;
                
                if (netSum >= amountToSend) 
                    break;
            }

            // PHASE 2: Local Improvement
            // Examine all the amounts found in the first phase, and find the
            // amount not in the current solution (others), which would get us
            // closest to the amountToSend.

            // Calculate the "others" array (note: spendableProofs is sorted ASC)
            // Using set.Contains() for filtering gives faster lookups: O(n+m)
            // Using array.Contains() would be way slower: O(n*m)
            var selectedCs = S.Select(pwf => pwf.Proof.C).ToHashSet();
            var others = spendableProofs.Where(obj => !selectedCs.Contains(obj.Proof.C)).ToList();
            
            // Generate a random order for accessing the trial subset ('S')
            var indices = ShuffleArray(Enumerable.Range(0, S.Count)).Take(MAX_P2SWAP).ToList();
            
            foreach (int i in indices)
            {
                // Exact or acceptable close match solution found?
                double netSum = SumExFees(amount, feePPK);
                if (Math.Abs(netSum - amountToSend) < 0.0001 ||
                    (!exactMatch && netSum >= amountToSend && netSum <= maxOverAmount))
                {
                    break;
                }

                // Get details for proof being replaced (objP), and temporarily
                // calculate the subset amount/fee with that proof removed.
                var objP = S[i];
                ulong tempAmount = amount - objP.Proof.Amount;
                ulong tempFeePPK = feePPK - objP.PpkFee;
                double tempNetSum = SumExFees(tempAmount, tempFeePPK);
                double target = amountToSend - tempNetSum;

                // Find a better replacement proof (objQ) and swap it in
                // Exact match can only replace larger to close on the target
                // Close match can replace larger or smaller as needed, but will
                // not replace larger unless it closes on the target
                var qIndex = BinarySearchIndex(others, target, exactMatch);
                if (qIndex != null)
                {
                    var objQ = others[qIndex.Value];
                    if (!exactMatch || objQ.ExFee > objP.ExFee)
                    {
                        if (target >= 0 || objQ.ExFee <= objP.ExFee)
                        {
                            S[i] = objQ;
                            amount = tempAmount + objQ.Proof.Amount;
                            feePPK = tempFeePPK + objQ.PpkFee;
                            others.RemoveAt(qIndex.Value);
                            InsertSorted(others, objP);
                        }
                    }
                }
            }

            // Update best solution
            double delta = CalculateDelta(amount, feePPK);
            if (delta < bestDelta)
            {
                
                bestSubset = S.OrderByDescending(a => a.ExFee).ToList(); // copy & sort
                bestDelta = delta;
                bestAmount = amount;
                bestFeePPK = feePPK;

                // "PHASE 3": Final check to make sure we haven't overpaid fees
                // and see if we can improve the solution. This is an adaptation
                // to the original RGLI, which helps us identify close match and
                // optimal fee solutions more consistently
                var tempS = bestSubset.ToList(); // copy
                while (tempS.Count > 1 && bestDelta > 0)
                {
                    var objP = tempS.Last();
                    tempS.RemoveAt(tempS.Count - 1);
                    
                    ulong tempAmount2 = amount - objP.Proof.Amount;
                    ulong tempFeePPK2 = feePPK - objP.PpkFee;
                    double tempDelta = CalculateDelta(tempAmount2, tempFeePPK2);
                    
                    if (double.IsPositiveInfinity(tempDelta)) 
                        break;
                        
                    if (tempDelta < bestDelta)
                    {
                        bestSubset = tempS.ToList();
                        bestDelta = tempDelta;
                        bestAmount = tempAmount2;
                        bestFeePPK = tempFeePPK2;
                        amount = tempAmount2;
                        feePPK = tempFeePPK2;
                    }
                }
            }

            // Check if solution is acceptable
            if (bestSubset != null && !double.IsPositiveInfinity(bestDelta))
            {
                double bestSum = SumExFees(bestAmount, bestFeePPK);
                if (Math.Abs(bestSum - amountToSend) < 0.0001 ||
                    (!exactMatch && bestSum >= amountToSend && bestSum <= maxOverAmount))
                {
                    break;
                }
            }

            // Time limit reached?
            if (timer.Elapsed() > MAX_TIMEMS)
            {
                if (exactMatch)
                {
                    throw new TimeoutException("Proof selection took too long. Try again with a smaller proof set.");
                }
                else
                {
                    break;
                }
            }
        }

        // Return Result
        if (bestSubset != null && !double.IsPositiveInfinity(bestDelta))
        {
            var bestProofs = bestSubset.Select(obj => obj.Proof).ToList();
            var bestProofCs = bestProofs.Select(p => p.C).ToHashSet();
            var keep = proofs.Where(p => !bestProofCs.Contains(p.C)).ToList();
            
            return new SendResponse { Keep = keep, Send = bestProofs };
        }

        return new SendResponse { Keep = proofs, Send = new List<Proof>() };
    }
}
