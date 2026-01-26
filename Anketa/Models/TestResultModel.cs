namespace Anketa.Models
{
    public class TestResultModel
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public string? UserName { get; set; }
        public string? Group { get; set; }
        public int? Age { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
        public decimal Score { get; set; } = 0;
        public decimal MaxScore { get; set; } = 0;
        public string AnswersJson { get; set; } = string.Empty;
        public Test? Test { get; set; }
    }
}
