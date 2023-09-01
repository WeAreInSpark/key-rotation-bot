using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Azure.Core;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.FileProviders;

namespace Kerbee.Internal;

internal static class StaticAppExtensions
{
    private const string DefaultContentType = "application/octet-stream";

    private static readonly PhysicalFileProvider s_fileProvider = new(Path.Combine(FunctionEnvironment.RootPath, "wwwroot"));
    private static readonly FileExtensionContentTypeProvider s_contentTypeProvider = new();

    public async static Task<HttpResponseData> CreateStaticAppResponse(this HttpRequestData req, string defaultFile = "index.html", string fallbackPath = "404.html", string? fallbackExclude = null)
    {
        var (_, value) = req.FunctionContext.BindingContext.BindingData.FirstOrDefault(x => x.Key == "path");

        var virtualPath = $"/{value}";

        var contents = s_fileProvider.GetDirectoryContents(virtualPath);

        if (contents.Exists)
        {
            virtualPath += virtualPath.EndsWith("/") ? defaultFile : $"/{defaultFile}";
        }

        var response = req.CreateResponse();

        var fileInfo = GetFileInformation(virtualPath, fallbackPath, fallbackExclude);

        if (!fileInfo.Exists || fileInfo.PhysicalPath is null)
        {
            response.StatusCode = HttpStatusCode.NotFound;

            return response;
        }

        SetResponseHeaders(response, fileInfo);

        if (!HttpMethods.IsHead(req.Method))
        {
            var bytes = await File.ReadAllBytesAsync(fileInfo.PhysicalPath);
            await response.WriteBytesAsync(bytes);
        }

        return response;
    }

    private static void SetResponseHeaders(HttpResponseData response, IFileInfo fileInfo)
    {
        response.Headers.Add(HttpHeader.Names.ContentType, s_contentTypeProvider.TryGetContentType(fileInfo.Name, out var contentType) ? contentType : DefaultContentType);
        response.Headers.Add(HttpHeader.Names.ContentLength, fileInfo.Length.ToString());
        response.Headers.Add("Last-Modified", fileInfo.LastModified.ToString("r"));
    }

    private static IFileInfo GetFileInformation(string virtualPath, string fallbackPath, string? fallbackExclude)
    {
        var fileInfo = s_fileProvider.GetFileInfo(virtualPath);

        if (!fileInfo.Exists)
        {
            // Try Fallback
            if (!string.IsNullOrEmpty(fallbackPath) && (string.IsNullOrEmpty(fallbackExclude) || !Regex.IsMatch(virtualPath, fallbackExclude)))
            {
                return s_fileProvider.GetFileInfo(fallbackPath);
            }
        }

        return fileInfo;
    }
}
