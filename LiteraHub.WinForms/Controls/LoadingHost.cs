using System.ComponentModel;
using System.Diagnostics;

namespace LiteraHub.WinForms.Controls;

// Wraps a content control. RunAsync shows a skeleton only when the work takes longer than DelayMs.
public sealed class LoadingHost : ThemedUserControl
{
    private const int DelayMs = 150;      // avoid flicker for fast loads
    private const int MinVisibleMs = 300; // once shown, do not flash away

    private readonly SkeletonView _skeleton = new() { Dock = DockStyle.Fill, Visible = false };
    private Control? _content;
    private int _runId;

    public LoadingHost()
    {
        Controls.Add(_skeleton);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SkeletonStyle SkeletonStyle
    {
        get => _skeleton.Style;
        set => _skeleton.Style = value;
    }

    // True if the most recent RunAsync had to show the skeleton (useful for testing).
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LastRunShowedSkeleton { get; private set; }

    public void SetContent(Control content)
    {
        if (_content is not null) Controls.Remove(_content);

        _content = content;
        content.Dock = DockStyle.Fill;
        Controls.Add(content);
        _skeleton.BringToFront();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        var id = ++_runId;
        LastRunShowedSkeleton = false;

        var task = work();
        var finishedFirst = await Task.WhenAny(task, Task.Delay(DelayMs));
        if (finishedFirst == task) return await task;

        if (IsDisposed) return await task;
        LastRunShowedSkeleton = true;
        _skeleton.Visible = true;
        _skeleton.BringToFront();
        var shown = Stopwatch.StartNew();

        try
        {
            return await task;
        }
        finally
        {
            var remaining = MinVisibleMs - (int)shown.ElapsedMilliseconds;
            if (remaining > 0) await Task.Delay(remaining);

            if (!IsDisposed && id == _runId) _skeleton.Visible = false;
        }
    }

    public async Task RunAsync(Func<Task> work)
    {
        await RunAsync<bool>(async () =>
        {
            await work();
            return true;
        });
    }
}