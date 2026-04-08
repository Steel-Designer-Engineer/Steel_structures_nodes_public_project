namespace Steel_structures_nodes_public_project.Calculate.Models.RSN
{
    /// <summary>
    /// Строка данных РСН (расчётные сочетания нагрузок). Содержит дополнительное поле типа элемента.
    /// </summary>
    public class RsnRow : ForceRow
    {
        public string ElemType { get; set; }
    }
}
