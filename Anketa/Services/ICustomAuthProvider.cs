namespace Anketa.Services
{
    public interface ICustomAuthProvider
    {
        Task LoginUser(string userName, string email, string role, string userId);
        Task LogoutUser();
        Task<bool> IsUserAuthenticated();
        Task<string?> GetCurrentUserName();
    }
}
