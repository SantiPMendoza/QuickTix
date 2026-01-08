namespace QuickTix.Desktop.Services
{
    /// <summary>
    /// Contrato para el almacenamiento del token JWT en el cliente.
    /// Permite abstraer el mecanismo de persistencia (memoria, disco, secure storage, etc.).
    /// </summary>
    public interface ITokenStore
    {
        /// <summary>
        /// Obtiene el token actual (si existe).
        /// </summary>
        /// <returns>Token JWT o null.</returns>
        string? GetToken();

        /// <summary>
        /// Almacena el token actual.
        /// </summary>
        /// <param name="token">Token JWT (o null si se quiere limpiar).</param>
        void SetToken(string? token);

        /// <summary>
        /// Limpia el token almacenado.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Implementación simple en memoria del <see cref="ITokenStore"/>.
    /// Útil para sesión en ejecución; no persiste entre reinicios.
    /// </summary>
    public sealed class TokenStore : ITokenStore
    {
        // Token JWT actual en memoria
        private string? _token;

        /// <summary>
        /// Obtiene el token actual almacenado.
        /// </summary>
        /// <returns>Token JWT o null.</returns>
        public string? GetToken() => _token;

        /// <summary>
        /// Establece el token actual almacenado.
        /// </summary>
        /// <param name="token">Token JWT.</param>
        public void SetToken(string? token) => _token = token;

        /// <summary>
        /// Elimina el token almacenado.
        /// </summary>
        public void Clear() => _token = null;
    }
}
