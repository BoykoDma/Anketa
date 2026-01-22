namespace Anketa.Models
{
    public class TestResultModel
    {
        public int Id { get; set; }
        public int TestId { get; set; }

        // Информация о проходящем
        //public string? UserId { get; set; } // null для анонимных
        public string? UserName { get; set; }
        public string? Group { get; set; }
        public int? Age { get; set; }

        // Результаты
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public decimal Score { get; set; } = 0;
        public decimal MaxScore { get; set; } = 0;

        // Ответы пользователя в JSON формате
        public string AnswersJson { get; set; } = string.Empty;

        // Навигационное свойство
        public Test? Test { get; set; }
    }
}
