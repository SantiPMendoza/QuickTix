namespace QuickTix.Desktop.Services
{
    public interface ITokenStore
    {
        string? GetToken();
        void SetToken(string? token);
        void Clear();
    }

    public sealed class TokenStore : ITokenStore
    {
        private string? _token;

        public string? GetToken() => _token;

        public void SetToken(string? token) => _token = token;

        public void Clear() => _token = null;
    }
}
