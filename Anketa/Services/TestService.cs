using Anketa.Components.Pages;
using Anketa.Models;
using Anketa.Models.ConnectionDB;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Anketa.Services
{
    public class TestService
    {
        private readonly Context _context;
        private readonly ILogger<TestService> _logger;

        public TestService(Context context, ILogger<TestService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ============ РАБОТА С РЕЗУЛЬТАТАМИ ТЕСТОВ ============

        public async Task<TestResultModel?> GetTestResultAsync(int resultId)
        {
            return await _context.TestResults
                .Include(r => r.Test)
                .ThenInclude(t => t.Questions)
                .FirstOrDefaultAsync(r => r.Id == resultId);
        }

        public async Task<Test?> GetTestAsync(int testId)
        {
            try
            {
                var test = await _context.Tests
                    .FirstOrDefaultAsync(t => t.Id == testId && t.IsPublished);

                if (test == null)
                    return null;

                // Загружаем вопросы отдельно
                test.Questions = await _context.Questions
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
                // Получаем тест
                var test = await _context.Tests
                    .Include(t => t.Questions)
                    .FirstOrDefaultAsync(t => t.Id == model.TestId);

                if (test == null)
                    throw new Exception($"Тест с ID {model.TestId} не найден");

                // Проверяем обязательные поля
                ValidateRequiredFields(test, model);

                // Автоматически рассчитываем баллы
                int totalQuestions = test.Questions.Count;
                decimal pointsPerQuestion = totalQuestions > 0 ? 5m / totalQuestions : 0;

                // Рассчитываем результат
                decimal totalScore = 0;

                foreach (var question in test.Questions)
                {
                    var answer = model.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                    if (answer != null && IsAnswerCorrect(question, answer))
                    {
                        totalScore += pointsPerQuestion;
                    }
                }

                // Округляем до 2 знаков после запятой
                totalScore = Math.Round(totalScore, 2);

                // Создаем результат
                var testResult = new TestResultModel
                {
                    TestId = model.TestId,
                    UserName = model.UserName,
                    Group = model.Group,
                    Age = model.Age,
                    CompletedAt = DateTime.UtcNow,
                    Score = totalScore,
                    MaxScore = 5m, // Всегда 5
                    AnswersJson = JsonSerializer.Serialize(model.Answers)
                };

                // Сохраняем
                _context.TestResults.Add(testResult);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Сохранен результат теста {TestId}, ID результата: {ResultId}",
                    model.TestId, testResult.Id);

                return testResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сохранении результата теста");
                throw;
            }
        }

        private bool IsAnswerCorrect(Question question, QuestionAnswerViewModel answer)
        {
            return question.Type switch
            {
                QuestionType.SingleChoice => CheckSingleChoice(question, answer),
                QuestionType.MultipleChoice => CheckMultipleChoice(question, answer),
                QuestionType.TextAnswer => CheckTextAnswer(question, answer),
                QuestionType.TrueFalse => CheckTrueFalse(question, answer),
                _ => false
            };
        }

        private bool CheckSingleChoice(Question question, QuestionAnswerViewModel answer)
        {
            var correctOption = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
            return correctOption != null && answer.SelectedOptionId == correctOption.Id;
        }

        private bool CheckMultipleChoice(Question question, QuestionAnswerViewModel answer)
        {
            var correctOptions = question.AnswerOptions.Where(o => o.IsCorrect).Select(o => o.Id).ToList();
            return correctOptions.Count > 0 &&
                   correctOptions.All(id => answer.SelectedOptionIds.Contains(id)) &&
                   answer.SelectedOptionIds.Count == correctOptions.Count;
        }

        private bool CheckTextAnswer(Question question, QuestionAnswerViewModel answer)
        {
            if (string.IsNullOrWhiteSpace(answer.TextAnswer) ||
                string.IsNullOrWhiteSpace(question.CorrectTextAnswer))
                return false;

            return answer.TextAnswer.Trim().ToLower() == question.CorrectTextAnswer.Trim().ToLower();
        }

        private bool CheckTrueFalse(Question question, QuestionAnswerViewModel answer)
        {
            var correctOption = question.AnswerOptions.FirstOrDefault(o => o.IsCorrect);
            return correctOption != null && answer.SelectedOptionId == correctOption.Id;
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

            // Используем 5 как максимальный балл
            decimal maxScore = 5m;

            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(a => a.QuestionId == question.Id);
                if (answer != null && IsAnswerCorrect(question, answer))
                {
                    // Каждый правильный ответ добавляет свою долю от 5 баллов
                    totalScore += maxScore / questions.Count;
                }
            }

            return new ScoreCalculationResult
            {
                ActualScore = Math.Round(totalScore, 2),
                MaxScore = maxScore
            };
        }

        public async Task<List<TestResultModel>> GetTestResultsAsync(int testId)
        {
            return await _context.TestResults
                .Where(r => r.TestId == testId)
                .OrderByDescending(r => r.CompletedAt)
                .ToListAsync();
        }

        public async Task<TestResultDetailsViewModel> GetResultDetailsAsync(int resultId)
        {
            var result = await _context.TestResults
                .Include(r => r.Test)
                .ThenInclude(t => t.Questions)
                .FirstOrDefaultAsync(r => r.Id == resultId);

            if (result == null)
                throw new Exception($"Результат с ID {resultId} не найден");

            var userAnswers = JsonSerializer.Deserialize<List<QuestionAnswerViewModel>>(
                result.AnswersJson) ?? new List<QuestionAnswerViewModel>();

            var questionDetails = new List<QuestionResultDetail>();
            decimal pointsPerQuestion = result.Test!.Questions.Count > 0 ? 5m / result.Test.Questions.Count : 0;

            foreach (var question in result.Test.Questions.OrderBy(q => q.Order))
            {
                var userAnswer = userAnswers.FirstOrDefault(a => a.QuestionId == question.Id);
                var isCorrect = IsAnswerCorrect(question, userAnswer);

                questionDetails.Add(new QuestionResultDetail
                {
                    QuestionId = question.Id,
                    QuestionText = question.Text,
                    QuestionType = question.Type,
                    Points = pointsPerQuestion,
                    UserAnswer = FormatUserAnswer(question, userAnswer),
                    CorrectAnswer = GetCorrectAnswerText(question),
                    IsCorrect = isCorrect,
                    PointsAwarded = isCorrect ? pointsPerQuestion : 0
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
                        // Баллы не сохраняем - рассчитываются автоматически
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
                            Id = optionOrder++
                        });
                    }

                    test.Questions.Add(question);
                }

                _context.Tests.Add(test);
                await _context.SaveChangesAsync();

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
                var test = await _context.Tests
                    .Include(t => t.Questions)
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
                _context.Questions.RemoveRange(test.Questions);

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

                    _context.Questions.Add(question);
                }

                await _context.SaveChangesAsync();

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
                var test = await _context.Tests.FindAsync(testId);
                if (test == null)
                    return false;

                _context.Tests.Remove(test);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Удален тест {TestId}", testId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении теста");
                return false;
            }
        }

        public async Task<List<Test>> GetUserTestsAsync(string userId)
        {
            return await _context.Tests
                .Where(t => t.CreatedByUserId == userId)
                .Include(t => t.Questions)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<Test>> GetPublishedTestsAsync()
        {
            return await _context.Tests
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