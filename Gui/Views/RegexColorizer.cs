using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;

namespace Gui.Views;

public class RegexColorizer : ColorizingTransformer
{
    public List<(int Start, int Length)> SegmentsToHighlight { get; } = new();

    protected override void Colorize(ITextRunConstructionContext context)
    {
        int lineStart = context.VisualLine.FirstDocumentLine.Offset;
        int lineEnd = context.VisualLine.LastDocumentLine.EndOffset;

        foreach (var segment in SegmentsToHighlight)
        {
            if (segment.Start < lineEnd && (segment.Start + segment.Length) > lineStart)
            {
                int start = Math.Max(segment.Start, lineStart);
                int end = Math.Min(segment.Start + segment.Length, lineEnd);

                ChangeVisualElements(start, end, element =>
                {
                    element.TextRunProperties.SetBackgroundBrush(Brushes.Yellow);
                    element.TextRunProperties.SetForegroundBrush(Brushes.Black);
                });
            }
        }
    }
}