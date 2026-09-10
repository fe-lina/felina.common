using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Felina.Client;

public sealed class MappedProxyEndpoints
{
    internal MappedProxyEndpoints(RouteGroupBuilder group)
    {
        Group = group;
    }

    public RouteGroupBuilder Group { get; }
    public RouteHandlerBuilder? Preflight { get; internal set; }
    public RouteHandlerBuilder? Upload { get; internal set; }
    public RouteHandlerBuilder? UploadBegin { get; internal set; }
    public RouteHandlerBuilder? UploadTicket { get; internal set; }
    public RouteHandlerBuilder? UploadCommit { get; internal set; }
    public RouteHandlerBuilder? UploadTicketUrl { get; internal set; }
    public RouteHandlerBuilder? UploadTicketChunkInit { get; internal set; }
    public RouteHandlerBuilder? UploadTicketTusCreate { get; internal set; }
    public RouteHandlerBuilder? Download { get; internal set; }
    public RouteHandlerBuilder? View { get; internal set; }
    public RouteHandlerBuilder? AccessUrl { get; internal set; }
    public RouteHandlerBuilder? Details { get; internal set; }
    public RouteHandlerBuilder? Parent { get; internal set; }
    public RouteHandlerBuilder? Revisions { get; internal set; }
    public RouteHandlerBuilder? Revision { get; internal set; }
    public RouteHandlerBuilder? RevisionView { get; internal set; }
    public RouteHandlerBuilder? VersionMetadataGet { get; internal set; }
    public RouteHandlerBuilder? VersionMetadataSet { get; internal set; }
    public RouteHandlerBuilder? DocumentMetadataGet { get; internal set; }
    public RouteHandlerBuilder? DocumentMetadataSet { get; internal set; }
    public RouteHandlerBuilder? Browse { get; internal set; }
    public RouteHandlerBuilder? Search { get; internal set; }
    public RouteHandlerBuilder? CreateFolder { get; internal set; }
    public RouteHandlerBuilder? DeleteFile { get; internal set; }
    public RouteHandlerBuilder? RestoreFile { get; internal set; }
    public RouteHandlerBuilder? DeleteFolder { get; internal set; }
    public RouteHandlerBuilder? RestoreFolder { get; internal set; }
    public RouteHandlerBuilder? ChunkInit { get; internal set; }
    public RouteHandlerBuilder? ChunkPart { get; internal set; }
    public RouteHandlerBuilder? ChunkComplete { get; internal set; }
    public RouteHandlerBuilder? ChunkStatus { get; internal set; }
    public RouteHandlerBuilder? ChunkAbort { get; internal set; }
    public RouteHandlerBuilder? TusOptions { get; internal set; }
    public RouteHandlerBuilder? TusCreate { get; internal set; }
    public RouteHandlerBuilder? TusHead { get; internal set; }
    public RouteHandlerBuilder? TusPatch { get; internal set; }
    public RouteHandlerBuilder? TusDelete { get; internal set; }
}
