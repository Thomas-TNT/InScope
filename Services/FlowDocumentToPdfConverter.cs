using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace InScope.Services;

/// <summary>
/// Converts WPF FlowDocument content to QuestPDF format.
/// Handles Paragraph, List, Section, BlockUIContainer (images).
/// </summary>
internal static class FlowDocumentToPdfConverter
{
    private const float BulletIndent = 20f;
    private const float ListSpacing = 6f;

    public static void AddToDocument(FlowDocument document, IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(11));

            page.Content().Column(column =>
            {
                column.Spacing(4);
                foreach (Block block in document.Blocks)
                {
                    AddBlock(block, column, 0);
                }
            });
        });
    }

    private static void AddBlock(Block block, dynamic column, int indentLevel)
    {
        switch (block)
        {
            case Paragraph para:
                AddParagraph(para, column, indentLevel);
                break;
            case List list:
                AddList(list, column, indentLevel);
                break;
            case Section section:
                foreach (Block child in section.Blocks)
                    AddBlock(child, column, indentLevel);
                break;
            case BlockUIContainer uiContainer:
                AddBlockUIContainer(uiContainer, column);
                break;
            case Table table:
                AddTable(table, column);
                break;
        }
    }

    private static void AddParagraph(Paragraph para, dynamic column, int indentLevel)
    {
        var text = GetTextFromBlock(para);
        if (string.IsNullOrWhiteSpace(text))
        {
            column.Item().Height(8);
            return;
        }

        var item = column.Item();
        if (indentLevel > 0)
            item.PaddingLeft(indentLevel * BulletIndent);

        item.Text(text);
    }

    private static void AddList(List list, dynamic column, int indentLevel)
    {
        foreach (ListItem listItem in list.ListItems)
        {
            foreach (Block block in listItem.Blocks)
            {
                if (block is Paragraph p)
                {
                    var text = GetTextFromBlock(p);
                    column.Item().Row(row =>
                    {
                        if (indentLevel > 0)
                            row.ConstantItem(indentLevel * BulletIndent);
                        row.ConstantItem(15).Text("\u2022");
                        row.ConstantItem(5);
                        row.RelativeItem().Text(text);
                    });
                    column.Item().Height(ListSpacing);
                }
                else
                {
                    AddBlock(block, column, indentLevel + 1);
                }
            }
        }
    }

    private static void AddBlockUIContainer(BlockUIContainer container, dynamic column)
    {
        if (container.Child is System.Windows.Controls.Image wpfImage &&
            wpfImage.Source is BitmapSource source)
        {
            var bytes = BitmapSourceToPngBytes(source);
            if (bytes != null && bytes.Length > 0)
            {
                column.Item().Image(bytes);
            }
        }
    }

    private static void AddTable(Table table, dynamic column)
    {
        var text = GetTextFromBlock(table);
        if (!string.IsNullOrWhiteSpace(text))
            column.Item().Text(text);
    }

    private static string GetTextFromBlock(Block block)
    {
        var range = new TextRange(block.ContentStart, block.ContentEnd);
        return range.Text;
    }

    private static byte[]? BitmapSourceToPngBytes(BitmapSource source)
    {
        try
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
