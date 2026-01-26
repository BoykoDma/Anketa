namespace Anketa.Models
{
    public class Question
    {
        public int Id { get; set; }
        public int TestId { get; set; }
        public int Order { get; set; } = 0;
        public string Text { get; set; } = string.Empty;
        public QuestionType Type { get; set; } = QuestionType.SingleChoice;
        public List<AnswerOption> AnswerOptions { get; set; } = new();
        public string? CorrectTextAnswer { get; set; }
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
        SingleChoice,
        MultipleChoice,
        TextAnswer,
        TrueFalse
    }
}
