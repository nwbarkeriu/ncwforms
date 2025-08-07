using JobCompareApp.Services;

public class TestEmployeeMatch
{
    public static void Main(string[] args)
    {
        var qbName = "Alfonso Anguiano Saucedo";
        var avionteName = "Alfonzo Anguiano Saucedo";
        
        Console.WriteLine($"Testing employee match:");
        Console.WriteLine($"QB Name: {qbName}");
        Console.WriteLine($"Avionte Name: {avionteName}");
        Console.WriteLine();
        
        // Test the best match function
        var avionteNames = new[] { avionteName, "Other Employee", "Another Person" };
        var match = EmployeeMatcher.FindBestEmployeeMatch(qbName, avionteNames);
        
        Console.WriteLine($"Best Match Result: {match ?? "NO MATCH"}");
        
        // Test the extract best match with score
        var (bestMatch, score, reason) = EmployeeMatcher.ExtractBestMatch(qbName, avionteNames, 0.6);
        
        Console.WriteLine($"Extract Best Match:");
        Console.WriteLine($"  Match: {bestMatch ?? "NO MATCH"}");
        Console.WriteLine($"  Score: {score:P2}");
        Console.WriteLine($"  Reason: {reason}");
        
        // Test with higher threshold
        var (bestMatch80, score80, reason80) = EmployeeMatcher.ExtractBestMatch(qbName, avionteNames, 0.8);
        
        Console.WriteLine($"Extract Best Match (80% threshold):");
        Console.WriteLine($"  Match: {bestMatch80 ?? "NO MATCH"}");
        Console.WriteLine($"  Score: {score80:P2}");
        Console.WriteLine($"  Reason: {reason80}");
    }
}
