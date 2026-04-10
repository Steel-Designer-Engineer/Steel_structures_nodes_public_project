namespace steel_structures_nodes.Wpf.Models
{
    /// <summary>
    /// РњРѕРґРµР»СЊ РїСЂРµРґСЃС‚Р°РІР»РµРЅРёСЏ РІР°СЂРёР°РЅС‚Р° СѓР·Р»РѕРІРѕРіРѕ СЃРѕРµРґРёРЅРµРЅРёСЏ РґР»СЏ РѕС‚РѕР±СЂР°Р¶РµРЅРёСЏ РІ РІС‹РїР°РґР°СЋС‰РµРј СЃРїРёСЃРєРµ.
    /// </summary>
    public sealed class ConnectionOptionViewModel
    {
        public string Code { get; set; }
        public string Description { get; set; }

        public string Display => string.IsNullOrWhiteSpace(Description)
            ? Code
            : Code + " \u2014 " + Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

        public override string ToString() => Display;
    }
}
