using System.Collections.Generic;

namespace steel_structures_nodes.Calculate.Services
{
    /// <summary>
    /// Метаданные Excel-книги: содержит список имён листов.
    /// </summary>
    public class ExcelWorkbookInfo
    {
        public ExcelWorkbookInfo(List<string> sheetNames)
        {
            SheetNames = sheetNames;
        }
        public List<string> SheetNames { get; }
    }
}
