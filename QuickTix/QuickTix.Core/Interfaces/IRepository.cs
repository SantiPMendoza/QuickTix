using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickTix.Core.Interfaces
{
    /// <summary>
    /// Contrato base de acceso a datos para entidades persistidas.
    /// Define operaciones CRUD asíncronas y métodos auxiliares de consulta.
    /// </summary>
    /// <typeparam name="TEntity">Tipo de entidad EF Core.</typeparam>
    public interface IRepository<TEntity> where TEntity : class
    {
        /// <summary>
        /// Obtiene todas las entidades.
        /// Normalmente se usa para listados (habitualmente no-tracking).
        /// </summary>
        /// <returns>Colección de entidades.</returns>
        Task<ICollection<TEntity?>> GetAllAsync();

        /// <summary>
        /// Obtiene una entidad por su identificador.
        /// Normalmente se usa para lectura (habitualmente no-tracking).
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns>Entidad si existe; en caso contrario, null.</returns>
        Task<TEntity?> GetAsync(int id);

        /// <summary>
        /// Obtiene una entidad por su identificador preparada para actualización.
        /// Normalmente se usa con tracking e includes mínimos necesarios para edición.
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns>Entidad si existe; en caso contrario, null.</returns>
        Task<TEntity?> GetForUpdateAsync(int id);

        /// <summary>
        /// Obtiene una entidad por su identificador con detalle adicional.
        /// Se utiliza cuando se requiere más información que en <see cref="GetAsync"/>,
        /// por ejemplo includes o proyecciones específicas.
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns>Entidad si existe; en caso contrario, null.</returns>
        Task<TEntity?> GetDetailAsync(int id);

        /// <summary>
        /// Indica si existe una entidad con el identificador indicado.
        /// </summary>
        /// <param name="id">Identificador de la entidad.</param>
        /// <returns>True si existe; en caso contrario, false.</returns>
        Task<bool> ExistsAsync(int id);

        /// <summary>
        /// Crea una nueva entidad en el contexto y persiste cambios.
        /// </summary>
        /// <param name="entity">Entidad a crear.</param>
        /// <returns>True si se persiste correctamente; en caso contrario, false.</returns>
        Task<bool> CreateAsync(TEntity entity);

        /// <summary>
        /// Actualiza una entidad existente y persiste cambios.
        /// Nota: según implementación, puede asumir que la entidad ya está trackeada.
        /// </summary>
        /// <param name="entity">Entidad a actualizar.</param>
        /// <returns>True si se persiste correctamente; en caso contrario, false.</returns>
        Task<bool> UpdateAsync(TEntity entity);

        /// <summary>
        /// Elimina una entidad por su identificador y persiste cambios.
        /// </summary>
        /// <param name="id">Identificador de la entidad a eliminar.</param>
        /// <returns>True si se elimina correctamente; en caso contrario, false.</returns>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Persiste los cambios pendientes en el contexto.
        /// </summary>
        /// <returns>True si se persiste correctamente; en caso contrario, false.</returns>
        Task<bool> SaveAsync();

        /// <summary>
        /// Limpia la caché asociada al repositorio, si aplica.
        /// </summary>
        void ClearCache();
    }
}
