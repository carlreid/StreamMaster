using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

using System.Text;

namespace StreamMasterInfrastructure.Services.Frontend.Mappers
{
    public abstract class StaticResourceMapperBase : IMapHttpRequestsToDisk
    {
        private readonly StringComparison _caseSensitive;
        private readonly ILogger _logger;
        private readonly IContentTypeProvider _mimeTypeProvider;

        protected StaticResourceMapperBase(ILogger logger)
        {
            _logger = logger;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
        }

        public abstract bool CanHandle(string resourceUrl);

        public IActionResult GetResponse(string resourceUrl)
        {
            var filePath = Map(resourceUrl);

            if (File.Exists(filePath))
            {
                if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                return new FileStreamResult(GetContentStream(filePath), new MediaTypeHeaderValue(contentType)
                {
                    Encoding = contentType == "text/plain" ? Encoding.UTF8 : null
                });
            }

            _logger.LogWarning("File {0} not found", filePath);

            return null;
        }

        public abstract string Map(string resourceUrl);

        protected virtual Stream GetContentStream(string filePath)
        {
            return File.OpenRead(filePath);
        }
    }
}
