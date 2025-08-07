using System.Text.RegularExpressions;

namespace JobCompareApp.Services
{
    public static class EmployeeMatcher
    {
        /// <summary>
        /// Finds the best matching Avionte employee name for a given QuickBooks employee name
        /// Enhanced with better fuzzy matching and amount consideration
        /// </summary>
        public static string? FindBestEmployeeMatch(string qbEmployeeName, IEnumerable<string> avionteEmployeeNames)
        {
            if (string.IsNullOrWhiteSpace(qbEmployeeName))
                return null;

            var avionteNames = avionteEmployeeNames.ToList();
            if (!avionteNames.Any())
                return null;

            // 1. Try exact match first
            var exactMatch = avionteNames.FirstOrDefault(a => string.Equals(a, qbEmployeeName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;

            // 2. Try normalized comparison (remove special chars, normalize spacing)
            var normalizedQB = NormalizeEmployeeName(qbEmployeeName);
            var normalizedMatch = avionteNames.FirstOrDefault(a => string.Equals(NormalizeEmployeeName(a), normalizedQB, StringComparison.OrdinalIgnoreCase));
            if (normalizedMatch != null)
                return normalizedMatch;

            // 3. Try name component matching (first name + last name variations)
            var qbComponents = ExtractNameComponents(normalizedQB);
            if (qbComponents.HasValidComponents)
            {
                var componentMatch = avionteNames.FirstOrDefault(a =>
                {
                    var avionteComponents = ExtractNameComponents(NormalizeEmployeeName(a));
                    return avionteComponents.HasValidComponents && AreNameComponentsSimilar(qbComponents, avionteComponents);
                });
                if (componentMatch != null)
                    return componentMatch;
            }

            // 3.5. Try first two words matching (specific case for names like "Antonio Mendez" vs "Antonio Mendez Morillo")
            var firstTwoWordsMatch = avionteNames.FirstOrDefault(a => AreFirstTwoWordsSimilar(normalizedQB, NormalizeEmployeeName(a)));
            if (firstTwoWordsMatch != null)
                return firstTwoWordsMatch;

            // 4. Enhanced fuzzy matching with multiple FuzzyWuzzy-style algorithms
            var fuzzyMatches = avionteNames
                .Select(a => new { 
                    Name = a, 
                    Score = CalculateNameSimilarity(normalizedQB, NormalizeEmployeeName(a)),
                    Reason = GetMatchReason(normalizedQB, NormalizeEmployeeName(a))
                })
                .Where(x => x.Score >= 0.8) // High threshold for employee names
                .OrderByDescending(x => x.Score)
                .ToList();

            if (fuzzyMatches.Any())
                return fuzzyMatches.First().Name;

            // 5. Fallback: Very relaxed matching for potential same person with different formatting
            var relaxedMatches = avionteNames
                .Select(a => new { 
                    Name = a, 
                    Score = CalculateNameSimilarity(normalizedQB, NormalizeEmployeeName(a)),
                    Reason = GetMatchReason(normalizedQB, NormalizeEmployeeName(a))
                })
                .Where(x => x.Score >= 0.6) // Very relaxed for potential matches
                .OrderByDescending(x => x.Score)
                .ToList();

            return relaxedMatches.FirstOrDefault()?.Name;
        }

        /// <summary>
        /// FuzzyWuzzy-style process.extractOne - finds best match with score and reasoning
        /// </summary>
        public static (string? BestMatch, double Score, string Reason) ExtractBestMatch(string target, IEnumerable<string> choices, double threshold = 0.8)
        {
            if (string.IsNullOrWhiteSpace(target))
                return (null, 0, "Empty target");

            var normalizedTarget = NormalizeEmployeeName(target);
            var bestMatch = choices
                .Select(choice => new { 
                    Name = choice, 
                    Score = CalculateNameSimilarity(normalizedTarget, NormalizeEmployeeName(choice)),
                    Reason = GetMatchReason(normalizedTarget, NormalizeEmployeeName(choice))
                })
                .Where(x => x.Score >= threshold)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch != null 
                ? (bestMatch.Name, bestMatch.Score, bestMatch.Reason)
                : (null, 0, $"No match above {threshold:P0} threshold");
        }

        /// <summary>
        /// Determines which matching algorithm produced the best score
        /// </summary>
        private static string GetMatchReason(string name1, string name2)
        {
            if (string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase))
                return "Exact match";

            var scores = new[]
            {
                (Score: CalculateRatio(name1, name2), Reason: "Basic similarity"),
                (Score: CalculatePartialRatio(name1, name2), Reason: "Partial match"),
                (Score: CalculateTokenSortRatio(name1, name2), Reason: "Word order similarity"),
                (Score: CalculateTokenSetRatio(name1, name2), Reason: "Word set similarity"),
                (Score: CalculateComponentSimilarity(name1, name2), Reason: "Name component match")
            };

            var bestScore = scores.OrderByDescending(s => s.Score).First();
            return $"{bestScore.Reason} ({bestScore.Score:P0})";
        }

        /// <summary>
        /// Enhanced employee matching that considers client context and amount information
        /// Uses a composite score that considers both name similarity and amount alignment
        /// </summary>
        public static string? FindBestEmployeeMatchWithContext(
            string qbEmployeeName, 
            IEnumerable<string> avionteEmployeeNames,
            string clientName,
            decimal qbAmount,
            Dictionary<string, decimal> avionteAmountsByEmployee)
        {
            if (string.IsNullOrWhiteSpace(qbEmployeeName))
                return null;

            var avionteNames = avionteEmployeeNames.ToList();
            if (!avionteNames.Any())
                return null;

            // Calculate composite scores for all potential matches
            var allMatches = avionteNames
                .Where(a => avionteAmountsByEmployee.ContainsKey(a))
                .Select(a => {
                    var nameSimilarity = CalculateNameSimilarity(NormalizeEmployeeName(qbEmployeeName), NormalizeEmployeeName(a));
                    var amountSimilarity = CalculateAmountSimilarity(qbAmount, avionteAmountsByEmployee[a]);
                    var amountMatch = Math.Abs(qbAmount - avionteAmountsByEmployee[a]) < 0.01m;
                    
                    var baseScore = nameSimilarity;
                    
                    // Boost score significantly for exact amount matches
                    if (amountMatch)
                    {
                        baseScore += 0.3; // Major boost for exact amounts
                    }
                    else
                    {
                        // Apply amount similarity multiplier
                        baseScore *= (0.3 + 0.7 * amountSimilarity);
                    }
                    
                    return new {
                        Name = a,
                        Amount = avionteAmountsByEmployee[a],
                        NameSimilarity = nameSimilarity,
                        AmountSimilarity = amountSimilarity,
                        AmountMatch = amountMatch,
                        CompositeScore = baseScore
                    };
                })
                .ToList();

            // Find the best match with minimum threshold
            var bestMatch = allMatches
                .Where(x => x.CompositeScore >= 0.6) // Require decent overall score
                .OrderByDescending(x => x.CompositeScore)
                .ThenByDescending(x => x.NameSimilarity) // Tie-breaker
                .FirstOrDefault();

            return bestMatch?.Name;
        }

        /// <summary>
        /// Calculates similarity between two amounts (0.0 to 1.0)
        /// </summary>
        private static double CalculateAmountSimilarity(decimal amount1, decimal amount2)
        {
            if (amount1 == 0 && amount2 == 0) return 1.0;
            if (amount1 == 0 || amount2 == 0) return 0.0;
            
            var maxAmount = Math.Max(Math.Abs(amount1), Math.Abs(amount2));
            var difference = Math.Abs(amount1 - amount2);
            
            return Math.Max(0.0, 1.0 - (double)(difference / maxAmount));
        }

        /// <summary>
        /// Creates unified employee matches for a specific client
        /// </summary>
        public static Dictionary<string, EmployeeMatch> CreateEmployeeMatches(
            IEnumerable<string> qbEmployeeNames,
            IEnumerable<string> avionteEmployeeNames,
            Dictionary<string, decimal> qbAmountsByEmployee,
            Dictionary<string, decimal> avionteAmountsByEmployee)
        {
            var matches = new Dictionary<string, EmployeeMatch>(StringComparer.OrdinalIgnoreCase);
            var qbNames = qbEmployeeNames.Distinct().ToList();
            var avionteNames = avionteEmployeeNames.Distinct().ToList();
            var processedAvionte = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Process QB employees first, finding their Avionte matches
            foreach (var qbName in qbNames)
            {
                var avionteMatch = FindBestEmployeeMatch(qbName, avionteNames);
                var unifiedName = qbName; // Use QB name as the unified name

                if (avionteMatch != null)
                {
                    // Found a match - check if amounts are consistent
                    var qbAmount = qbAmountsByEmployee.GetValueOrDefault(qbName, 0);
                    var avionteAmount = avionteAmountsByEmployee.GetValueOrDefault(avionteMatch, 0);
                    
                    matches[unifiedName] = new EmployeeMatch
                    {
                        UnifiedName = unifiedName,
                        QBName = qbName,
                        AvionteName = avionteMatch,
                        QBAmount = qbAmount,
                        AvionteAmount = avionteAmount,
                        MatchType = GetEmployeeMatchType(qbName, avionteMatch, qbAmount, avionteAmount)
                    };
                    processedAvionte.Add(avionteMatch);
                }
                else
                {
                    // No Avionte match found
                    var qbAmount = qbAmountsByEmployee.GetValueOrDefault(qbName, 0);
                    matches[unifiedName] = new EmployeeMatch
                    {
                        UnifiedName = unifiedName,
                        QBName = qbName,
                        AvionteName = null,
                        QBAmount = qbAmount,
                        AvionteAmount = 0,
                        MatchType = EmployeeMatchType.QBOnly
                    };
                }
            }

            // Process remaining Avionte employees that weren't matched
            foreach (var avionteName in avionteNames.Where(a => !processedAvionte.Contains(a)))
            {
                var avionteAmount = avionteAmountsByEmployee.GetValueOrDefault(avionteName, 0);
                matches[avionteName] = new EmployeeMatch
                {
                    UnifiedName = avionteName,
                    QBName = null,
                    AvionteName = avionteName,
                    QBAmount = 0,
                    AvionteAmount = avionteAmount,
                    MatchType = EmployeeMatchType.AvionteOnly
                };
            }

            return matches;
        }

        /// <summary>
        /// Normalizes an employee name by removing special characters and normalizing spacing
        /// </summary>
        public static string NormalizeEmployeeName(string employeeName)
        {
            if (string.IsNullOrWhiteSpace(employeeName))
                return string.Empty;

            var normalized = employeeName.Trim();

            // Replace hyphens, underscores, and other separators with spaces
            normalized = Regex.Replace(normalized, @"[-_\.]+", " ");

            // Remove special characters but keep letters, numbers, and spaces
            normalized = Regex.Replace(normalized, @"[^\w\s]", " ");

            // Normalize whitespace
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Extracts name components (first, middle, last) from a normalized name
        /// </summary>
        private static NameComponents ExtractNameComponents(string normalizedName)
        {
            if (string.IsNullOrWhiteSpace(normalizedName))
                return new NameComponents();

            var parts = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            return new NameComponents
            {
                FirstName = parts.Length > 0 ? parts[0] : "",
                MiddleName = parts.Length > 2 ? parts[1] : "",
                LastName = parts.Length > 1 ? parts[parts.Length - 1] : "",
                FullName = normalizedName
            };
        }

        /// <summary>
        /// Checks if the first two words of two names are similar (handles cases like "Antonio Mendez" vs "Antonio Mendez Morillo")
        /// </summary>
        private static bool AreFirstTwoWordsSimilar(string name1, string name2)
        {
            if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
                return false;

            var parts1 = name1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var parts2 = name2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Both names must have at least 2 words
            if (parts1.Length < 2 || parts2.Length < 2)
                return false;

            // Check if first two words match exactly
            var firstWordMatch = string.Equals(parts1[0], parts2[0], StringComparison.OrdinalIgnoreCase);
            var secondWordMatch = string.Equals(parts1[1], parts2[1], StringComparison.OrdinalIgnoreCase);

            return firstWordMatch && secondWordMatch;
        }

        /// <summary>
        /// Checks if two name components represent the same person
        /// </summary>
        private static bool AreNameComponentsSimilar(NameComponents name1, NameComponents name2)
        {
            // Both must have at least first name
            if (string.IsNullOrEmpty(name1.FirstName) || string.IsNullOrEmpty(name2.FirstName))
                return false;

            // First name must match (exact or similar)
            var firstNameMatch = string.Equals(name1.FirstName, name2.FirstName, StringComparison.OrdinalIgnoreCase) ||
                               CalculateStringSimilarity(name1.FirstName, name2.FirstName) >= 0.8;

            if (!firstNameMatch)
                return false;

            // For names with different word counts, check if the shorter name is a prefix of the longer one
            var name1Parts = name1.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var name2Parts = name2.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // If one name has fewer parts, check if all its parts match the beginning of the longer name
            if (name1Parts.Length != name2Parts.Length)
            {
                var shorterParts = name1Parts.Length < name2Parts.Length ? name1Parts : name2Parts;
                var longerParts = name1Parts.Length < name2Parts.Length ? name2Parts : name1Parts;

                // Check if the first N words of the longer name match the shorter name exactly
                for (int i = 0; i < shorterParts.Length; i++)
                {
                    if (!string.Equals(shorterParts[i], longerParts[i], StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true; // All parts of shorter name match the beginning of longer name
            }

            // For names with same number of parts, require first and last name to match
            if (!string.IsNullOrEmpty(name1.LastName) && !string.IsNullOrEmpty(name2.LastName))
            {
                var lastNameMatch = string.Equals(name1.LastName, name2.LastName, StringComparison.OrdinalIgnoreCase) ||
                                  CalculateStringSimilarity(name1.LastName, name2.LastName) >= 0.8;
                return lastNameMatch;
            }

            // If we can't determine last names clearly, fall back to fuzzy matching
            return CalculateStringSimilarity(name1.FullName, name2.FullName) >= 0.8;
        }

        /// <summary>
        /// Calculates similarity between two full names using multiple FuzzyWuzzy-style algorithms
        /// </summary>
        private static double CalculateNameSimilarity(string name1, string name2)
        {
            if (string.IsNullOrEmpty(name1) || string.IsNullOrEmpty(name2))
                return 0;

            // Try multiple similarity approaches and take the best score (FuzzyWuzzy style)
            var exactScore = string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0;
            var ratioScore = CalculateRatio(name1, name2);                    // Basic Levenshtein
            var partialRatioScore = CalculatePartialRatio(name1, name2);      // Best substring match
            var tokenSortScore = CalculateTokenSortRatio(name1, name2);       // Sort words first
            var tokenSetScore = CalculateTokenSetRatio(name1, name2);         // Compare unique words
            var componentScore = CalculateComponentSimilarity(name1, name2);   // Your existing logic

            return new[] { exactScore, ratioScore, partialRatioScore, tokenSortScore, tokenSetScore, componentScore }.Max();
        }

        /// <summary>
        /// FuzzyWuzzy-style Ratio: Basic Levenshtein distance similarity
        /// </summary>
        private static double CalculateRatio(string s1, string s2)
        {
            return CalculateStringSimilarity(s1.ToLower(), s2.ToLower());
        }

        /// <summary>
        /// FuzzyWuzzy-style Partial Ratio: Finds best substring match
        /// </summary>
        private static double CalculatePartialRatio(string s1, string s2)
        {
            s1 = s1.ToLower();
            s2 = s2.ToLower();

            var shorter = s1.Length <= s2.Length ? s1 : s2;
            var longer = s1.Length <= s2.Length ? s2 : s1;

            if (shorter.Length == 0)
                return 0;

            if (shorter.Length == longer.Length)
                return CalculateStringSimilarity(shorter, longer);

            var bestScore = 0.0;
            
            // Try all possible substrings of the longer string with the same length as shorter
            for (int i = 0; i <= longer.Length - shorter.Length; i++)
            {
                var substring = longer.Substring(i, shorter.Length);
                var score = CalculateStringSimilarity(shorter, substring);
                if (score > bestScore)
                    bestScore = score;
            }

            return bestScore;
        }

        /// <summary>
        /// FuzzyWuzzy-style Token Sort Ratio: Sorts words alphabetically before comparing
        /// </summary>
        private static double CalculateTokenSortRatio(string s1, string s2)
        {
            var tokens1 = s1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x);
            var tokens2 = s2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries).OrderBy(x => x);

            var sorted1 = string.Join(" ", tokens1);
            var sorted2 = string.Join(" ", tokens2);

            return CalculateStringSimilarity(sorted1, sorted2);
        }

        /// <summary>
        /// FuzzyWuzzy-style Token Set Ratio: Compares intersection and differences of word sets
        /// </summary>
        private static double CalculateTokenSetRatio(string s1, string s2)
        {
            var tokens1 = new HashSet<string>(s1.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var tokens2 = new HashSet<string>(s2.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries));

            var intersection = tokens1.Intersect(tokens2).OrderBy(x => x);
            var diff1 = tokens1.Except(tokens2).OrderBy(x => x);
            var diff2 = tokens2.Except(tokens1).OrderBy(x => x);

            var intersectionStr = string.Join(" ", intersection);
            var diff1Str = string.Join(" ", diff1);
            var diff2Str = string.Join(" ", diff2);

            var sorted1 = string.IsNullOrEmpty(diff1Str) ? intersectionStr : $"{intersectionStr} {diff1Str}".Trim();
            var sorted2 = string.IsNullOrEmpty(diff2Str) ? intersectionStr : $"{intersectionStr} {diff2Str}".Trim();

            if (string.IsNullOrEmpty(sorted1) && string.IsNullOrEmpty(sorted2))
                return 1.0;

            if (string.IsNullOrEmpty(sorted1) || string.IsNullOrEmpty(sorted2))
                return 0.0;

            return CalculateStringSimilarity(sorted1, sorted2);
        }

        /// <summary>
        /// Calculates string similarity using Levenshtein distance
        /// </summary>
        private static double CalculateStringSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;

            var maxLength = Math.Max(s1.Length, s2.Length);
            if (maxLength == 0) return 1.0;

            var distance = LevenshteinDistance(s1.ToLower(), s2.ToLower());
            return 1.0 - (double)distance / maxLength;
        }

        /// <summary>
        /// Calculates similarity based on name components
        /// </summary>
        private static double CalculateComponentSimilarity(string name1, string name2)
        {
            var comp1 = ExtractNameComponents(name1);
            var comp2 = ExtractNameComponents(name2);

            if (!comp1.HasValidComponents && !comp2.HasValidComponents)
                return 0;

            // Handle cases where one name is a prefix of another (like "Antonio Mendez" vs "Antonio Mendez Morillo")
            var name1Parts = name1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var name2Parts = name2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (name1Parts.Length != name2Parts.Length)
            {
                var shorterParts = name1Parts.Length < name2Parts.Length ? name1Parts : name2Parts;
                var longerParts = name1Parts.Length < name2Parts.Length ? name2Parts : name1Parts;

                // Check if shorter name is a perfect prefix of longer name
                bool isPerfectPrefix = true;
                for (int i = 0; i < shorterParts.Length; i++)
                {
                    if (!string.Equals(shorterParts[i], longerParts[i], StringComparison.OrdinalIgnoreCase))
                    {
                        isPerfectPrefix = false;
                        break;
                    }
                }

                if (isPerfectPrefix)
                {
                    // Perfect prefix match - very high confidence
                    return 0.95; // High but not perfect to distinguish from exact matches
                }

                // Check if first two words match (common case: "First Last" vs "First Last Additional")
                if (shorterParts.Length >= 2 && longerParts.Length >= 2)
                {
                    var firstTwoMatch = string.Equals(shorterParts[0], longerParts[0], StringComparison.OrdinalIgnoreCase) &&
                                       string.Equals(shorterParts[1], longerParts[1], StringComparison.OrdinalIgnoreCase);
                    if (firstTwoMatch)
                        return 0.85; // High confidence for first two words matching
                }
            }

            // Fall back to standard component matching
            if (!comp1.HasValidComponents || !comp2.HasValidComponents)
                return 0;

            var firstScore = CalculateStringSimilarity(comp1.FirstName, comp2.FirstName);
            var lastScore = CalculateStringSimilarity(comp1.LastName, comp2.LastName);

            // Weight first and last name equally
            return (firstScore + lastScore) / 2.0;
        }

        /// <summary>
        /// Calculates Levenshtein distance between two strings
        /// </summary>
        private static int LevenshteinDistance(string s1, string s2)
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
        /// Determines the type of employee match
        /// </summary>
        private static EmployeeMatchType GetEmployeeMatchType(string qbName, string avionteName, decimal qbAmount, decimal avionteAmount)
        {
            if (string.Equals(qbName, avionteName, StringComparison.OrdinalIgnoreCase))
                return EmployeeMatchType.Exact;

            var normalizedQB = NormalizeEmployeeName(qbName);
            var normalizedAvionte = NormalizeEmployeeName(avionteName);

            if (string.Equals(normalizedQB, normalizedAvionte, StringComparison.OrdinalIgnoreCase))
                return EmployeeMatchType.Normalized;

            var qbComponents = ExtractNameComponents(normalizedQB);
            var avionteComponents = ExtractNameComponents(normalizedAvionte);

            if (qbComponents.HasValidComponents && avionteComponents.HasValidComponents && 
                AreNameComponentsSimilar(qbComponents, avionteComponents))
                return EmployeeMatchType.NameComponents;

            return EmployeeMatchType.Fuzzy;
        }
    }

    /// <summary>
    /// Represents name components for employee matching
    /// </summary>
    public class NameComponents
    {
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public bool HasValidComponents => !string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName);
    }

    /// <summary>
    /// Represents a matched employee between QuickBooks and Avionte systems
    /// </summary>
    public class EmployeeMatch
    {
        public string UnifiedName { get; set; } = string.Empty;
        public string? QBName { get; set; }
        public string? AvionteName { get; set; }
        public decimal QBAmount { get; set; }
        public decimal AvionteAmount { get; set; }
        public EmployeeMatchType MatchType { get; set; }

        public bool HasQBData => !string.IsNullOrEmpty(QBName);
        public bool HasAvionteData => !string.IsNullOrEmpty(AvionteName);
        public bool IsPerfectMatch => HasQBData && HasAvionteData;
        public decimal Variance => QBAmount - AvionteAmount;
        public bool HasSignificantVariance => Math.Abs(Variance) > 0.01m;
        public bool AmountsZeroOut => Math.Abs(Math.Abs(QBAmount) - Math.Abs(AvionteAmount)) < 0.01m && 
                                     Math.Abs(QBAmount + AvionteAmount) < 0.01m;
    }

    /// <summary>
    /// Types of employee matches
    /// </summary>
    public enum EmployeeMatchType
    {
        Exact,          // Perfect string match
        Normalized,     // Matched after normalization (removing special chars, etc.)
        NameComponents, // Matched on first/last name components
        Fuzzy,          // Matched via similarity scoring
        QBOnly,         // Only exists in QuickBooks
        AvionteOnly     // Only exists in Avionte
    }
}
