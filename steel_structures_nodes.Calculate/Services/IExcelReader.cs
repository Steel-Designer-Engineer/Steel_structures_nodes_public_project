using System.Collections.Generic;
using Steel_structures_nodes_public_project.Calculate.Models;

namespace Steel_structures_nodes_public_project.Calculate.Services
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
