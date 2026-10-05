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
        // ابتدائي، إعدادي، وكبار أصبح لديهم 4 مواد في التيرم
        bool isFourSubjects = stage.Contains("ابتدائي") || stage.Contains("إعدادي") || stage.Contains("اعدادي") || stage.Contains("كبار");
        decimal maxScore = isFourSubjects ? 400m : 500m;
        
        if (maxScore == 0) return 0;
        return (totalScore / maxScore) * 100m;
    }

    public bool IsPassing(decimal percentage)
    {
        return percentage >= 50m; // نسبة النجاح 50% فأكثر
    }
}
