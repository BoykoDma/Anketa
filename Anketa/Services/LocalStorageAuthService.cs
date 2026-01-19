using Microsoft.JSInterop;
using System.Text.Json;

namespace Anketa.Services
{
    public class LocalStorageAuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string StorageKey = "auth_user_session";

        public LocalStorageAuthService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SaveUserSessionAsync(UserSession userSession)
        {
            try
            {
                var json = JsonSerializer.Serialize(userSession);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
                Console.WriteLine($"Сессия сохранена: {userSession.UserName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения сессии: {ex.Message}");
            }
        }

        public async Task<UserSession?> GetUserSessionAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine("Сессия не найдена в LocalStorage");
                    return null;
                }

                var session = JsonSerializer.Deserialize<UserSession>(json);
                Console.WriteLine($"Сессия загружена: {session?.UserName}");
                return session;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения сессии: {ex.Message}");
                return null;
            }
        }

        public async Task RemoveUserSessionAsync()
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                Console.WriteLine("Сессия удалена из LocalStorage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка удаления сессии: {ex.Message}");
            }
        }

        public async Task<bool> HasUserSessionAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", StorageKey);
                return !string.IsNullOrEmpty(json);
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserSession
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string UserId { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryTime { get; set; } = DateTime.UtcNow.AddDays(7);
    }
}
