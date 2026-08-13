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

        if (TotalSeats <= 0)
        {
            yield return new ValidationResult("Общее количество мест должно быть положительным числом", [nameof(TotalSeats)]);
        }
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (AvailableSeats < count)
            return false;
        AvailableSeats -= count;
        return true;
    }

    public bool ReleaseSeats(int count = 1)
    {
        if (AvailableSeats + count > TotalSeats)
        {
            AvailableSeats = TotalSeats;
        }
        else
        { 
            AvailableSeats += count; 
        }
        
        return true;
    }

}

