namespace Anketa.Models
{
    public class Test
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty; // Для авторизованных
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsPublished { get; set; } = true;

        // Минимальные настройки
        public bool RequireName { get; set; } = false;
        public bool RequireGroup { get; set; } = false;
        public bool RequireAge { get; set; } = false;

        // Навигационные свойства
        public List<Question> Questions { get; set; } = new();
        public List<TestResultModel> Results { get; set; } = new();
    }
}
