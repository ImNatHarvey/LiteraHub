using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Application-session cover cache shared by Author and Category galleries.
/// Stores compressed source bytes, NOT GDI+ Image instances (each PictureBox owns its bitmap).
/// Memory is bounded. Remote downloads are limited to four concurrent requests.
/// </summary>
public static class CatalogCoverCache
{
    private const long MaxCacheBytes = 32L * 1024 * 1024;
    private const int MaxFileBytes = 8 * 1024 * 1024;

    private sealed record CacheEntry(string Key, byte[] Bytes);

    private static readonly object Sync = new();
    private static readonly Dictionary<string, LinkedListNode<CacheEntry>> Entries = new(StringComparer.Ordinal);
    private static readonly LinkedList<CacheEntry> Recent = new();
    private static readonly Dictionary<string, Task<byte[]?>> Pending = new(StringComparer.Ordinal);
    private static readonly SemaphoreSlim Downloads = new(4, 4);
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8), MaxResponseContentBufferSize = MaxFileBytes };
    private static long _bytes;

    /// <summary>Loads a cover onto an existing PictureBox, leaving its themed background visible on failure.</summary>
    public static async Task LoadIntoAsync(PictureBox target, string? path)
    {
        if (target.IsDisposed || string.IsNullOrWhiteSpace(path)) return;

        try
        {
            string key = MakeKey(path);
            byte[]? bytes = await GetBytesAsync(key, path);
            if (bytes is null || bytes.Length == 0 || target.IsDisposed) return;

            // Each control gets its own bitmap, so closing a gallery cannot dispose another card's image.
            using var stream = new MemoryStream(bytes, writable: false);
            using var image = Image.FromStream(stream);
            var bitmap = new Bitmap(image);
            if (target.IsDisposed) { bitmap.Dispose(); return; }

            var old = target.Image;
            target.Image = bitmap;
            old?.Dispose();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or
                                    TaskCanceledException or ArgumentException or NotSupportedException or OutOfMemoryException or
                                    ExternalException)
        {
            // Optional cover: retain the themed background, without disrupting the gallery.
        }
    }

    private static string MakeKey(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            return uri.AbsoluteUri;

        string fullPath = Path.GetFullPath(path);
        var info = new FileInfo(fullPath);
        return $"{fullPath}|{(info.Exists ? info.Length : -1)}|{(info.Exists ? info.LastWriteTimeUtc.Ticks : 0)}";
    }

    private static Task<byte[]?> GetBytesAsync(string key, string path)
    {
        lock (Sync)
        {
            if (Entries.TryGetValue(key, out var node))
            {
                Recent.Remove(node);
                Recent.AddFirst(node);
                return Task.FromResult<byte[]?>(node.Value.Bytes);
            }

            if (Pending.TryGetValue(key, out var existing)) return existing;
            // Always start outside the calling stack so Pending is registered before completion.
            var task = Task.Run(() => FetchAndStoreAsync(key, path));
            Pending[key] = task;
            return task;
        }
    }

    private static async Task<byte[]?> FetchAndStoreAsync(string key, string path)
    {
        byte[]? result = null;
        try
        {
            await Downloads.WaitAsync().ConfigureAwait(false);
            try
            {
                if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    result = await Http.GetByteArrayAsync(uri).ConfigureAwait(false);
                else if (File.Exists(path) && new FileInfo(path).Length <= MaxFileBytes)
                    result = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            }
            finally { Downloads.Release(); }
            if (result?.Length > MaxFileBytes) result = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or HttpRequestException or
                                    TaskCanceledException or ArgumentException or NotSupportedException)
        {
            // Missing or unreachable covers are not cached permanently; a later visit may retry.
        }
        finally
        {
            lock (Sync)
            {
                if (result is { Length: > 0 })
                {
                    var node = Recent.AddFirst(new CacheEntry(key, result));
                    Entries[key] = node;
                    _bytes += result.Length;
                    while (_bytes > MaxCacheBytes && Recent.Last is { } tail)
                    {
                        Recent.RemoveLast();
                        Entries.Remove(tail.Value.Key);
                        _bytes -= tail.Value.Bytes.Length;
                    }
                }
                Pending.Remove(key);
            }
        }
        return result;
    }
}
