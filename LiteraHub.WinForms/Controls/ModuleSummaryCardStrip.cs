using FontAwesome.Sharp;

namespace LiteraHub.WinForms.Controls;

public sealed class ModuleSummaryCardStrip : ThemedUserControl
{
    private readonly List<ModuleSummaryCard> _cards = new();
    public const int PreferredRowHeight = 100;

    public ModuleSummaryCardStrip()
    {
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 12, 0, 12);
    }

    public void Add(string title, IconChar icon, Action? action = null)
    {
        var card = new ModuleSummaryCard(title, icon, action);
        _cards.Add(card);
        Controls.Add(card);
        ArrangeCards();
    }

    public void SetValues(params long[] values)
    {
        if (values.Length != _cards.Count) throw new ArgumentException("Summary card count mismatch.", nameof(values));
        for (int i = 0; i < values.Length; i++) _cards[i].SetValue(values[i], false);
    }

    public void SetLoading() { foreach (var card in _cards) card.SetValue(null, false); }
    public void SetUnavailable() { foreach (var card in _cards) card.SetValue(null, true); }
    public void SetSelected(params int[] indices)
    {
        for (int i = 0; i < _cards.Count; i++) _cards[i].SetSelected(indices.Contains(i));
    }

    protected override void OnResize(EventArgs e) { base.OnResize(e); ArrangeCards(); }

    private void ArrangeCards()
    {
        if (_cards is null || _cards.Count == 0) return;
        int gap = Width >= 1200 ? 16 : 12;
        int available = Math.Max(0, ClientSize.Width - gap * (_cards.Count - 1));
        int x = 0;
        for (int i = 0; i < _cards.Count; i++)
        {
            int w = available * (i + 1) / _cards.Count - available * i / _cards.Count;
            _cards[i].SetBounds(x, 0, w, Math.Max(0, ClientSize.Height));
            x += w + gap;
        }
    }
}
