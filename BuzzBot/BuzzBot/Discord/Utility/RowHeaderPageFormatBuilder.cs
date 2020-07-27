using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuzzBot.Discord.Utility
{
    public class RowHeaderPageFormatBuilder
    {
        private readonly List<string> _rowDefinitions = new List<string>();
        private readonly List<SectionDefinition> _sectionDefinitions = new List<SectionDefinition>();
        private int _linesPerPage = 15;

        public RowHeaderPageFormatBuilder AddRow(string rowTitle)
        {
            _rowDefinitions.Add(rowTitle);
            return this;
        }

        public RowHeaderPageFormatBuilder AddSectionDefinition(string[] rowStrings)
        {
            if (rowStrings.Length != _rowDefinitions.Count)
                throw new InvalidOperationException("Number of row strings in section definition must be equal to number of defined rows");
            var secDef = new SectionDefinition { Sections = rowStrings.ToList() };
            _sectionDefinitions.Add(secDef);
            return this;
        }

        public RowHeaderPageFormatBuilder LinesPerPage(int linesPerPage)
        {
            _linesPerPage = linesPerPage;
            return this;
        }

        private class SectionDefinition
        {
            public List<string> Sections { get; set; }
        }

        public PageFormat Build()
        {
            var returnFormat = new PageFormat() { HeaderLine = string.Empty, ContentLines = new List<string>(), LinesPerPage = _linesPerPage};

            var minimumHeaderColumnSize = _rowDefinitions.Select(rd => rd.Length).Max() + 3;
            var minimumPageWidth = 80;
            var maximumPageWidth = 160;

            foreach (var sectionDefinition in _sectionDefinitions)
            {
                var contentSectionSb = new StringBuilder();
                for (var i = 0; i < sectionDefinition.Sections.Count; i++)
                {
                    var lineSb = new StringBuilder("+");
                    lineSb.Append(_rowDefinitions[i]);
                    while (lineSb.Length < minimumHeaderColumnSize)
                        lineSb.Append(' ');
                    for (int j = 0; j < sectionDefinition.Sections[i].Length; j++)
                    {
                        if (lineSb.Length >= maximumPageWidth - 3)
                        {
                            lineSb.Append("...");
                            break;
                        }

                        lineSb.Append(sectionDefinition.Sections[i][j]);
                    }
                    while (lineSb.Length < minimumPageWidth)
                    {
                        lineSb.Append(' ');
                    }

                    lineSb.AppendLine();
                    contentSectionSb.AppendLine(lineSb.ToString());
                }
                returnFormat.ContentLines.Add(contentSectionSb.ToString());
            }

            return returnFormat;
        }
    }
}