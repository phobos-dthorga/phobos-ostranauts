using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>The places story content is grounded in (Framework 0.114.0; owner request, 6 October 2026): the merged
/// <c>places</c> tables as one lookup with no game types. A place is a station by its registration id or prefix; a
/// sub-station names the regional place it lies <c>within</c>, one level deep. The game tells us where the player is by
/// station id (the docked station, and the nearest regional station as the current region), and this turns those ids
/// into place keys.</summary>
public sealed class StoryPlaces
{
    private readonly Dictionary<string, StoryPlace> places;
    private readonly List<(string Key, string Station)> byLength;
    public static StoryPlaces Empty { get; } = new(new Dictionary<string, StoryPlace>());

    public StoryPlaces(IReadOnlyDictionary<string, StoryPlace> places)
    {
        this.places = new Dictionary<string, StoryPlace>(places, StringComparer.Ordinal);
        // Longest station prefix first, so OKLG_RES wins over OKLG for an id that names the residential level.
        byLength = this.places.Select(p => (p.Key, p.Value.station)).OrderByDescending(p => p.station.Length).ThenBy(p => p.Key, StringComparer.Ordinal).ToList();
    }

    public int Count => places.Count;
    public IEnumerable<string> Keys => places.Keys;
    public bool Contains(string? key) => key != null && places.ContainsKey(key);
    public StoryPlace? Get(string? key) => key != null && places.TryGetValue(key, out var place) ? place : null;
    /// <summary>A regional place names no place it lies within.</summary>
    public bool IsRegional(string? key) => Get(key) is StoryPlace place && place.within == null;
    /// <summary>The regional place this one lies within, or itself; an unknown key reads as itself.</summary>
    public string Root(string key) => Get(key)?.within is string within && places.ContainsKey(within) ? within : key;

    /// <summary>A station's parts carry its id with a suffix (VORB_HAB, VORB|Aux).</summary>
    public static bool Part(string? regId, string station) => regId != null && station.Length > 0 &&
        (regId == station || regId.StartsWith(station + "_", StringComparison.Ordinal) || regId.StartsWith(station + "|", StringComparison.Ordinal));

    /// <summary>The place a ship or station registration id belongs to, by the longest station prefix, or null.</summary>
    public string? Find(string? regId)
    {
        if (string.IsNullOrEmpty(regId)) return null;
        foreach (var (key, station) in byLength) if (Part(regId, station)) return key;
        return null;
    }
    /// <summary>Whether an id lies at a place: the place itself, or a sub-place of a regional place.</summary>
    public bool Covers(string place, string? regId) => Find(regId) is string found && (found == place || Root(found) == place);

    /// <summary>The "Region News:" label: the place's own, else its regional place's.</summary>
    public string? Region(string? key) => Get(key) is StoryPlace place ? (place.region ?? Get(Root(key!))?.region) : null;
    public string? Name(string? key) => Get(key)?.name;
    public string? Body(string? key) => Get(key) is StoryPlace place ? (place.body ?? Get(Root(key!))?.body) : null;
    public string? Station(string? key) => Get(key)?.station;
}
