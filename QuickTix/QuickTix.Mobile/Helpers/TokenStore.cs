namespace QuickTix.Mobile.Helpers
{
    /// <summary>
    /// Contrato mínimo para persistir el token de autenticación en el cliente móvil.
    /// Permite recuperar, almacenar y limpiar el token actual.
    /// </summary>
    public interface ITokenStore
    {
        string? GetToken();
        void SetToken(string? token);
        void Clear();
    }

    /// <summary>
    /// Implementación simple en memoria para almacenar el token JWT durante la vida de la aplicación.
    /// </summary>
    public sealed class TokenStore : ITokenStore
    {
        private string? _token;

        public string? GetToken() => _token;

        public void SetToken(string? token) => _token = token;

        public void Clear() => _token = null;
    }
}
