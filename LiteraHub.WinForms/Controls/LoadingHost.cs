using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteraHub.WinForms.Controls;

/// <summary>
/// Wraps existing page content with a delayed, theme-aware skeleton.
/// Quick requests never show a skeleton; slower requests keep it visible
/// briefly to avoid flicker. Only the latest request controls visibility.
/// </summary>
public sealed class LoadingHost : ThemedUserControl
{
    private const int DelayMs = 150;
    private const int MinVisibleMs = 300;

    private readonly SkeletonView _skeleton = new()
    {
        Dock = DockStyle.Fill,
        Visible = false,
        AccessibleName = "Loading content",
        AccessibleDescription = "Please wait while the content is loaded."
    };

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

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool LastRunShowedSkeleton { get; private set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsLoading { get; private set; }

    public void SetContent(Control content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (ReferenceEquals(_content, content)) return;
        if (_content is not null) Controls.Remove(_content);

        _content = content;
        content.Dock = DockStyle.Fill;
        Controls.Add(content);
        _skeleton.BringToFront();
    }

    public async Task<T> RunAsync<T>(Func<Task<T>> work)
    {
        ArgumentNullException.ThrowIfNull(work);

        int id = ++_runId;
        LastRunShowedSkeleton = false;
        IsLoading = true;
        _skeleton.Visible = false;

        try
        {
            Task<T> task = work();
            ArgumentNullException.ThrowIfNull(task);

            // No placeholder for work completing during the initial delay.
            Task first = await Task.WhenAny(task, Task.Delay(DelayMs));
            if (first == task || task.IsCompleted || IsDisposed)
                return await task;

            // Do not let an older overlapping request show the skeleton.
            if (id != _runId)
                return await task;

            LastRunShowedSkeleton = true;
            _skeleton.Visible = true;
            _skeleton.BringToFront();
            var visibleTime = Stopwatch.StartNew();

            try
            {
                return await task;
            }
            finally
            {
                // Only delay when this request actually displayed a skeleton.
                int remaining = MinVisibleMs - (int)visibleTime.ElapsedMilliseconds;
                if (remaining > 0 && !IsDisposed && id == _runId)
                    await Task.Delay(remaining);
            }
        }
        finally
        {
            // A previous request must never hide a newer request's placeholder.
            if (id == _runId)
            {
                IsLoading = false;
                if (!IsDisposed && !Disposing)
                    _skeleton.Visible = false;
            }
        }
    }

    public async Task RunAsync(Func<Task> work)
    {
        ArgumentNullException.ThrowIfNull(work);
        await RunAsync<bool>(async () =>
        {
            await work();
            return true;
        });
    }
}
