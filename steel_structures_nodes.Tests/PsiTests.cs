using System;
using System.Collections.Generic;
using steel_structures_nodes.Calculate.Calculate;
using steel_structures_nodes.Calculate.Models;
using Xunit;

namespace steel_structures_nodes.Tests
{
    /// <summary>
    /// РўРµСЃС‚С‹ РїРµСЂРµРјРµРЅРЅРѕР№ Psi (П€) вЂ” РєРѕСЌС„С„РёС†РёРµРЅС‚ РёР· Р°Р»СЊР±РѕРјР° РЅРµСЃСѓС‰РµР№ СЃРїРѕСЃРѕР±РЅРѕСЃС‚Рё
    /// РёР· РїСЂРѕРµРєС‚Р° steel_structures_nodes.Calculate.
    /// Psi Р·Р°РґР°С‘С‚СЃСЏ РІ Р°Р»СЊР±РѕРјРµ Рё РїСЂРѕСЃС‚Р°РІР»СЏРµС‚СЃСЏ <see cref="Calculator.BuildAnalysisTable"/>
    /// РѕРґРёРЅР°РєРѕРІРѕ РґР»СЏ РІСЃРµС… СЃС‚СЂРѕРє С‚Р°Р±Р»РёС†С‹ Р°РЅР°Р»РёР·Р°.
    /// Р’ <see cref="Calculator.ExtractSummary"/> Р±РµСЂС‘С‚СЃСЏ РїРµСЂРІРѕРµ РІСЃС‚СЂРµС‡РµРЅРЅРѕРµ Р·РЅР°С‡РµРЅРёРµ Psi.
    /// </summary>
    public class PsiTests : CalculatorTestBase
    {
        /// <summary>
        /// РџСЂРѕРІРµСЂСЏРµС‚: Psi РёР· Р°Р»СЊР±РѕРјР° (0.85) РїСЂРѕСЃС‚Р°РІР»СЏРµС‚СЃСЏ РґР»СЏ РІСЃРµС… 10 СЃС‚СЂРѕРє С‚Р°Р±Р»РёС†С‹ Р°РЅР°Р»РёР·Р°.
        /// Р­С‚Рѕ Р·РЅР°С‡РµРЅРёРµ РѕРґРёРЅР°РєРѕРІРѕ РґР»СЏ РІСЃРµС… СЃС‚СЂРѕРє MAX вЂ” РѕРЅРѕ Р·Р°РІРёСЃРёС‚ РѕС‚ С‚РёРїР° СѓР·Р»Р°, Р° РЅРµ РѕС‚ РєРѕРјР±РёРЅР°С†РёРё.
        /// </summary>
        [Fact]
        public void AllRows_HavePsi_FromAlbum()
        {
            var rows = new List<ForceRow> { Make(n: "5", qz: "1") };
            var table = CreateCalc(qy: 10, nt: 100, psi: 0.85).BuildAnalysisTable(rows, Array.Empty<ForceRow>());
            foreach (var r in table)
                Assert.Equal(0.85, r.Psi);
        }

        /// <summary>
        /// РџСЂРѕРІРµСЂСЏРµС‚ ExtractSummary: SummaryPsi Р±РµСЂС‘С‚СЃСЏ РёР· РїРµСЂРІРѕР№ СЃС‚СЂРѕРєРё Р°РЅР°Р»РёР·Р°, РіРґРµ Psi Р·Р°РїРѕР»РЅРµРЅ.
        /// Р—РґРµСЃСЊ РµРґРёРЅСЃС‚РІРµРЅРЅР°СЏ СЃС‚СЂРѕРєР° СЃ Psi=1.23 в†’ SummaryPsi=1.23.
        /// </summary>
        [Fact]
        public void SummaryPsi_FromAnalysisRows()
        {
            var analysis = new List<AnalysisRow> { new AnalysisRow { RowType = "MAX Coeff", U = 0.5, Psi = 1.23 } };
            var summary = CreateCalc().ExtractSummary(analysis);
            Assert.Equal(1.23d, summary.SummaryPsi);
        }
    }
}
