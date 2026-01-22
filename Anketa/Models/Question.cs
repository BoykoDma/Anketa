namespace Anketa.Models
{
    // Модель вопроса
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public int Order { get; set; } = 0;
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.SingleChoice;

        // Убираем хранение баллов в БД
        // public decimal Points { get; set; } = 1; <- УДАЛИТЬ

        // Варианты ответов (только для типов с выбором)
        public List<AnswerOption> AnswerOptions { get; set; } = new();

        // Для текстовых вопросов - правильный ответ (опционально)
        public string? CorrectTextAnswer { get; set; }

        // Навигационное свойство
        public Test? Test { get; set; }
    }

    public class AnswerOption
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
    }

    public enum QuestionType
    {
        SingleChoice,    // Один правильный ✓
        MultipleChoice,  // Несколько правильных
        TextAnswer,      // Текстовый ответ
        TrueFalse        // Верно/Неверно
    }
}
