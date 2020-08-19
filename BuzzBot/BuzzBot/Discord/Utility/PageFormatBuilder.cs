using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuzzBot.Discord.Utility;
using Discord;

namespace BuzzBot.Discord.Utility
{
}
public class PageFormatBuilder
{
    private bool _alternate;
    private readonly List<ColumnData> _columnData = new List<ColumnData>();
    private readonly List<RowData> _rowData = new List<RowData>();
    private int _linesPerPage = 15;
    private IEmote _revealEmote = new Emoji("🔎");

    public PageFormatBuilder ConfigureRevealEmote(IEmote emote)
    {
        _revealEmote = emote;
        return this;
    }
    public PageFormatBuilder AddHiddenColumn(string columnName,  int index = -1)
        => AddColumn(columnName, true, index);

    private PageFormatBuilder AddColumn(string columnName, bool isHidden, int index)
    {
        if (_rowData.Any())
        {
            throw new InvalidOperationException("Unable to add a new column, row data has already been provided");
        }
        var columnData = new ColumnData()
        {
            Id = columnName,
            Title = columnName,
            IsHidden = isHidden
        };
        if (index == -1)
            _columnData.Add(columnData);
        else
        {
            _columnData.Insert(index, columnData);
        }
        return this;
    }

    /// <summary>
    /// Adds a new column to the Page Format builder
    /// </summary>
    /// <param name="columnName">Column name to serve as the identifier</param>
    /// <param name="displayName">Name to display, will default to the columnName parameter</param>
    /// <param name="index">Index to insert the column, will default to end</param>
    /// <returns></returns>
    public PageFormatBuilder AddColumn(string columnName, int index = -1)
        => AddColumn(columnName, false, index);
    /// <summary>
    /// Configures the builder to alternate each row between green and red
    /// </summary>
    /// <returns></returns>
    public PageFormatBuilder AlternateRowColors()
    {
        _alternate = true;
        return this;
    }

    public PageFormatBuilder LinesPerPage(int number)
    {
        _linesPerPage = number;
        return this;
    }

    /// <summary>
    /// Adds a row
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    public PageFormatBuilder AddRow(string[] fields)
    {
        if (fields.Length != _columnData.Count)
        {
            throw new InvalidOperationException(
                "Incomplete field data, array must match the number of configured columns");
        }
        var rowData = new RowData() { Fields = fields };
        _rowData.Add(rowData);
        return this;
    }

    private BasePageFormat Build(bool buildHiddenColumns)
    {
        var columnData = _columnData.Where(cd => buildHiddenColumns | !cd.IsHidden).ToList();
        var hiddenColumnIndices = _columnData
            .Where(cd => !buildHiddenColumns & cd.IsHidden)
            .Select(cd => _columnData.IndexOf(cd))
            .ToArray();
        var numberOfColumns = columnData.Count;
        var minimumPageWidth = 80;
        var maximumPageWidth = 160;
        List<int> minimumColumnSizes = new List<int>();
        for (int i = 0; i < numberOfColumns; i++)
        {
            if (hiddenColumnIndices.Contains(i)) continue;
            var minimumColumnSize = Math.Max(_columnData[0].Title.Length,
                _rowData.Select(rd => rd.Fields[i]?.Length ?? 0).Max()) + 2;
            minimumColumnSizes.Add(minimumColumnSize);
        }

        var projectedWidth = minimumColumnSizes.Sum() + 2 * numberOfColumns + 1;
        if (projectedWidth > maximumPageWidth)
            throw new InvalidOperationException($"Unable to build page format, page width would exceed maximum allowable of {maximumPageWidth} characters");
        List<int> columnSizeActuals = new List<int>();
        if (projectedWidth < minimumPageWidth)
        {
            var normalizedColumnWidth = minimumPageWidth / numberOfColumns;
            var extra = minimumPageWidth - projectedWidth;
            var numberToAddTo = minimumColumnSizes.Count(val => val < normalizedColumnWidth);
            foreach (var value in minimumColumnSizes.Where(val => val >= normalizedColumnWidth))
            {
                var surplus = value - normalizedColumnWidth;
                extra -= surplus;
            }

            var toAdd = extra / numberToAddTo;
            foreach (var value in minimumColumnSizes)
            {
                if (value >= normalizedColumnWidth)
                {
                    columnSizeActuals.Add(value);
                    continue;
                }
                var newValue = value + toAdd;
                columnSizeActuals.Add(newValue);
            }
        }
        else
        {
            columnSizeActuals = minimumColumnSizes;
        }
        var returnValue = new BasePageFormat() { LinesPerPage = _linesPerPage };
        var actualWidth = columnSizeActuals.Sum() + numberOfColumns + 1;
        var headerSb = new StringBuilder("  ");
        var horizontalRuleSb = new StringBuilder();
        var widthShouldBe = 2;
        for (var i = 0; i < _columnData.Count; i++)
        {
            if (hiddenColumnIndices.Contains(i)) continue;
            widthShouldBe += columnSizeActuals[i];
            if (i != _columnData.Count - 1) widthShouldBe += 1;
            var column = _columnData[i];
            headerSb.Append(column.Title);
            while (headerSb.Length < widthShouldBe)
            {
                headerSb.Append(' ');
            }
        }

        while (horizontalRuleSb.Length < actualWidth)
        {
            horizontalRuleSb.Append('-');
        }

        returnValue.HeaderLine = headerSb.ToString();
        returnValue.HorizontalRule = horizontalRuleSb.ToString();
        var contentLines = new List<string>();
        var alternate = false;
        foreach (var row in _rowData)
        {
            contentLines.Add(CreateLine(row, columnSizeActuals, alternate, hiddenColumnIndices));
            if (_alternate) alternate = !alternate;
        }

        returnValue.ContentLines = contentLines;
        return returnValue;
    }

    /// <summary>
    /// Returns a page format object using all of the parameters set up by the builder
    /// </summary>
    /// <returns></returns>
    public PageFormat Build()
    {
        var formatBase = Build(false);
        var format = new PageFormat
        {
            HeaderLine = formatBase.HeaderLine,
            ContentLines = formatBase.ContentLines,
            HorizontalRule = formatBase.HorizontalRule,
            LinesPerPage = formatBase.LinesPerPage,
            RevealEmote = _revealEmote
    };
        if (!_columnData.Any(cd => cd.IsHidden))
            return format;
        format.HasHiddenColumns = true;
        format.RevealedPageFormat = Build(true);
        return format;
    }

    private string CreateLine(RowData rowData, List<int> columnSizes, bool alternate, int[] hiddenColumnIndices)
    {
        var codeIdentifier = alternate ? "- " : "+ ";
        var lineSb = new StringBuilder(codeIdentifier);
        var sizeShouldBe = 2;
        for (var i = 0; i < rowData.Fields.Length; i++)
        {
            if(hiddenColumnIndices.Contains(i))continue;
            var columnSize = columnSizes[i];
            sizeShouldBe += columnSize + 1;
            var field = rowData.Fields[i];
            lineSb.Append(field);
            while (lineSb.Length < sizeShouldBe)
                lineSb.Append(' ');
        }

        return lineSb.ToString();
    }

    private class ColumnData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public bool IsHidden { get; set; }
    }

    public class RowData
    {
        public string[] Fields { get; set; }
    }
}
