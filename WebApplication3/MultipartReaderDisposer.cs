using System.Linq.Expressions;
using Microsoft.AspNetCore.WebUtilities;

namespace WebApplication3
{
    public sealed class MultipartReaderDisposer : IDisposable
    {
        private static Func<Stream, bool>? _getFinalBoundaryFound;

        private readonly FileMultipartSection _section;
        private readonly string _url;

        public MultipartReaderDisposer(FileMultipartSection section, string url)
        {
            ArgumentNullException.ThrowIfNull(section);
            ArgumentNullException.ThrowIfNull(url);

            _section = section;
            _url = url;
        }

        public void Dispose()
        {
            Stream stream = _section.Section.Body;

            // Length in diesem Stream ist die größte jemals erreichte Position. Nur 0 wenn niemals gelesen
            // https://github.com/dotnet/aspnetcore/blob/3639b9b53af970c897fe1ea01d7b959662391a69/src/Http/WebUtilities/src/MultipartReaderStream.cs#L68
            if (stream.Length > 0 && !GetFinalBoundaryFound(stream))
            {
                throw new InvalidOperationException($"Multipart content not read completely because of tail stream '{_section.Name}' {_url}.");
            }
        }

        private static bool GetFinalBoundaryFound(Stream stream)
        {
            Func<Stream, bool>? getFinalBoundaryFound = _getFinalBoundaryFound;
            if (getFinalBoundaryFound is null)
            {
                Type streamType = stream.GetType();
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

            return getFinalBoundaryFound(stream);
        }
    }
}
