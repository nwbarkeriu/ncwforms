using System.Text.RegularExpressions;

namespace JobCompareApp.Services
{
    public static class ClientMatcher
    {
        // Base exception rules (automatically made bidirectional)
        private static readonly Dictionary<string, string> BaseExceptionRules = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Complete Mechanical Services", "CMS- Complete Mechanical Services" },
            { "LCI", "Lippert" },
            { "JBI Electrical Systems", "OWL Services (JBI Electrical Systems Inc.)" },
            { "TJ Maxx ARRC", "TJX Companies" },
            { "Carl Nelson & Company", "Carl A Nelson" },
            { "Elgin Separation Solutions", "Elgin Power Solutions" },
            { "Gaylor Electric", "Gaylor Group" },
            { "InPwr Inc", "In Pwr Inc" }
        };

        // Bidirectional exception rules (automatically includes both directions)
        private static readonly Dictionary<string, string> ExceptionRules = CreateBidirectionalRules(BaseExceptionRules);

        /// <summary>
        /// Creates a bidirectional dictionary from base rules (both A→B and B→A)
        /// </summary>
        private static Dictionary<string, string> CreateBidirectionalRules(Dictionary<string, string> baseRules)
        {
            var bidirectional = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var rule in baseRules)
            {
                // Add original direction
                bidirectional[rule.Key] = rule.Value;
                // Add reverse direction
                bidirectional[rule.Value] = rule.Key;
            }
            
            return bidirectional;
        }

        /// <summary>
        /// FuzzyWuzzy-style client name matching with multiple algorithms
        /// </summary>
        public static (string? BestMatch, double Score, string Reason) ExtractBestClientMatch(string target, IEnumerable<string> choices, double threshold = 0.7)
        {
            if (string.IsNullOrWhiteSpace(target))
                return (null, 0, "Empty target");

            var normalizedTarget = NormalizeClientName(target);
            var bestMatch = choices
                .Select(choice => new { 
                    Name = choice, 
                    Score = CalculateClientSimilarity(normalizedTarget, NormalizeClientName(choice)),
                    Reason = GetClientMatchReason(normalizedTarget, NormalizeClientName(choice))
                })
                .Where(x => x.Score >= threshold)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch != null 
                ? (bestMatch.Name, bestMatch.Score, bestMatch.Reason)
                : (null, 0, $"No match above {threshold:P0} threshold");
        }

        /// <summary>
        /// Calculates similarity between client names using multiple algorithms
        /// </summary>
        private static double CalculateClientSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return 0;

            var exactScore = string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0;
            var ratioScore = CalculateLevenshteinRatio(name1, name2);
            var firstWordsScore = CalculateFirstWordsScore(name1, name2);
            var containsScore = CalculateContainsScore(name1, name2);

            return new[] { exactScore, ratioScore, firstWordsScore, containsScore }.Max();
        }

        /// <summary>
        /// Basic Levenshtein ratio for client names
        /// </summary>
        private static double CalculateLevenshteinRatio(string s1, string s2)
        {
            var maxLength = Math.Max(s1.Length, s2.Length);
            if (maxLength == 0) return 1.0;

            var distance = CalculateLevenshteinDistance(s1.ToLower(), s2.ToLower());
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Simple Levenshtein distance implementation
        /// </summary>
        private static int CalculateLevenshteinDistance(string s1, string s2)
        {
            var matrix = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                matrix[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[s1.Length, s2.Length];
        }

        /// <summary>
        /// Calculates score based on first words matching
        /// </summary>
        private static double CalculateFirstWordsScore(string name1, string name2)
        {
            var firstWords1 = GetFirstNWords(name1, 2);
            var firstWords2 = GetFirstNWords(name2, 2);

            if (string.IsNullOrEmpty(firstWords1) || string.IsNullOrEmpty(firstWords2))
                return 0;

            return string.Equals(firstWords1, firstWords2, StringComparison.OrdinalIgnoreCase) ? 0.85 : 0;
        }

        /// <summary>
        /// Calculates score based on contains logic
        /// </summary>
        private static double CalculateContainsScore(string name1, string name2)
        {
            if (name1.Length < 3 || name2.Length < 3)
                return 0;

            if (name1.Contains(name2, StringComparison.OrdinalIgnoreCase) || 
                name2.Contains(name1, StringComparison.OrdinalIgnoreCase))
                return 0.75;

            return 0;
        }

        /// <summary>
        /// Determines which matching algorithm produced the best score for clients
        /// </summary>
        private static string GetClientMatchReason(string name1, string name2)
        {
            if (string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase))
                return "Exact match";

            var ratioScore = CalculateLevenshteinRatio(name1, name2);
            var firstWordsScore = CalculateFirstWordsScore(name1, name2);
            var containsScore = CalculateContainsScore(name1, name2);

            if (firstWordsScore > 0)
                return $"First words match ({firstWordsScore:P0})";
            if (containsScore > 0)
                return $"Contains match ({containsScore:P0})";
            if (ratioScore > 0)
                return $"Similarity match ({ratioScore:P0})";

            return "No significant match";
        }

        /// <summary>
        /// Finds the best matching Avionte client name for a given QuickBooks client name
        /// Enhanced with better exception rule handling and fuzzy matching
        /// </summary>
        public static string? FindBestAvionteMatch(string qbClientName, IEnumerable<string> avionteClientNames, 
            Dictionary<string, decimal>? qbAmountsByClient = null, Dictionary<string, decimal>? avionteAmountsByClient = null)
        {
            if (string.IsNullOrWhiteSpace(qbClientName))
                return null;

            var avionteNames = avionteClientNames.ToList();
            if (!avionteNames.Any())
                return null;

            // 1. Check exception rules first (HIGHEST PRIORITY)
            if (ExceptionRules.ContainsKey(qbClientName))
            {
                var mappedName = ExceptionRules[qbClientName];
                // Try exact match first
                var exactMatch = avionteNames.FirstOrDefault(a => string.Equals(a, mappedName, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                    return exactMatch;
                
                // If exact match fails, try fuzzy matching on the mapped name
                var fuzzyMatch = avionteNames
                    .Select(a => new { Name = a, Score = CalculateClientSimilarity(mappedName, a) })
                    .Where(x => x.Score >= 0.75) // Lower threshold for exception rule matches
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();
                
                if (fuzzyMatch != null)
                    return fuzzyMatch.Name;
            }

            // 2. Try exact match
            var exactMatchResult = avionteNames.FirstOrDefault(a => string.Equals(a, qbClientName, StringComparison.OrdinalIgnoreCase));
            if (exactMatchResult != null)
                return exactMatchResult;

            // 3. Try amount-based matching with enhanced name similarity checks
            if (qbAmountsByClient != null && avionteAmountsByClient != null && qbAmountsByClient.ContainsKey(qbClientName))
            {
                var qbAmount = qbAmountsByClient[qbClientName];
                
                var amountMatches = avionteNames
                    .Where(a => avionteAmountsByClient.ContainsKey(a))
                    .Select(a => new { 
                        Name = a, 
                        Amount = avionteAmountsByClient[a],
                        NameSimilarity = CalculateClientSimilarity(NormalizeClientName(qbClientName), NormalizeClientName(a))
                    })
                    .Where(x => {
                        // Check if amounts zero out (same absolute value, potentially opposite signs)
                        var amountsZeroOut = Math.Abs(Math.Abs(qbAmount) - Math.Abs(x.Amount)) < 0.01m && 
                                           Math.Abs(qbAmount + x.Amount) < 0.01m;
                        
                        // For amount zero-out matches, require some name similarity (relaxed threshold)
                        return amountsZeroOut && x.NameSimilarity >= 0.3; // Very relaxed for zero-out amounts
                    })
                    .OrderByDescending(x => x.NameSimilarity)
                    .FirstOrDefault();
                
                if (amountMatches != null)
                    return amountMatches.Name;
            }

            // 4. Enhanced fuzzy matching with multiple algorithms
            var fuzzyResults = avionteNames
                .Select(a => new { 
                    Name = a, 
                    Score = CalculateClientSimilarity(NormalizeClientName(qbClientName), NormalizeClientName(a)),
                    Reason = GetClientMatchReason(NormalizeClientName(qbClientName), NormalizeClientName(a))
                })
                .Where(x => x.Score >= 0.7) // Standard threshold for fuzzy matches
                .OrderByDescending(x => x.Score)
                .ToList();

            if (fuzzyResults.Any())
                return fuzzyResults.First().Name;

            // 5. Fallback: Try very relaxed matching for potential matches
            var relaxedMatch = avionteNames
                .Select(a => new { Name = a, Score = CalculateClientSimilarity(NormalizeClientName(qbClientName), NormalizeClientName(a)) })
                .Where(x => x.Score >= 0.5) // Very relaxed threshold
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return relaxedMatch?.Name;
        }

        /// <summary>
        /// Finds the best matching QuickBooks client name for a given Avionte client name
        /// Enhanced with bidirectional exception rule handling
        /// </summary>
        public static string? FindBestQBMatch(string avionteClientName, IEnumerable<string> qbClientNames)
        {
            if (string.IsNullOrWhiteSpace(avionteClientName))
                return null;

            var qbNames = qbClientNames.ToList();
            if (!qbNames.Any())
                return null;

            // 1. Check bidirectional exception rules first
            if (ExceptionRules.ContainsKey(avionteClientName))
            {
                var mappedName = ExceptionRules[avionteClientName];
                var exactMatch = qbNames.FirstOrDefault(q => string.Equals(q, mappedName, StringComparison.OrdinalIgnoreCase));
                if (exactMatch != null)
                    return exactMatch;
                
                // Try fuzzy matching on the mapped name if exact fails
                var fuzzyMatch = qbNames
                    .Select(q => new { Name = q, Score = CalculateClientSimilarity(mappedName, q) })
                    .Where(x => x.Score >= 0.75)
                    .OrderByDescending(x => x.Score)
                    .FirstOrDefault();
                
                if (fuzzyMatch != null)
                    return fuzzyMatch.Name;
            }

            // 2. Try exact match
            var exactMatchResult = qbNames.FirstOrDefault(q => string.Equals(q, avionteClientName, StringComparison.OrdinalIgnoreCase));
            if (exactMatchResult != null)
                return exactMatchResult;

            // 3. Try normalized comparison
            var normalizedAvionte = NormalizeClientName(avionteClientName);
            var normalizedMatch = qbNames.FirstOrDefault(q => string.Equals(NormalizeClientName(q), normalizedAvionte, StringComparison.OrdinalIgnoreCase));
            if (normalizedMatch != null)
                return normalizedMatch;

            // 4. Try first 2 words matching
            var avionteFirstWords = GetFirstNWords(normalizedAvionte, 2);
            if (!string.IsNullOrEmpty(avionteFirstWords))
            {
                var firstWordsMatch = qbNames.FirstOrDefault(q =>
                {
                    var qbFirstWords = GetFirstNWords(NormalizeClientName(q), 2);
                    return !string.IsNullOrEmpty(qbFirstWords) && 
                           string.Equals(avionteFirstWords, qbFirstWords, StringComparison.OrdinalIgnoreCase);
                });
                if (firstWordsMatch != null)
                    return firstWordsMatch;
            }

            // 5. Try contains matching
            var containsMatch = qbNames.FirstOrDefault(q =>
            {
                var normalizedQB = NormalizeClientName(q);
                return normalizedQB.Contains(normalizedAvionte, StringComparison.OrdinalIgnoreCase) ||
                       normalizedAvionte.Contains(normalizedQB, StringComparison.OrdinalIgnoreCase);
            });
            if (containsMatch != null)
                return containsMatch;

            // 6. No match found
            return null;
        }

        /// <summary>
        /// Creates unified client matches for reconciliation
        /// Returns a dictionary where key is the unified name and value contains both QB and Avionte matches
        /// </summary>
        public static Dictionary<string, ClientMatch> CreateUnifiedMatches(
            IEnumerable<string> qbClientNames, 
            IEnumerable<string> avionteClientNames,
            Dictionary<string, decimal>? qbAmountsByClient = null,
            Dictionary<string, decimal>? avionteAmountsByClient = null)
        {
            var matches = new Dictionary<string, ClientMatch>(StringComparer.OrdinalIgnoreCase);
            var qbNames = qbClientNames.Distinct().ToList();
            var avionteNames = avionteClientNames.Distinct().ToList();
            var processedAvionte = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Process QB names first, finding their Avionte matches
            foreach (var qbName in qbNames)
            {
                var avionteMatch = FindBestAvionteMatch(qbName, avionteNames, qbAmountsByClient, avionteAmountsByClient);
                var unifiedName = qbName; // Use QB name as the unified name

                if (avionteMatch != null)
                {
                    // Found a match
                    matches[unifiedName] = new ClientMatch
                    {
                        UnifiedName = unifiedName,
                        QBName = qbName,
                        AvionteName = avionteMatch,
                        MatchType = GetMatchType(qbName, avionteMatch, qbAmountsByClient, avionteAmountsByClient)
                    };
                    processedAvionte.Add(avionteMatch);
                }
                else
                {
                    // No Avionte match found
                    matches[unifiedName] = new ClientMatch
                    {
                        UnifiedName = unifiedName,
                        QBName = qbName,
                        AvionteName = null,
                        MatchType = MatchType.QBOnly
                    };
                }
            }

            // Process remaining Avionte names that weren't matched
            foreach (var avionteName in avionteNames.Where(a => !processedAvionte.Contains(a)))
            {
                var unifiedName = avionteName; // Use Avionte name as unified name for unmatched

                matches[unifiedName] = new ClientMatch
                {
                    UnifiedName = unifiedName,
                    QBName = null,
                    AvionteName = avionteName,
                    MatchType = MatchType.AvionteOnly
                };
            }

            return matches;
        }

        /// <summary>
        /// Normalizes a client name by removing special characters, company suffixes, and extra whitespace
        /// </summary>
        private static string NormalizeClientName(string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
                return string.Empty;

            var normalized = clientName.Trim();

            // Remove common company suffixes (case insensitive)
            var suffixPattern = @"\b(LLC|Inc\.?|Corp\.?|Corporation|Company|Co\.?|Ltd\.?|Limited)\b";
            normalized = Regex.Replace(normalized, suffixPattern, "", RegexOptions.IgnoreCase);

            // Remove special characters but keep letters, numbers, and spaces
            normalized = Regex.Replace(normalized, @"[^\w\s]", " ");

            // Normalize whitespace
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Gets the first N words from a normalized client name
        /// </summary>
        private static string GetFirstNWords(string normalizedName, int wordCount)
        {
            if (string.IsNullOrWhiteSpace(normalizedName))
                return string.Empty;

            var words = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return string.Empty;

            var firstWords = words.Take(wordCount).ToArray();
            return string.Join(" ", firstWords);
        }

        /// <summary>
        /// Determines the type of match between QB and Avionte names
        /// Enhanced with bidirectional exception rule detection
        /// </summary>
        private static MatchType GetMatchType(string qbName, string avionteName, 
            Dictionary<string, decimal>? qbAmountsByClient = null, Dictionary<string, decimal>? avionteAmountsByClient = null)
        {
            if (string.Equals(qbName, avionteName, StringComparison.OrdinalIgnoreCase))
                return MatchType.Exact;

            // Check bidirectional exception rules (now automatically handled)
            if (ExceptionRules.ContainsKey(qbName) && string.Equals(ExceptionRules[qbName], avionteName, StringComparison.OrdinalIgnoreCase))
                return MatchType.ExceptionRule;

            // Check if this was an amount-based match (amounts that zero out AND names are similar)
            if (qbAmountsByClient != null && avionteAmountsByClient != null && 
                qbAmountsByClient.ContainsKey(qbName) && avionteAmountsByClient.ContainsKey(avionteName))
            {
                var qbAmount = qbAmountsByClient[qbName];
                var avionteAmount = avionteAmountsByClient[avionteName];
                var amountsZeroOut = Math.Abs(Math.Abs(qbAmount) - Math.Abs(avionteAmount)) < 0.01m && 
                                   Math.Abs(qbAmount + avionteAmount) < 0.01m;
                
                if (amountsZeroOut)
                {
                    // Verify name similarity as well
                    var normalizedQBForCheck = NormalizeClientName(qbName);
                    var normalizedAvionteForCheck = NormalizeClientName(avionteName);
                    var qbFirstWordsForCheck = GetFirstNWords(normalizedQBForCheck, 2);
                    var avionteFirstWordsForCheck = GetFirstNWords(normalizedAvionteForCheck, 2);
                    
                    var namesSimilar = string.Equals(normalizedQBForCheck, normalizedAvionteForCheck, StringComparison.OrdinalIgnoreCase) ||
                                     (!string.IsNullOrEmpty(qbFirstWordsForCheck) && !string.IsNullOrEmpty(avionteFirstWordsForCheck) && 
                                      string.Equals(qbFirstWordsForCheck, avionteFirstWordsForCheck, StringComparison.OrdinalIgnoreCase)) ||
                                     (normalizedQBForCheck.Length > 3 && normalizedAvionteForCheck.Contains(normalizedQBForCheck, StringComparison.OrdinalIgnoreCase)) ||
                                     (normalizedAvionteForCheck.Length > 3 && normalizedQBForCheck.Contains(normalizedAvionteForCheck, StringComparison.OrdinalIgnoreCase));
                    
                    if (namesSimilar)
                        return MatchType.AmountZeroOut;
                }
            }

            if (string.Equals(NormalizeClientName(qbName), NormalizeClientName(avionteName), StringComparison.OrdinalIgnoreCase))
                return MatchType.Normalized;

            var qbFirstWords = GetFirstNWords(NormalizeClientName(qbName), 2);
            var avionteFirstWords = GetFirstNWords(NormalizeClientName(avionteName), 2);
            if (!string.IsNullOrEmpty(qbFirstWords) && 
                string.Equals(qbFirstWords, avionteFirstWords, StringComparison.OrdinalIgnoreCase))
                return MatchType.FirstWords;

            return MatchType.Fuzzy;
        }
    }

    /// <summary>
    /// Represents a matched client between QuickBooks and Avionte systems
    /// </summary>
    public class ClientMatch
    {
        public string UnifiedName { get; set; } = string.Empty;
        public string? QBName { get; set; }
        public string? AvionteName { get; set; }
        public MatchType MatchType { get; set; }

        public bool HasQBData => !string.IsNullOrEmpty(QBName);
        public bool HasAvionteData => !string.IsNullOrEmpty(AvionteName);
        public bool IsPerfectMatch => HasQBData && HasAvionteData;
    }

    /// <summary>
    /// Types of matches between client names
    /// </summary>
    public enum MatchType
    {
        Exact,          // Perfect string match
        ExceptionRule,  // Matched via custom exception rule
        AmountZeroOut,  // Matched because amounts zero out AND names are similar
        Normalized,     // Matched after normalization (removing LLC, special chars, etc.)
        FirstWords,     // Matched on first 2 words
        Fuzzy,          // Matched via contains logic
        QBOnly,         // Only exists in QuickBooks
        AvionteOnly     // Only exists in Avionte
    }
}
