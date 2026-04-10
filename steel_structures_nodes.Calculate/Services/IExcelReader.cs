using System.Collections.Generic;
using steel_structures_nodes.Calculate.Models;

namespace steel_structures_nodes.Calculate.Services
{
    /// <summary>
    /// Интерфейс чтения Excel-файлов: получение информации о книге и данных РСУ/РСН.
    /// </summary>
    public interface IExcelReader
    {
        /// <summary>
        /// Возвращает информацию о книге Excel (список листов).
        /// </summary>
        ExcelWorkbookInfo GetWorkbookInfo(string filePath);

        /// <summary>
        /// Читает строки РСУ/РСН с указанного листа Excel-файла.
        /// </summary>
        List<ForceRow> ReadRsuRsn(string filePath, string sheetName);
    }
}
