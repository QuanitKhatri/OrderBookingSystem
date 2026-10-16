using System.Text.Json;

namespace Distribution.Connect;

/// <summary>
/// Remembers which broadcasts this computer has already seen. Kept in the user's own AppData rather
/// than the database, so it stays per-PC and needs no migration.
/// </summary>
public sealed class BroadcastStateStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    private readonly string _path;
    private readonly object _gate = new();
    private State _state;

    public BroadcastStateStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DistributionManagementSystem",
            "broadcast-state.json");
        _state = Load(_path);
    }

    private sealed class State
    {
        public HashSet<string> ReadIds { get; set; } = new(StringComparer.Ordinal);

        /// <summary>Messages already shown as a pop-up, so each one only interrupts once.</summary>
        public HashSet<string> AnnouncedIds { get; set; } = new(StringComparer.Ordinal);
    }

    private static State Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var loaded = JsonSerializer.Deserialize<State>(File.ReadAllText(path));
                if (loaded is not null) return loaded;
            }
        }
        catch (Exception)
        {
            // A damaged or unreadable state file must never stop the application starting;
            // the worst case is that a message is shown as unread again.
        }
        return new State();
    }

    public bool IsRead(string id)
    {
        lock (_gate) return _state.ReadIds.Contains(id);
    }

    public void MarkRead(string id)
    {
        lock (_gate)
        {
            if (!_state.ReadIds.Add(id)) return;
            Save();
        }
    }

    public void MarkAllRead(IEnumerable<string> ids)
    {
        lock (_gate)
        {
            bool changed = false;
            foreach (var id in ids) changed |= _state.ReadIds.Add(id);
            if (changed) Save();
        }
    }

    /// <summary>
    /// Returns the messages that have not been shown as a pop-up yet and marks them as shown, so
    /// the caller can announce them exactly once.
    /// </summary>
    public List<BroadcastMessage> TakeUnannounced(IEnumerable<BroadcastMessage> messages)
    {
        lock (_gate)
        {
            var fresh = messages
                .Where(m => !_state.AnnouncedIds.Contains(m.Id) && !_state.ReadIds.Contains(m.Id))
                .ToList();

            if (fresh.Count == 0) return fresh;

            foreach (var m in fresh) _state.AnnouncedIds.Add(m.Id);
            Save();
            return fresh;
        }
    }

    /// <summary>
    /// Marks everything currently published as already announced, without marking it read. Used on
    /// the very first run so that a new installation does not pop up a pile of old messages.
    /// </summary>
    public void SuppressAnnouncementsFor(IEnumerable<BroadcastMessage> messages)
    {
        lock (_gate)
        {
            bool changed = false;
            foreach (var m in messages) changed |= _state.AnnouncedIds.Add(m.Id);
            if (changed) Save();
        }
    }

    public int CountUnread(IEnumerable<BroadcastMessage> messages)
    {
        lock (_gate) return messages.Count(m => !_state.ReadIds.Contains(m.Id));
    }

    /// <summary>Drops remembered ids that are no longer in the feed, so the file cannot grow forever.</summary>
    public void Prune(IEnumerable<BroadcastMessage> currentMessages)
    {
        var live = currentMessages.Select(m => m.Id).ToHashSet(StringComparer.Ordinal);
        lock (_gate)
        {
            int removed = _state.ReadIds.RemoveWhere(id => !live.Contains(id));
            removed += _state.AnnouncedIds.RemoveWhere(id => !live.Contains(id));
            if (removed > 0) Save();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_state, Json));
        }
        catch (Exception)
        {
            // Read state is a convenience, not data worth failing over.
        }
    }
}
