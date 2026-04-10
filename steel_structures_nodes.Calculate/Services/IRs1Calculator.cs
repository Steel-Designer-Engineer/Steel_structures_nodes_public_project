using System.Collections.Generic;
using steel_structures_nodes.Calculate.Models;

namespace steel_structures_nodes.Calculate.Services
{
    /// <summary>
    /// Интерфейс калькулятора РС1: определение максимальных усилий и построение таблицы анализа.
    /// </summary>
    public interface IRs1Calculator
    {
        /// <summary>
        /// Строит таблицу анализа РС1 из строк РСУ и РСН.
        /// РСУ и РСН обрабатываются независимо — если один из источников пуст или null,
        /// расчёт ведётся по имеющимся данным.
        /// </summary>
        /// <param name="rsu">Исходные строки РСУ (может быть <c>null</c>).</param>
        /// <param name="rsn">Исходные строки РСН (может быть <c>null</c>).</param>
        /// <returns>Список строк анализа (MAX-строки). Если вход пуст — возвращается пустой список.</returns>
        IReadOnlyList<Calculate.AnalysisRow> BuildAnalysisTable(IReadOnlyList<ForceRow> rsu, IReadOnlyList<ForceRow> rsn);

        /// <summary>
        /// Строит таблицу анализа РС1, фильтруя данные по указанным номерам элементов.
        /// РСУ и РСН обрабатываются независимо — null подставляется как пустой список.
        /// </summary>
        /// <param name="rsu">Исходные строки РСУ (может быть <c>null</c>).</param>
        /// <param name="rsn">Исходные строки РСН (может быть <c>null</c>).</param>
        /// <param name="elementFilter">Набор номеров элементов для фильтра (если <c>null</c> или пусто — фильтр не применяется).</param>
        /// <returns>Список строк анализа (MAX-строки).</returns>
        IReadOnlyList<Calculate.AnalysisRow> BuildAnalysisTable(IReadOnlyList<ForceRow> rsu, IReadOnlyList<ForceRow> rsn, ISet<string> elementFilter);

        /// <summary>
        /// Извлекает сводку экстремальных значений из готовой таблицы анализа.
        /// </summary>
        /// <param name="analysisRows">Готовая таблица анализа.</param>
        /// <returns>
        /// Сводку экстремальных значений в виде <see cref="ForceRow"/>:
        /// <list type="bullet">
        /// <item><description><see cref="ForceRow.AlbumNt"/> / <see cref="ForceRow.AlbumN"/></description></item>
        /// <item><description><see cref="ForceRow.SummaryQy"/> / <see cref="ForceRow.SummaryQz"/></description></item>
        /// <item><description><see cref="ForceRow.SummaryMx"/> / <see cref="ForceRow.SummaryMy"/> / <see cref="ForceRow.SummaryMz"/></description></item>
        /// <item><description><see cref="ForceRow.MaxU"/> / <see cref="ForceRow.SummaryPsi"/></description></item>
        /// </list>
        /// </returns>
        ForceRow ExtractSummary(IReadOnlyList<Calculate.AnalysisRow> analysisRows);

        /// <summary>
        /// Выполняет полный расчёт, создаёт новый файл Result_vXXX.json в указанном каталоге.
        /// Каждый вызов создаёт файл с новой версией (автоинкремент).
        /// РСУ и РСН независимы: если один из источников null или пуст — расчёт ведётся по имеющимся данным.
        /// Возвращает результат и путь к созданному файлу через out-параметр.
        /// </summary>
        /// <param name="rsu">Исходные строки РСУ (может быть <c>null</c>).</param>
        /// <param name="rsn">Исходные строки РСН (может быть <c>null</c>).</param>
        /// <param name="elementFilter">Фильтр по элементам (если задан).</param>
        /// <param name="resultDir">Каталог, куда сохраняются версии <c>Result_vXXX.json</c>.</param>
        /// <param name="createdFilePath">Путь к созданному файлу результата.</param>
        /// <returns>Результат расчёта, сериализуемый в JSON.</returns>
        ForceRow CalculateAndSave(
            IReadOnlyList<ForceRow> rsu,
            IReadOnlyList<ForceRow> rsn,
            ISet<string> elementFilter,
            string resultDir,
            out string createdFilePath);
    }
}
