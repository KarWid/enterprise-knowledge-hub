using EnterpriseKnowledgeHub.Modules.Knowledge.Domain;

namespace EnterpriseKnowledgeHub.Infrastructure.DocumentUploads.Validation;

internal sealed class PdfDocumentContentValidator : IDocumentContentValidator
{
    private static readonly byte[] Signature = "%PDF-"u8.ToArray();

    public DocumentFormat Format => DocumentFormat.Pdf;

    public long? MaximumInspectionBytes => Signature.Length;

    public async Task<bool> ValidateAsync(Stream content, long contentLength, CancellationToken cancellationToken)
    {
        if (contentLength < Signature.Length)
        {
            return false;  
        } 

        var header = new byte[Signature.Length];

        await content.ReadExactlyAsync(header, cancellationToken);

        return header.AsSpan().SequenceEqual(Signature);
    }
}
