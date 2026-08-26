namespace SchoolSystemAPI.Services;

public interface IResultsService
{
    decimal CalculatePercentage(decimal totalScore, string stage);
    bool IsPassing(decimal percentage);
}

public class ResultsService : IResultsService
{
    public decimal CalculatePercentage(decimal totalScore, string stage)
    {
        // اللوجيك الخاص بتحديد النهاية العظمى (400 للابتدائي، 500 للباقي)
        decimal maxScore = stage.Contains("ابتدائي") ? 400m : 500m;
        
        if (maxScore == 0) return 0;
        return (totalScore / maxScore) * 100m;
    }

    public bool IsPassing(decimal percentage)
    {
        return percentage >= 50m; // نسبة النجاح 50% فأكثر
    }
}
