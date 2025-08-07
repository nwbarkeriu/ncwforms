using JobCompareApp.Services;

// Test program to demonstrate FuzzyWuzzy-style matching
namespace JobCompareApp.Tests
{
    public static class FuzzyMatchTest
    {
        public static void RunEmployeeMatchingTests()
        {
            Console.WriteLine("=== FuzzyWuzzy-Style Employee Matching Test ===\n");

            // Test the problematic names you mentioned
            var qbNames = new[]
            {
                "Antonio Mendez",
                "Hector Vizcaya", 
                "Juan Rodriguez",
                "Ricardo Mendez Diaz"
            };

            var avionteNames = new[]
            {
                "Antonio Mendez Morillo",
                "Hector Vizcaya Daza",
                "Juan Rodriguez Salas", 
                "Ricardo Mendez Morillo"
            };

            foreach (var qbName in qbNames)
            {
                Console.WriteLine($"QB Name: '{qbName}'");
                
                // Test the current FindBestEmployeeMatch method
                var bestMatch = EmployeeMatcher.FindBestEmployeeMatch(qbName, avionteNames);
                Console.WriteLine($"  Best Match: '{bestMatch ?? "None"}'");

                // Test the new FuzzyWuzzy-style ExtractBestMatch method
                var (fuzzyMatch, score, reason) = EmployeeMatcher.ExtractBestMatch(qbName, avionteNames, 0.7);
                Console.WriteLine($"  Fuzzy Match: '{fuzzyMatch ?? "None"}' (Score: {score:P1}, Reason: {reason})");

                // Show all matches above threshold
                Console.WriteLine("  All potential matches:");
                foreach (var avionteName in avionteNames)
                {
                    var similarity = CalculateTestSimilarity(qbName, avionteName);
                    Console.WriteLine($"    '{avionteName}': {similarity:P1}");
                }
                Console.WriteLine();
            }
        }

        private static double CalculateTestSimilarity(string name1, string name2)
        {
            // Simple test - using reflection to access private method would be complex
            // This is just for demonstration
            var normalized1 = name1.ToLower().Replace("-", " ");
            var normalized2 = name2.ToLower().Replace("-", " ");
            
            var parts1 = normalized1.Split(' ');
            var parts2 = normalized2.Split(' ');
            
            if (parts1.Length >= 2 && parts2.Length >= 2)
            {
                if (parts1[0] == parts2[0] && parts1[1] == parts2[1])
                    return 0.95; // First two words match
            }
            
            return 0.3; // Simplified for demo
        }
    }
}
