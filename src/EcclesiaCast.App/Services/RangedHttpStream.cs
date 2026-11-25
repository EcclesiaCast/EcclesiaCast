using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace EcclesiaCast.App.Services;

/// <summary>
/// A sound on the internet read as a seekable stream, always with a
/// <c>Range</c> request. YouTube answers a request with no range at a trickle
/// (about 30 KB/s) or with a 403, but a ranged one at full speed (a whole song
/// in a tenth of a second), and a media player does not always ask for one;
/// this class does, so LibVLC can start playing with the first bytes and seek.
/// </summary>
public sealed class RangedHttpStream : Stream
{
    private static readonly HttpClient Client = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly string _url;
    private Stream? _body;
    private long _position;
    private long _length = -1;

    public RangedHttpStream(string url)
    {
        _url = url;
        // The first request tells how long the sound is.
        Open(0);
    }

    public override bool CanRead => true;

    public override bool CanSeek => _length >= 0;

    public override bool CanWrite => false;

    public override long Length => _length >= 0 ? _length : throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    private void Open(long from)
    {
        _body?.Dispose();
        _body = null;

        using var request = new HttpRequestMessage(HttpMethod.Get, _url);
        request.Headers.Range = new RangeHeaderValue(from, null);
        var response = Client.Send(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            throw new IOException($"YouTube respondió {status} al pedir el audio.");
        }

        if (response.Content.Headers.ContentRange?.Length is { } total)
            _length = total;
        else if (from == 0 && response.Content.Headers.ContentLength is { } size)
            _length = size;

        _body = response.Content.ReadAsStream();
        _position = from;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_length >= 0 && _position >= _length)
            return 0;

        if (_body is null)
            throw new ObjectDisposedException(nameof(RangedHttpStream));
        var read = _body.Read(buffer, offset, count);
        _position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            _ => _length + offset,
        };
        target = Math.Clamp(target, 0, _length);
        if (target != _position)
            Open(target);
        return _position;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _body?.Dispose();
            _body = null;
        }

        base.Dispose(disposing);
    }

    public override void Flush()
    {
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
