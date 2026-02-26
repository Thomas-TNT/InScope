using System;
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
                    AddBlock(block, () => column.Item(), 0);
                }
            });
        });
    }

    private static void AddBlock(Block block, Func<IContainer> getItem, int indentLevel)
    {
        switch (block)
        {
            case Paragraph para:
                AddParagraph(para, getItem, indentLevel);
                break;
            case List list:
                AddList(list, getItem, indentLevel);
                break;
            case Section section:
                foreach (Block child in section.Blocks)
                    AddBlock(child, getItem, indentLevel);
                break;
            case BlockUIContainer uiContainer:
                AddBlockUIContainer(uiContainer, getItem);
                break;
            case Table table:
                AddTable(table, getItem);
                break;
        }
    }

    private static void AddParagraph(Paragraph para, Func<IContainer> getItem, int indentLevel)
    {
        var text = GetTextFromBlock(para);
        if (string.IsNullOrWhiteSpace(text))
        {
            getItem().Height(8);
            return;
        }

        var item = getItem();
        if (indentLevel > 0)
            item.PaddingLeft(indentLevel * BulletIndent);
        item.Text(text);
    }

    private static void AddList(List list, Func<IContainer> getItem, int indentLevel)
    {
        foreach (ListItem listItem in list.ListItems)
        {
            foreach (Block block in listItem.Blocks)
            {
                if (block is Paragraph p)
                {
                    var text = GetTextFromBlock(p);
                    var indent = indentLevel;
                    getItem().Row(row =>
                    {
                        if (indent > 0)
                            row.ConstantItem(indent * BulletIndent);
                        row.ConstantItem(15).Text("\u2022");
                        row.ConstantItem(5);
                        row.RelativeItem().Text(text);
                    });
                    getItem().Height(ListSpacing);
                }
                else
                {
                    AddBlock(block, getItem, indentLevel + 1);
                }
            }
        }
    }

    private static void AddBlockUIContainer(BlockUIContainer container, Func<IContainer> getItem)
    {
        if (container.Child is System.Windows.Controls.Image wpfImage &&
            wpfImage.Source is BitmapSource source)
        {
            var bytes = BitmapSourceToPngBytes(source);
            if (bytes != null && bytes.Length > 0)
            {
                getItem().Image(bytes);
            }
        }
    }

    private static void AddTable(Table table, Func<IContainer> getItem)
    {
        var text = GetTextFromBlock(table);
        if (!string.IsNullOrWhiteSpace(text))
            getItem().Text(text);
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
