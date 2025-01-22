using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;

namespace Jellyfin.Plugin.MediaSegmentsApi.Providers;

/// <summary>
/// MediaSegmentsApi media segment provider.
/// </summary>
public class SegmentProvider : IMediaSegmentProvider
{
    /// <inheritdoc/>
    public string Name => Plugin.Instance!.Name;

    /// <inheritdoc/>
    public Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(MediaSegmentGenerationRequest request, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MediaSegmentDto>>([]);

    /// <inheritdoc/>
    public ValueTask<bool> Supports(BaseItem item) => ValueTask.FromResult(false);
}
