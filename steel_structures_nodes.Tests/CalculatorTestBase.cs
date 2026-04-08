using Steel_structures_nodes_public_project.Calculate.Calculate;
using Steel_structures_nodes_public_project.Calculate.Models;
using Steel_structures_nodes_public_project.Calculate.Services;

namespace Steel_structures_nodes_public_project.Tests
{
    /// <summary>
    /// Р‘Р°Р·РѕРІС‹Р№ РєР»Р°СЃСЃ РґР»СЏ РІСЃРµС… С‚РµСЃС‚РѕРІ РєР°Р»СЊРєСѓР»СЏС‚РѕСЂР° Р РЎ1 РёР· РїСЂРѕРµРєС‚Р° steel_structures_nodes.Calculate.
    /// РЎРѕРґРµСЂР¶РёС‚:
    /// - <see cref="TestAlbumProvider"/> вЂ” С‚РµСЃС‚РѕРІС‹Р№ РїСЂРѕРІР°Р№РґРµСЂ Р°Р»СЊР±РѕРјР° РЅРµСЃСѓС‰РµР№ СЃРїРѕСЃРѕР±РЅРѕСЃС‚Рё
    ///   СЃ РЅР°СЃС‚СЂР°РёРІР°РµРјС‹РјРё Р·РЅР°С‡РµРЅРёСЏРјРё AlbumQy, AlbumNt Рё AlbumPsi;
    /// - <see cref="Make"/> вЂ” С„Р°Р±СЂРёРєР° СЃС‚СЂРѕРє <see cref="ForceRow"/> РґР»СЏ Р±С‹СЃС‚СЂРѕРіРѕ СЃРѕР·РґР°РЅРёСЏ
    ///   С‚РµСЃС‚РѕРІС‹С… РґР°РЅРЅС‹С… (СЃС‚СЂРѕРєРё СѓСЃРёР»РёР№ РёР· Р РЎРЈ/Р РЎРќ);
    /// - <see cref="CreateCalc"/> вЂ” СЃРѕР·РґР°С‘С‚ СЌРєР·РµРјРїР»СЏСЂ <see cref="Calculator"/>
    ///   СЃ С‚РµСЃС‚РѕРІС‹Рј РїСЂРѕРІР°Р№РґРµСЂРѕРј Р°Р»СЊР±РѕРјР°.
    /// </summary>
    public abstract class CalculatorTestBase
    {
        /// <summary>
        /// РўРµСЃС‚РѕРІС‹Р№ РїСЂРѕРІР°Р№РґРµСЂ Р°Р»СЊР±РѕРјР° РЅРµСЃСѓС‰РµР№ СЃРїРѕСЃРѕР±РЅРѕСЃС‚Рё.
        /// Р’РѕР·РІСЂР°С‰Р°РµС‚ РѕРґРёРЅ Рё С‚РѕС‚ Р¶Рµ <see cref="ForceRow"/> РґР»СЏ Р»СЋР±РѕРіРѕ РєР»СЋС‡Р° вЂ”
        /// РїРѕР·РІРѕР»СЏРµС‚ РєРѕРЅС‚СЂРѕР»РёСЂРѕРІР°С‚СЊ РїР°СЂР°РјРµС‚СЂС‹ AlbumQy, AlbumNt, AlbumPsi РІ С‚РµСЃС‚Р°С….
        /// </summary>
        protected sealed class TestAlbumProvider : IAlbumCapacityProvider
        {
            private readonly ForceRow _cap;
            public TestAlbumProvider(double qy = 10.0, double nt = 100.0, double? psi = null)
            {
                _cap = new ForceRow { AlbumQy = qy, AlbumNt = nt, AlbumPsi = psi };
            }
            public ForceRow GetByKey(string key) => _cap;
        }

        /// <summary>
        /// РЎРѕР·РґР°С‘С‚ СЃС‚СЂРѕРєСѓ <see cref="ForceRow"/> СЃ СѓРєР°Р·Р°РЅРЅС‹РјРё СѓСЃРёР»РёСЏРјРё.
        /// Р’СЃРµ РїР°СЂР°РјРµС‚СЂС‹ вЂ” СЃС‚СЂРѕРєРё, С‚.Рє. ForceRow С…СЂР°РЅРёС‚ РёСЃС…РѕРґРЅС‹Рµ Р·РЅР°С‡РµРЅРёСЏ РёР· Excel/CSV РІ СЃС‚СЂРѕРєРѕРІРѕРј РІРёРґРµ.
        /// РќРµСѓРєР°Р·Р°РЅРЅС‹Рµ РїР°СЂР°РјРµС‚СЂС‹ РѕСЃС‚Р°СЋС‚СЃСЏ null (РѕС‚СЃСѓС‚СЃС‚РІРёРµ РґР°РЅРЅС‹С…).
        /// </summary>
        protected static ForceRow Make(
            string dcl = "1", string elem = "1", string sect = null,
            string n = null, string qy = null, string qz = null,
            string mx = null, string my = null, string mz = null, string mw = null)
        {
            return new ForceRow
            {
                DclNo = dcl, Elem = elem, Sect = sect,
                N = n, Qy = qy, Qz = qz,
                Mx = mx, My = my, Mz = mz, Mw = mw,
            };
        }

        /// <summary>
        /// РЎРѕР·РґР°С‘С‚ РєР°Р»СЊРєСѓР»СЏС‚РѕСЂ СЃ С‚РµСЃС‚РѕРІС‹Рј РїСЂРѕРІР°Р№РґРµСЂРѕРј Р°Р»СЊР±РѕРјР°.
        /// РџРѕ СѓРјРѕР»С‡Р°РЅРёСЋ: AlbumQy=10, AlbumNt=100, AlbumPsi=null.
        /// </summary>
        protected Calculator CreateCalc(double qy = 10, double nt = 100, double? psi = null)
            => new Calculator(new TestAlbumProvider(qy, nt, psi));
    }
}
