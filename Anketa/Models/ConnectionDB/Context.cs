using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Anketa.Models.ConnectionDB
{
    public class Context : DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<TestResultModel> TestResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Тест
            modelBuilder.Entity<Test>()
                .HasMany(t => t.Questions)
                .WithOne()
                .HasForeignKey(q => q.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Test>()
                .HasMany(t => t.Results)
                .WithOne(r => r.Test)
                .HasForeignKey(r => r.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Вопрос
            modelBuilder.Entity<Question>()
                .Property(q => q.AnswerOptions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<AnswerOption>>(v, (JsonSerializerOptions)null) ?? new()
                );

            // Результат
            modelBuilder.Entity<TestResultModel>()
                .Property(r => r.AnswersJson)
                .HasColumnType("nvarchar(max)");
        }
    }
}
