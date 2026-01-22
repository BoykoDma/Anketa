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
        // Убираем ручное поле Points
        // public decimal Points { get; set; } = 1; <- УДАЛИТЬ

        // Для вопросов с выбором
        public List<AnswerOptionViewModel> AnswerOptions { get; set; } = new();

        // Для текстовых вопросов
        public string? CorrectTextAnswer { get; set; }

        // Добавляем автоматическое свойство только для чтения
        public decimal Points => 1m; // Будет пересчитываться динамически
    }

    public class AnswerOptionViewModel
    {
        public int? Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    // Для прохождения теста
    public class TestTakingViewModel
    {
        public int TestId { get; set; }
        public string? UserName { get; set; }
        public string? Group { get; set; }
        public int? Age { get; set; }
        public List<QuestionAnswerViewModel> Answers { get; set; } = new();
    }

    // Ответ на один вопрос
    public class QuestionAnswerViewModel
    {
        public int QuestionId { get; set; }
        public QuestionType QuestionType { get; set; }

        // Для SingleChoice и TrueFalse
        public int? SelectedOptionId { get; set; }

        // Для MultipleChoice
        public List<int> SelectedOptionIds { get; set; } = new();

        // Для TextAnswer
        public string? TextAnswer { get; set; }
    }
}
