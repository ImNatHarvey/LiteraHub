using System;
using System.Drawing;

namespace LiteraHub.WinForms.Controls;

/// <summary>Content-width-based layout for related section cards; independent of screen resolution.</summary>
internal sealed class ResponsiveSectionLayout
{
    public int Width { get; }
    public int Padding { get; }
    public int Gap { get; }
    public int Columns { get; }
    public int CardWidth { get; }

    public ResponsiveSectionLayout(int availableWidth, int minimumCardWidth = 270)
    {
        Width = Math.Max(1, availableWidth);
        Padding = Width >= 1200 ? 20 : 16;
        Gap = Width >= 1200 ? 18 : 12;
        Columns = Math.Clamp((Width + Gap) / (minimumCardWidth + Gap), 1, 3);
        CardWidth = Math.Max(1, (Width - (Columns - 1) * Gap) / Columns);
    }

    private int UsableWidth => Width - (Columns - 1) * Gap;

    public int X(int index)
    {
        int column = index % Columns;
        return column * Gap + (int)((long)column * UsableWidth / Columns);
    }

    public int WidthAt(int index, int cardCount)
    {
        // A lone card in a wrapped row spans that row, including its fields.
        if (index % Columns == 0 && cardCount - index == 1) return Width;
        int column = index % Columns;
        int start = (int)((long)column * UsableWidth / Columns);
        int end = (int)((long)(column + 1) * UsableWidth / Columns);
        return Math.Max(1, end - start);
    }

    public int Place(ThemedSectionCard[] cards, int[] heights, int viewportHeight)
    {
        int y = 0;
        for (int start = 0; start < cards.Length; start += Columns)
        {
            int count = Math.Min(Columns, cards.Length - start);
            int rowHeight = 0;
            for (int i = 0; i < count; i++) rowHeight = Math.Max(rowHeight, heights[start + i]);
            // A single row may fill the usable viewport; wrapped rows keep natural height.
            if (cards.Length <= Columns) rowHeight = Math.Max(rowHeight, viewportHeight - 2);
            for (int i = 0; i < count; i++)
                cards[start + i].SetBounds(X(i), y,
                    WidthAt(start + i, cards.Length), rowHeight);
            y += rowHeight + Gap;
        }
        return Math.Max(1, y - Gap + 2);
    }
}
