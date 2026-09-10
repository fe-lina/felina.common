using System.Net;

namespace Felina.Client;

internal sealed class PassThroughHttpHandler : DelegatingHandler
{
    public PassThroughHttpHandler()
        : base(new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        })
    {
    }
}
