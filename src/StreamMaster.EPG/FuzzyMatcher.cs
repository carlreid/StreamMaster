namespace StreamMaster.EPG;

/// <summary>
/// A helper class to compute fuzzy matches between strings.
/// </summary>
public static class FuzzyMatcher
{
    /// <summary>
    /// Computes a Levenshtein distance between two strings. Lower is closer.
    /// </summary>
    /// <param name="s1">First string</param>
    /// <param name="s2">Second string</param>
    /// <returns>Levenshtein distance (0 = identical)</returns>
    public static int LevenshteinDistance(string s1, string s2)
    {
        s1 = s1.ToLowerInvariant();
        s2 = s2.ToLowerInvariant();

        int n = s1.Length;
        int m = s2.Length;
        int[,] d = new int[n + 1, m + 1];

        // Initialize the matrix
        for (int i = 0; i <= n; i++)
        {
            d[i, 0] = i;
        }
        for (int j = 0; j <= m; j++)
        {
            d[0, j] = j;
        }

        // Compute distances
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s2[j - 1] == s1[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>
    /// Scores how close two strings are. Lower is better (distance).
    /// </summary>
    /// <param name="s1">First string.</param>
    /// <param name="s2">Second string.</param>
    /// <returns>An integer representing closeness (Levenshtein distance).</returns>
    public static int Score(string s1, string s2)
    {
        return LevenshteinDistance(s1, s2);
    }
}
