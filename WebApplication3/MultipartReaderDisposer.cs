using System.Linq.Expressions;
using Microsoft.AspNetCore.WebUtilities;

namespace WebApplication3
{
    public sealed class MultipartReaderDisposer : IAsyncDisposable
    {
        private static Func<Stream, bool>? _getFinalBoundaryFound;

        private readonly MultipartReader _reader;
        private readonly FileMultipartSection _section;
        private readonly string _url;

        public MultipartReaderDisposer(MultipartReader reader, FileMultipartSection section, string url)
        {
            ArgumentNullException.ThrowIfNull(reader);
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(url);

            _reader = reader;
            _section = section;
            _url = url;
        }

        public async ValueTask DisposeAsync()
        {
            await _section.Section.Body.DrainAsync(default);
            if (!GetFinalBoundaryFound())
            {
                throw new InvalidOperationException($"Multipart content not read completely because of tail stream '{_section.Name}' {_url}.");
            }
        }

        private bool GetFinalBoundaryFound()
        {
            Func<Stream, bool>? getFinalBoundaryFound = _getFinalBoundaryFound;
            if (getFinalBoundaryFound is null)
            {
                Type streamType = _section.Section.Body.GetType();
                ParameterExpression streamParameter = Expression.Parameter(typeof(Stream));
                getFinalBoundaryFound = Expression
                    .Lambda<Func<Stream, bool>>(
                        Expression.Property(
                            Expression.Convert(streamParameter, streamType), 
                            "FinalBoundaryFound"),
                        streamParameter)
                    .Compile();

                _getFinalBoundaryFound = getFinalBoundaryFound;
            }

            return getFinalBoundaryFound(_section.Section.Body);
        }
    }
}
