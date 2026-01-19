using Anketa.Models.ConnectionDB;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Anketa.Services
{
    public class UserServices
    {
        private readonly Context context;
        private readonly AuthenticationStateProvider _authProvider;

        public int userId { get; set; }
        public string userName { get; set; }
        public string userEmail { get; set; }
        private string tempUserName { get; set; }

        public UserServices(Context _context, AuthenticationStateProvider authProvider)
        {
            context = _context;
            _authProvider = authProvider;
        }

        public bool EditUserName(int id, string name)
        {
            try
            {
                var user = context.Users.FirstOrDefault(u => u.Id == id);

                if (user == null)
                {
                    return false;
                }

                user.Name = name;
                context.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении имени пользователя: {ex.Message}");
                return false;
            }
        }

        public async Task LoadUserDataAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                userName = user.Identity.Name ?? "Не указано";
                tempUserName = userName;

                var userIdString = user.FindFirst("UserId")?.Value
                                 ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(userIdString, out int parsedId))
                {
                    userId = parsedId;
                }

                userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? "Не указано";
            }
        }
    }
}
