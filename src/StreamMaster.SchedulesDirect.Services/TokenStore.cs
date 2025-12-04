namespace StreamMaster.SchedulesDirect.Services
{
    public interface ITokenStore
    {
        string? Token { get; }

        void SetToken(string? token);

        void ClearToken();
    }

    public class TokenStore : ITokenStore
    {
        private volatile string? _token;

        public string? Token => _token;

        public void SetToken(string? token)
        {
            _token = token;
        }

        public void ClearToken()
        {
            _token = null;
        }
    }
}