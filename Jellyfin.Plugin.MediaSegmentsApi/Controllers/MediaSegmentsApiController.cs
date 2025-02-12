using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.MediaSegments;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaSegmentsApi.Controllers;

/// <summary>
/// Extended API for MediaSegments Management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaSegmentsApiController"/> class.
/// </remarks>
/// <param name="mediaSegmentManager">MediaSegmentManager.</param>
/// <param name="libraryManager">The Library manager.</param>
/// <param name="segmentProviders">The Segment providers.</param>
/// <param name="logger">The logger.</param>
[Authorize(Policy = "RequiresElevation")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Route("MediaSegmentsApi")]
public class MediaSegmentsApiController(IMediaSegmentManager mediaSegmentManager, ILibraryManager libraryManager, IEnumerable<IMediaSegmentProvider> segmentProviders, ILogger<MediaSegmentsApiController> logger) : ControllerBase
{
    private readonly IMediaSegmentManager _mediaSegmentManager = mediaSegmentManager;

    private readonly ILibraryManager _libraryManager = libraryManager;

    private readonly IEnumerable<IMediaSegmentProvider> _segmentProviders = [.. segmentProviders.OrderBy(i => i is IHasOrder hasOrder ? hasOrder.Order : 0)];

    private readonly ILogger<MediaSegmentsApiController> _logger = logger;

    /// <summary>
    /// Plugin metadata endpoint.
    /// </summary>
    /// <returns>The plugin metadata.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public JsonResult GetPluginMetadata()
    {
        var json = new
        {
            version = Plugin.Instance!.Version.ToString(3),
        };

        return new JsonResult(json);
    }

    /// <summary>
    /// Create MediaSegment for itemId.
    /// </summary>
    /// <param name="itemId">The ItemId.</param>
    /// <param name="providerId">Provider of the Segment.</param>
    /// <param name="segment">MediaSegment data.</param>
    /// <returns>The created segment.</returns>
    [HttpPost("{itemId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueryResult<MediaSegmentDto>>> CreateSegmentAsync(
        [FromRoute, Required] Guid itemId,
        [FromQuery, Required] string providerId,
        [FromBody, Required] MediaSegmentDto segment)
    {
        var item = _libraryManager.GetItemById<BaseItem>(itemId);
        if (item is null || segment is null || providerId is null)
        {
            return NotFound(); // Item, segment, or providerId missing
        }

        var libraryOptions = _libraryManager.GetLibraryOptions(item);

        // Calculate provider UID from query parameter
        var providerUID = GetProviderId(providerId);

        // Get the list of active provider UIDs, i.e. those not disabled in the library options.
        var activeProviderIds = _segmentProviders
            .Where(e => !libraryOptions.DisabledMediaSegmentProviders.Contains(GetProviderId(e.Name)))
            .Select(e => GetProviderId(e.Name))
            .ToList();

        // Check for the specific provider
        if (!activeProviderIds.Contains(providerUID))
        {
            // Return a 404 response with a custom message for provider not found.
            _logger.LogError("Provider with id '{ProviderId}' not found.", providerId);
            return NotFound(new { message = $"Provider with id '{providerId}' not found." });
        }

        // Assign the item id to the segment
        var mediaSegment = new MediaSegmentDto
        {
            ItemId = item.Id,
            StartTicks = segment.StartTicks,
            EndTicks = segment.EndTicks,
            Type = segment.Type
        };

        _logger.LogInformation("Creating segment for item {ItemId} with provider {ProviderId}", item.Id, providerId);

        // Create the segment using the provider UID
        var seg = await _mediaSegmentManager.CreateSegmentAsync(mediaSegment, providerUID).ConfigureAwait(false);
        return Ok(seg);
    }

    /// <summary>
    /// Delete MediaSgment by segment id.
    /// </summary>
    /// <param name="segmentId">The Id of the segment.</param>
    /// <returns>Always 200.</returns>
    [HttpDelete("{segmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task DeleteSegmentAsync(
        [FromRoute, Required] Guid segmentId)
    {
        _logger.LogInformation("Deleting segment with id {SegmentId}", segmentId);
        await _mediaSegmentManager.DeleteSegmentAsync(segmentId).ConfigureAwait(false);
        Ok();
    }

    private static string GetProviderId(string name)
        => name.ToLowerInvariant()
            .GetMD5()
            .ToString("N", CultureInfo.InvariantCulture);
}
