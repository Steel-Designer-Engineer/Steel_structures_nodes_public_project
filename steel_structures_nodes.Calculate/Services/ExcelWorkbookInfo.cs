using System.Collections.Generic;

namespace Steel_structures_nodes_public_project.Calculate.Services
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
