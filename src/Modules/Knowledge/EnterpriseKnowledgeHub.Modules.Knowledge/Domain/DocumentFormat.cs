namespace EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

// Configuration can enable these formats; adding a format also requires a byte validator.
public sealed class DocumentFormat
{
    public static readonly DocumentFormat Pdf = new("pdf", ".pdf", "application/pdf");
    public static readonly DocumentFormat Markdown = new("markdown", ".md", "text/markdown");
    public static IReadOnlyList<DocumentFormat> All { get; } = Array.AsReadOnly(new[] { Pdf, Markdown });

    public string Id { get; }
    public string Extension { get; }
    public string ContentType { get; }

    private DocumentFormat(string id, string extension, string contentType)
    {
        Id = id;
        Extension = extension;
        ContentType = contentType;
    }

    public static DocumentFormat? FromId(string id) =>
        All.FirstOrDefault(format => string.Equals(format.Id, id, StringComparison.OrdinalIgnoreCase));

    public static DocumentFormat? FromFileName(string fileName) =>
        All.FirstOrDefault(format => string.Equals(format.Extension, Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase));
}
