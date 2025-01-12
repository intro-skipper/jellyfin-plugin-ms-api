using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.MediaSegments;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaSegmentsApi.Controllers;

/// <summary>
/// Extended API for MediaSegments Management.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MediaSegmentsApiController"/> class.
/// </remarks>
/// <param name="mediaSegmentManager">MediaSegmentManager.</param>
/// <param name="libraryManager">The Library manager.</param>
[Authorize(Policy = "RequiresElevation")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Route("MediaSegmentsApi")]
public class MediaSegmentsApiController(IMediaSegmentManager mediaSegmentManager, ILibraryManager libraryManager) : ControllerBase
{
    private readonly IMediaSegmentManager _mediaSegmentManager = mediaSegmentManager;

    private readonly ILibraryManager _libraryManager = libraryManager;

    private readonly string _pluginId = Plugin.Instance!.Name.ToLowerInvariant()
                                        .GetMD5()
                                        .ToString("N", CultureInfo.InvariantCulture);

    /// <summary>
    /// Plugin meta endpoint.
    /// </summary>
    /// <returns>The created segment.</returns>
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
        if (item is null || segment is null)
        {
            return NotFound();
        }

        segment.ItemId = item.Id;

        var seg = await _mediaSegmentManager.CreateSegmentAsync(segment, _pluginId).ConfigureAwait(false);
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
        await _mediaSegmentManager.DeleteSegmentAsync(segmentId).ConfigureAwait(false);
        Ok();
    }
}
