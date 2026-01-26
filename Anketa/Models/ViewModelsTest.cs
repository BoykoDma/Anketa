namespace Anketa.Models
{
    public class TestViewModel
    {
        public int? Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool RequireName { get; set; }
        public bool RequireGroup { get; set; }
        public bool RequireAge { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new();
    }

    public class QuestionViewModel
    {
        public int? Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.SingleChoice;
        public List<AnswerOptionViewModel> AnswerOptions { get; set; } = new();
        public string? CorrectTextAnswer { get; set; }
        public decimal Points => 1m;
    }

    public class AnswerOptionViewModel
    {
        public int? Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    public class TestTakingViewModel
    {
        public int TestId { get; set; }
        public string? UserName { get; set; }
        public string? Group { get; set; }
        public int? Age { get; set; }
        public List<QuestionAnswerViewModel> Answers { get; set; } = new();
    }

    public class QuestionAnswerViewModel
    {
        public int QuestionId { get; set; }
        public QuestionType QuestionType { get; set; }
        public int? SelectedOptionId { get; set; }
        public List<int> SelectedOptionIds { get; set; } = new();
        public string? TextAnswer { get; set; }
    }
}
