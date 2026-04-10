using steel_structures_nodes.Calculate.Models;

namespace steel_structures_nodes.Calculate.Services
{
    /// <summary>
    /// Интерфейс провайдера несущей способности узлов из альбома.
    /// </summary>
    public interface IAlbumCapacityProvider
    {
        /// <summary>
        /// Возвращает строку несущей способности по ключу (например, <c>P1-P4-P6</c>).
        /// </summary>
        /// <param name="key">Ключ строки несущей способности.</param>
        /// <returns>Строка несущей способности или <c>null</c>, если ключ не найден.</returns>
        ForceRow GetByKey(string key);
    }

    /// <summary>
    /// Провайдер-заглушка, всегда возвращающий <c>null</c>. Используется при отсутствии JSON-файла альбома.
    /// </summary>
    public class AlbumCapacityProvider : IAlbumCapacityProvider
    {
        /// <summary>
        /// Возвращает null если ничего не найдено
        /// </summary>
        /// <param name="key">Ключ строки несущей способности</param>
        /// <returns>Заглушка. Всегда возращает null</returns>
        public ForceRow GetByKey(string key)
        {
            return null;
        }
    }
}
