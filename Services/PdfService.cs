using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolSystemAPI.Controllers;

namespace SchoolSystemAPI.Services;

public interface IPdfService
{
    byte[] GenerateCertificatesPdf(string className, string stage, string year, List<StudentCertificateDto> students);
}

public class PdfService : IPdfService
{
    public PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateCertificatesPdf(string className, string stage, string year, List<StudentCertificateDto> students)
    {
        var document = Document.Create(container =>
        {
            foreach (var student in students)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").DirectionFromRightToLeft());

                    page.Content().Element(content => ComposeCertificate(content, student, className, stage, year));
                });
            }
        });

        return document.GeneratePdf();
    }

    private void ComposeCertificate(IContainer container, StudentCertificateDto student, string className, string stage, string year)
    {
        container
            .Padding(10)
            .Border(4)
            .BorderColor("#1e3a8a") // Navy
            .Padding(5)
            .Border(2)
            .BorderColor("#1e3a8a") // Double Border
            .Padding(30)
            .Column(column =>
            {
                column.Item().AlignCenter().Text("شهادة تقدير وتفوق").FontSize(40).FontColor("#1e3a8a").Bold();
                
                column.Item().PaddingTop(20).AlignCenter().Text(text =>
                {
                    text.Span("تتقدم إدارة النظام بخالص التهاني للطالب/ـة: ").FontSize(20);
                });
                
                column.Item().PaddingTop(10).AlignCenter().Text(student.Name).FontSize(35).FontColor("#800000").Bold(); // Maroon

                column.Item().PaddingTop(20).AlignCenter().Text(text =>
                {
                    text.Span($"لنجاحه بتفوق في المرحلة ").FontSize(18);
                    text.Span(stage).FontSize(20).Bold();
                    text.Span($" - فصل ").FontSize(18);
                    text.Span(className).FontSize(20).Bold();
                    if (!string.IsNullOrEmpty(year))
                    {
                        text.Span($" للعام ").FontSize(18);
                        text.Span(year).FontSize(20).Bold();
                    }
                });

                column.Item().PaddingTop(30).Element(tableContainer => ComposeGradesTable(tableContainer, student));

                column.Item().PaddingTop(30).AlignCenter().Text(text =>
                {
                    text.Span("المجموع النهائي: ").FontSize(24);
                    text.Span($"{student.FinalTotal} درجة").FontSize(28).FontColor("#1e3a8a").Bold();
                    text.Span($"  ({student.Percentage:F2}%)").FontSize(22).FontColor("#666666");
                });

                column.Item().PaddingTop(10).AlignCenter().Text(text =>
                {
                    text.Span("بتقدير عام: ").FontSize(24);
                    text.Span(student.Tier).FontSize(30).FontColor("#800000").Bold();
                });
            });
    }

    private void ComposeGradesTable(IContainer container, StudentCertificateDto student)
    {
        container.AlignCenter().MaxWidth(500).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().BorderBottom(2).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("المادة").FontSize(16).Bold();
                header.Cell().BorderBottom(2).BorderColor(Colors.Black).Padding(5).AlignCenter().Text("الدرجة").FontSize(16).Bold();
            });

            foreach (var subject in student.SubjectsGrades)
            {
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(subject.Key).FontSize(14);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignCenter().Text(subject.Value.ToString()).FontSize(14).Bold();
            }
        });
    }
}
