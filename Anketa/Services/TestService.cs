using Anketa.Models;
using Anketa.Models.ConnectionDB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;

namespace Anketa.Services
{

    public class TestService
    {
        private readonly Context context;
        private readonly ILogger<TestService> _logger;

        public TestService(Context _context, ILogger<TestService> logger)
        {
            context = _context;
            _logger = logger;
        }

        // ============ РАБОТА С РЕЗУЛЬТАТАМИ ТЕСТОВ ============

        public async Task<TestResultModel?> GetTestResultAsync(int resultId)
        {
            return await context.TestResults
                .Include(r => r.Test)
                .ThenInclude(t => t.Questions)
                .FirstOrDefaultAsync(r => r.Id == resultId);
        }

        public async Task<Test?> GetTestAsync(int testId)
        {
            try
            {
                var test = await context.Tests
                    .FirstOrDefaultAsync(t => t.Id == testId && t.IsPublished);

                if (test == null)
                    return null;

                // Загружаем вопросы отдельно
                test.Questions = await context.Questions
                    .Where(q => q.TestId == testId)
                    .OrderBy(q => q.Order)
                    .ToListAsync();

                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении теста {TestId}", testId);
                return null;
            }
        }

        public async Task<TestResultModel> SaveTestResultAsync(TestTakingViewModel model)
        {
            try
            {
                // Получаем тест и вопросы для расчета баллов
                var test = await context.Tests
                    .FirstOrDefaultAsync(t => t.Id == model.TestId);

                if (test == null)
                    throw new Exception($"Тест с ID {model.TestId} не найден");

                var questions = await context.Questions
                    .Where(q => q.TestId == model.TestId)
                    .ToListAsync();

                // Рассчитываем баллы
                var scoreResult = CalculateScore(questions, model.Answers);

                // Создаем результат
                var testResult = new TestResultModel
                {
                    TestId = model.TestId,
                    UserName = model.UserName,
                    Group = model.Group,
                    Age = model.Age,
                    CompletedAt = DateTime.UtcNow,
                    Score = scoreResult.ActualScore,
                    MaxScore = scoreResult.MaxScore,
                    AnswersJson = JsonSerializer.Serialize(model.Answers)
                };

                // Сохраняем
                context.TestResults.Add(testResult);
                await context.SaveChangesAsync();

                _logger.LogInformation("Сохранен результат теста {TestId}, результат ID: {ResultId}",
                    model.TestId, testResult.Id);

                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении результата теста");
                throw;
            }
        }

        private void ValidateRequiredFields(Test test, TestTakingViewModel model)
        {
            if (test.RequireName && string.IsNullOrWhiteSpace(model.UserName))
                throw new Exception("Имя пользователя обязательно для этого теста");

            if (test.RequireGroup && string.IsNullOrWhiteSpace(model.Group))
                throw new Exception("Группа обязательна для этого теста");

            if (test.RequireAge && !model.Age.HasValue)
                throw new Exception("Возраст обязателен для этого теста");
        }

        private ScoreCalculationResult CalculateScore(List<Question> questions, List<QuestionAnswerViewModel> answers)
        {
            decimal totalScore = 0;
            decimal maxScore = questions.Sum(q => q.Points);

            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                totalScore += CalculateQuestionScore(question, answer);
            }

            return new ScoreCalculationResult
            {
                ActualScore = totalScore,
                MaxScore = maxScore
            };
        }

        private decimal CalculateQuestionScore(Question question, QuestionAnswerViewModel? answer)
        {
            if (answer == null)
                return 0;

            return question.Type switch
            {
                QuestionType.SingleChoice => CalculateSingleChoiceScore(question, answer),
                QuestionType.MultipleChoice => CalculateMultipleChoiceScore(question, answer),
                QuestionType.TextAnswer => CalculateTextAnswerScore(question, answer),
                QuestionType.TrueFalse => CalculateTrueFalseScore(question, answer),
                _ => 0
            };
        }

        private decimal CalculateSingleChoiceScore(Question question, QuestionAnswerViewModel answer)
        {
            var correctOption = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
            if (correctOption == null) return 0;

            return answer.SelectedOptionId == correctOption.Id ? question.Points : 0;
        }

        private decimal CalculateMultipleChoiceScore(Question question, QuestionAnswerViewModel answer)
        {
            var correctOptions = question.AnswerOptions.Where(o => o.IsCorrect).ToList();
            if (correctOptions.Count == 0) return 0;

            // Подсчитываем правильные выборы пользователя
            int correctSelected = 0;
            int totalSelected = answer.SelectedOptionIds.Count;

            foreach (var selectedId in answer.SelectedOptionIds)
            {
                if (correctOptions.Any(co => co.Id == selectedId))
                    correctSelected++;
            }

            // Все правильные должны быть выбраны, ничего лишнего
            if (correctSelected == correctOptions.Count && totalSelected == correctOptions.Count)
                return question.Points;

            return 0;
        }

        private decimal CalculateTextAnswerScore(Question question, QuestionAnswerViewModel answer)
        {
            if (string.IsNullOrWhiteSpace(answer.TextAnswer) ||
                string.IsNullOrWhiteSpace(question.CorrectTextAnswer))
                return 0;

            var userAnswer = answer.TextAnswer.Trim().ToLower();
            var correctAnswer = question.CorrectTextAnswer.Trim().ToLower();

            return userAnswer == correctAnswer ? question.Points : 0;
        }

        private decimal CalculateTrueFalseScore(Question question, QuestionAnswerViewModel answer)
        {
            if (!answer.SelectedOptionId.HasValue) return 0;

            var selectedOption = question.AnswerOptions
                .FirstOrDefault(o => o.Id == answer.SelectedOptionId.Value);

            return selectedOption?.IsCorrect == true ? question.Points : 0;
        }

        //public async Task<TestResult?> GetTestResultAsync(int resultId)
        //{
        //    return await context.TestResults
        //        .Include(r => r.Test)
        //        .FirstOrDefaultAsync(r => r.Id == resultId);
        //}

        public async Task<List<TestResultModel>> GetTestResultsAsync(int testId)
        {
            return await context.TestResults
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        public async Task<TestResultDetailsViewModel> GetResultDetailsAsync(int resultId)
        {
            var result = await context.TestResults
                .Include(r => r.Test)
                .ThenInclude(t => t.Questions)
                .ThenInclude(q => q.AnswerOptions)
                .FirstOrDefaultAsync(r => r.Id == resultId);

            if (result == null)
                throw new Exception($"Результат с ID {resultId} не найден");

            var userAnswers = JsonSerializer.Deserialize<List<QuestionAnswerViewModel>>(
                result.AnswersJson) ?? new List<QuestionAnswerViewModel>();

            var questionDetails = new List<QuestionResultDetail>();

            foreach (var question in result.Test!.Questions.OrderBy(q => q.Order))
            {
                var userAnswer = userAnswers.FirstOrDefault(a => a.QuestionId == question.Id);
                var isCorrect = CalculateQuestionScore(question, userAnswer) > 0;

                questionDetails.Add(new QuestionResultDetail
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    Points = question.Points,
                    UserAnswer = FormatUserAnswer(question, userAnswer),
                    CorrectAnswer = GetCorrectAnswerText(question),
                    IsCorrect = isCorrect,
                    PointsAwarded = isCorrect ? question.Points : 0
                });
            }

            return new TestResultDetailsViewModel
            {
                ResultId = result.Id,
                TestTitle = result.Test.Title,
                UserName = result.UserName,
                Group = result.Group,
                Age = result.Age,
                CompletedAt = result.CompletedAt,
                Score = result.Score,
                MaxScore = result.MaxScore,
                Percentage = result.MaxScore > 0 ? (result.Score / result.MaxScore) * 100 : 0,
                QuestionDetails = questionDetails
            };
        }

        private string FormatUserAnswer(Question question, QuestionAnswerViewModel? answer)
        {
            if (answer == null) return "Нет ответа";

            return question.Type switch
            {
                QuestionType.SingleChoice => GetSelectedOptionText(question, answer.SelectedOptionId),
                QuestionType.MultipleChoice => GetMultipleOptionsText(question, answer.SelectedOptionIds),
                QuestionType.TextAnswer => answer.TextAnswer ?? "Нет ответа",
                QuestionType.TrueFalse => GetSelectedOptionText(question, answer.SelectedOptionId),
                _ => "Неизвестный тип вопроса"
            };
        }

        private string GetSelectedOptionText(Question question, int? optionId)
        {
            if (!optionId.HasValue) return "Не выбран";

            var option = question.AnswerOptions.FirstOrDefault(o => o.Id == optionId.Value);
            return option?.Text ?? "Неизвестный вариант";
        }

        private string GetMultipleOptionsText(Question question, List<int> optionIds)
        {
            if (optionIds.Count == 0) return "Не выбрано";

            var selectedOptions = question.AnswerOptions
                .Where(o => optionIds.Contains(o.Id))
                .Select(o => o.Text);

            return string.Join(", ", selectedOptions);
        }

        private string GetCorrectAnswerText(Question question)
        {
            return question.Type switch
            {
                QuestionType.SingleChoice => question.AnswerOptions.FirstOrDefault(o => o.IsCorrect)?.Text
                    ?? "Нет правильного ответа",
                QuestionType.MultipleChoice => string.Join(", ",
                    question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Text)),
                QuestionType.TextAnswer => question.CorrectTextAnswer ?? "Нет образца ответа",
                QuestionType.TrueFalse => question.AnswerOptions.FirstOrDefault(o => o.IsCorrect)?.Text
                    ?? "Нет правильного ответа",
                _ => "Неизвестный тип вопроса"
            };
        }

        // ============ РАБОТА С ТЕСТАМИ ============

        public async Task<Test> CreateTestAsync(TestViewModel model, string userId)
        {
            try
            {
                var test = new Test
                {
                    Title = model.Title,
                    Description = model.Description,
                    CreatedByUserId = userId,
                    CreatedDate = DateTime.UtcNow,
                    IsPublished = true,
                    RequireName = model.RequireName,
                    RequireGroup = model.RequireGroup,
                    RequireAge = model.RequireAge
                };

                // Добавляем вопросы
                int questionOrder = 1;
                foreach (var questionVm in model.Questions)
                {
                    var question = new Question
                    {
                        Order = questionOrder++,
                        Text = questionVm.Text,
                        Type = questionVm.Type,
                        Points = questionVm.Points,
                        CorrectTextAnswer = questionVm.CorrectTextAnswer
                    };

                    // Добавляем варианты ответов
                    int optionOrder = 1;
                    foreach (var optionVm in questionVm.AnswerOptions)
                    {
                        question.AnswerOptions.Add(new AnswerOption
                        {
                            Text = optionVm.Text,
                            IsCorrect = optionVm.IsCorrect,
                            Id = optionOrder++ // Временный ID для связей
                        });
                    }

                    test.Questions.Add(question);
                }

                context.Tests.Add(test);
                await context.SaveChangesAsync();

                _logger.LogInformation("Создан тест {TestId} пользователем {UserId}", test.Id, userId);
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании теста");
                throw;
            }
        }

        public async Task<Test> UpdateTestAsync(int testId, TestViewModel model)
        {
            try
            {
                var test = await context.Tests
                    .Include(t => t.Questions)
                    .ThenInclude(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(t => t.Id == testId);

                if (test == null)
                    throw new Exception($"Тест с ID {testId} не найден");

                // Обновляем основные свойства
                test.Title = model.Title;
                test.Description = model.Description;
                test.RequireName = model.RequireName;
                test.RequireGroup = model.RequireGroup;
                test.RequireAge = model.RequireAge;

                // Удаляем старые вопросы
                context.Questions.RemoveRange(test.Questions);

                // Добавляем новые вопросы
                int questionOrder = 1;
                foreach (var questionVm in model.Questions)
                {
                    var question = new Question
                    {
                        TestId = testId,
                        Order = questionOrder++,
                        Text = questionVm.Text,
                        Type = questionVm.Type,
                        Points = questionVm.Points,
                        CorrectTextAnswer = questionVm.CorrectTextAnswer
                    };

                    // Добавляем варианты ответов
                    foreach (var optionVm in questionVm.AnswerOptions)
                    {
                        question.AnswerOptions.Add(new AnswerOption
                        {
                            Text = optionVm.Text,
                            IsCorrect = optionVm.IsCorrect
                        });
                    }

                    context.Questions.Add(question);
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("Обновлен тест {TestId}", testId);
                return test;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении теста");
                throw;
            }
        }

        public async Task<bool> DeleteTestAsync(int testId)
        {
            try
            {
                var test = await context.Tests.FindAsync(testId);
                if (test == null)
                    return false;

                context.Tests.Remove(test);
                await context.SaveChangesAsync();

                _logger.LogInformation("Удален тест {TestId}", testId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении теста");
                return false;
            }
        }

        //public async Task<Test?> GetTestAsync(int testId)
        //{
        //    return await context.Tests
        //        .Include(t => t.Questions)
        //        .ThenInclude(q => q.AnswerOptions)
        //        .FirstOrDefaultAsync(t => t.Id == testId);
        //}

        public async Task<List<Test>> GetUserTestsAsync(string userId)
        {
            return await context.Tests
                .Where(t => t.CreatedByUserId == userId)
                .Include(t => t.Questions)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Test>> GetPublishedTestsAsync()
        {
            return await context.Tests
                .Where(t => t.IsPublished)
                .Include(t => t.Questions)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }
    }

    // Вспомогательные классы
    public class ScoreCalculationResult
    {
        public decimal ActualScore { get; set; }
        public decimal MaxScore { get; set; }
    }

    public class TestResultDetailsViewModel
    {
        public int ResultId { get; set; }
        public string TestTitle { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Group { get; set; }
        public int? Age { get; set; }
        public DateTime CompletedAt { get; set; }
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public List<QuestionResultDetail> QuestionDetails { get; set; } = new();
    }

    public class QuestionResultDetail
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public QuestionType QuestionType { get; set; }
        public decimal Points { get; set; }
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public decimal PointsAwarded { get; set; }
    }
}