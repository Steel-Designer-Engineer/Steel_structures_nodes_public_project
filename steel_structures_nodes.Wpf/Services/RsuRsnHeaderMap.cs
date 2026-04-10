using System;

namespace steel_structures_nodes.Wpf.Services
{
    /// <summary>
    /// РљР°СЂС‚Р° Р·Р°РіРѕР»РѕРІРєРѕРІ Excel-Р»РёСЃС‚Р° Р РЎРЈ/Р РЎРќ: РЅРѕРјРµСЂР° СЃС‚РѕР»Р±С†РѕРІ РґР»СЏ РєР°Р¶РґРѕРіРѕ РїРѕР»СЏ РґР°РЅРЅС‹С….
    /// </summary>
    internal sealed class RsuRsnHeaderMap
    {
        public int HeaderRow { get; set; }

        public int ColLoadCase { get; set; }
        public int ColElem { get; set; }
        public int ColSect { get; set; }
        public int ColElemType { get; set; } // RSN only

        public int ColN { get; set; }
        public int ColMx { get; set; }
        public int ColMy { get; set; }
        public int ColQz { get; set; }
        public int ColMz { get; set; }
        public int ColQy { get; set; }
        public int ColMw { get; set; }

        public bool IsValid => ColLoadCase > 0 && ColElem > 0 && ColN > 0 && ColMx > 0 && ColQz > 0 && ColMz > 0 && ColQy > 0;

        public override string ToString()
        {
            return string.Format(
                "HeaderRow={0}; LoadCase={1}; Elem={2}; Sect={3}; ElemType={4}; N={5}; MX={6}; MY={7}; QZ={8}; MZ={9}; QY={10}; MW={11}",
                HeaderRow, ColLoadCase, ColElem, ColSect, ColElemType, ColN, ColMx, ColMy, ColQz, ColMz, ColQy, ColMw);
        }
    }
}
