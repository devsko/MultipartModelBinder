namespace WebApplication3
{
    public interface IMultipartFeature
    {
        MultipartCollection? Multipart { get; }

        Task<MultipartCollection> ReadMultipartAsync(Func<string?> getTailStreamSectionName, CancellationToken cancellationToken);
    }
}
