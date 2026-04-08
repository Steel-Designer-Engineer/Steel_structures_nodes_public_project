using System.Globalization;
using Microsoft.Maui.Graphics;

namespace Steel_structures_nodes_public_project.Maui.Controls;

/// <summary>
/// Элемент данных для одной строки графика несущей способности.
/// <paramref name="Value"/> — несущая способность (альбом/табличное).
/// <paramref name="ActualValue"/> — фактическое усилие из расчёта РС1 (необязательно).
/// </summary>
public record CapacityBarItem(
    string Label,
    double Value,
    Color BarColor,
    double? ActualValue = null,
    Color? ActualColor = null);

/// <summary>
/// Рисует горизонтальную гистограмму сравнения: табличные vs расчётные.
/// Стиль аналогичен WPF-диаграмме: парные полоски, числовые значения,
/// процент использования, чередование фона строк и подсветка превышения.
/// </summary>
public class CapacityChartDrawable : IDrawable
{
    private static readonly Color TableBarColor = Color.FromArgb("#2B579A");
    private static readonly Color CalcOkColor = Color.FromArgb("#107C10");
    private static readonly Color CalcOverColor = Color.FromArgb("#E81123");
    private static readonly Color AltRowBg = Color.FromArgb("#F0F2F5");
    private static readonly Color OverRowBg = Color.FromArgb("#FDE7E9");

    private List<CapacityBarItem> _items = [];
    private bool _hasActual;

    /// <summary>
    /// Задаёт набор данных для отображения.
    /// </summary>
    public void SetData(IEnumerable<CapacityBarItem> items)
    {
        _items = items.ToList();
        _hasActual = _items.Any(i => i.ActualValue.HasValue);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (_items.Count == 0) return;

        const float labelWidth = 100f;
        const float valueWidth = 70f;
        const float ratioWidth = 55f;
        const float rowHeight = 32f;
        const float singleBarH = 14f;
        const float pairedBarH = 12f;
        const float barGap = 2f;
        const float topPadding = 4f;
        const float legendHeight = 26f;
        const float hintHeight = 16f;

        float maxValue = (float)_items.Max(i =>
        {
            double m = Math.Abs(i.Value);
            if (i.ActualValue.HasValue)
                m = Math.Max(m, Math.Abs(i.ActualValue.Value));
            return m;
        });
        if (maxValue == 0) maxValue = 1;

        float rightReserve = _hasActual ? valueWidth + ratioWidth : valueWidth;
        float barAreaWidth = dirtyRect.Width - labelWidth - rightReserve - 12f;
        if (barAreaWidth < 30) barAreaWidth = 30;

        float y = topPadding;

        // Легенда
        if (_hasActual)
        {
            DrawLegend(canvas, labelWidth, y, dirtyRect.Width - labelWidth);
            y += legendHeight;
            // Подсказка
            canvas.FontSize = 9;
            canvas.FontColor = Colors.Gray;
            canvas.DrawString("Использование = Расчётное / Табличное × 100%",
                labelWidth, y, dirtyRect.Width - labelWidth, hintHeight,
                HorizontalAlignment.Left, VerticalAlignment.Center);
            y += hintHeight;
        }

        int rowIdx = 0;
        foreach (var item in _items)
        {
            double tv = Math.Abs(item.Value);
            double cv = item.ActualValue.HasValue ? Math.Abs(item.ActualValue.Value) : 0;
            bool isOver = _hasActual && tv > 0 && cv > tv;

            // Фон строки: чередование + подсветка превышения
            if (_hasActual && isOver)
            {
                canvas.SetFillPaint(new SolidPaint(OverRowBg),
                    new RectF(0, y, dirtyRect.Width, rowHeight));
                canvas.FillRectangle(0, y, dirtyRect.Width, rowHeight);
            }
            else if (rowIdx % 2 == 1)
            {
                canvas.SetFillPaint(new SolidPaint(AltRowBg),
                    new RectF(0, y, dirtyRect.Width, rowHeight));
                canvas.FillRectangle(0, y, dirtyRect.Width, rowHeight);
            }

            // Подпись параметра
            canvas.FontSize = 12;
            canvas.FontColor = Colors.DimGray;
            canvas.DrawString(item.Label, 4, y, labelWidth - 8, rowHeight,
                HorizontalAlignment.Left, VerticalAlignment.Center);

            if (_hasActual)
            {
                // === Парный режим: табличное + расчётное ===
                float barY1 = y + 3;
                float barY2 = barY1 + pairedBarH + barGap;

                // Табличная полоска
                float ratioTable = (float)(tv / maxValue);
                float barWTable = ratioTable * barAreaWidth;
                if (barWTable < 2 && tv != 0) barWTable = 2;

                canvas.SetFillPaint(new SolidPaint(TableBarColor.WithAlpha(0.45f)),
                    new RectF(labelWidth, barY1, barWTable, pairedBarH));
                canvas.FillRoundedRectangle(labelWidth, barY1, barWTable, pairedBarH, 2);

                // Расчётная полоска
                float ratioCalc = (float)(cv / maxValue);
                float barWCalc = ratioCalc * barAreaWidth;
                if (barWCalc < 2 && cv != 0) barWCalc = 2;

                var calcColor = isOver ? CalcOverColor : CalcOkColor;

                canvas.SetFillPaint(new SolidPaint(calcColor),
                    new RectF(labelWidth, barY2, barWCalc, pairedBarH));
                canvas.FillRoundedRectangle(labelWidth, barY2, barWCalc, pairedBarH, 2);

                // Числовое табличное значение
                float valX = labelWidth + barAreaWidth + 4;
                canvas.FontSize = 10;
                canvas.FontColor = Colors.DimGray;
                string tableText = tv == 0 ? "—" : tv.ToString("0.###", CultureInfo.InvariantCulture);
                canvas.DrawString(tableText, valX, barY1, valueWidth, pairedBarH,
                    HorizontalAlignment.Right, VerticalAlignment.Center);

                // Числовое расчётное значение
                canvas.FontColor = calcColor;
                string calcText = cv == 0 ? "—" : cv.ToString("0.###", CultureInfo.InvariantCulture);
                canvas.DrawString(calcText, valX, barY2, valueWidth, pairedBarH,
                    HorizontalAlignment.Right, VerticalAlignment.Center);

                // Процент использования
                string ratioText;
                if (tv == 0 && cv == 0)
                    ratioText = "—";
                else if (tv == 0)
                    ratioText = "∞";
                else
                    ratioText = (cv / tv * 100).ToString("0.#", CultureInfo.InvariantCulture) + "%";

                float ratioX = valX + valueWidth + 2;
                canvas.FontSize = 13;
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                canvas.FontColor = isOver ? CalcOverColor : CalcOkColor;
                canvas.DrawString(ratioText, ratioX, y, ratioWidth, rowHeight,
                    HorizontalAlignment.Center, VerticalAlignment.Center);
            }
            else
            {
                // === Одиночный режим: только табличные ===
                float barY = y + (rowHeight - singleBarH) / 2;
                float ratio = (float)(tv / maxValue);
                float barW = ratio * barAreaWidth;
                if (barW < 2 && tv != 0) barW = 2;

                canvas.SetFillPaint(new SolidPaint(item.BarColor),
                    new RectF(labelWidth, barY, barW, singleBarH));
                canvas.FillRoundedRectangle(labelWidth, barY, barW, singleBarH, 3);

                canvas.FontSize = 11;
                canvas.FontColor = Colors.Black;
                string valText = tv == 0 ? "—" : tv.ToString("0.###", CultureInfo.InvariantCulture);
                canvas.DrawString(valText,
                    labelWidth + barW + 4, y, valueWidth, rowHeight,
                    HorizontalAlignment.Left, VerticalAlignment.Center);
            }

            y += rowHeight;
            rowIdx++;
        }
    }

    private void DrawLegend(ICanvas canvas, float x, float y, float width)
    {
        const float boxW = 14f;
        const float boxH = 12f;
        const float gap = 16f;

        // Табличные
        canvas.SetFillPaint(new SolidPaint(TableBarColor.WithAlpha(0.45f)),
            new RectF(x, y + 6, boxW, boxH));
        canvas.FillRoundedRectangle(x, y + 6, boxW, boxH, 2);
        canvas.FontSize = 11;
        canvas.FontColor = Colors.DimGray;
        canvas.DrawString("Табличные", x + boxW + 4, y, 70, 24,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        // Расчётные (ОК)
        float x2 = x + boxW + 4 + 70 + gap;
        canvas.SetFillPaint(new SolidPaint(CalcOkColor),
            new RectF(x2, y + 6, boxW, boxH));
        canvas.FillRoundedRectangle(x2, y + 6, boxW, boxH, 2);
        canvas.FontColor = Colors.DimGray;
        canvas.DrawString("Расчётные", x2 + boxW + 4, y, 70, 24,
            HorizontalAlignment.Left, VerticalAlignment.Center);

        // Превышение
        float x3 = x2 + boxW + 4 + 70 + gap;
        canvas.SetFillPaint(new SolidPaint(CalcOverColor),
            new RectF(x3, y + 6, boxW, boxH));
        canvas.FillRoundedRectangle(x3, y + 6, boxW, boxH, 2);
        canvas.FontColor = Colors.DimGray;
        canvas.DrawString("Превышение", x3 + boxW + 4, y, 80, 24,
            HorizontalAlignment.Left, VerticalAlignment.Center);
    }

    /// <summary>
    /// Высота, необходимая для отрисовки всех элементов.
    /// </summary>
    public float RequiredHeight
    {
        get
        {
            float legend = _hasActual ? 26f + 16f : 0f;
            return _items.Count * 32f + legend + 8f;
        }
    }
}
