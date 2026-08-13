using System.ComponentModel.DataAnnotations;

namespace ProjectWork.Models;

public class Event : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Название обязательно для заполнения")]
    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public required int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }



    /// <summary>
    /// Создать событие: проверяет данные, присваивает идентификатор
    /// и делает все места свободными. Бросает <see cref="ValidationException"/>,
    /// если данные некорректны
    /// </summary>
    public static Event Create(string title, string? description,
        DateTime startAt, DateTime endAt, int totalSeats)
    {
        var errors = GetErrors(title, startAt, endAt, totalSeats).ToList();

        if (errors.Count > 0)
            throw new ValidationException(string.Join(" ", errors.Select(e => e.Message)));

        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var (field, message) in GetErrors(Title, StartAt, EndAt, TotalSeats))
        {
            yield return new ValidationResult(message, [field]);
        }
    }

    // Единый набор правил: используется и фабрикой Create, и валидацией модели
    private static IEnumerable<(string Field, string Message)> GetErrors(
        string? title, DateTime startAt, DateTime endAt, int totalSeats)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            yield return (nameof(Title), "Название обязательно для заполнения");
        }

        if (startAt == default)
        {
            yield return (nameof(StartAt), "Время начала обязательно к заполнению");
        }

        if (endAt == default)
        {
            yield return (nameof(EndAt), "Время окончания обязательно к заполнению");
        }

        if (startAt != default &&
            endAt != default &&
            endAt <= startAt)
        {
            yield return (nameof(EndAt), "Время окончания должно быть позже времени начала");
        }

        if (totalSeats <= 0)
        {
            yield return (nameof(TotalSeats), "Общее количество мест должно быть положительным числом");
        }
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;
        AvailableSeats -= count;
        return true;
    }

    /// <summary>
    /// Освободить места. Свободных мест не может стать больше, чем всего
    /// </summary>
    public void ReleaseSeats(int count = 1)
    {
        AvailableSeats = Math.Min(AvailableSeats + count, TotalSeats);
    }

}

