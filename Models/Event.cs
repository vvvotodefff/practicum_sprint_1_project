using System.ComponentModel.DataAnnotations;

namespace ProjectWork.Models;

public class Event : IValidatableObject
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Название обязательно")]
    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt == default)
        {
            yield return new ValidationResult("Время начала обязательно к заполнению", [nameof(StartAt)]);
        }

        if (EndAt == default)
        {
            yield return new ValidationResult("Время окончания обязательно к заполнению", [nameof(EndAt)]);
        }

        if (StartAt != default &&
            EndAt != default &&
            EndAt <= StartAt)
        {
            yield return new ValidationResult("Время окончания должно быть позже времени начала", [nameof(EndAt)]);
        }

    }
}

