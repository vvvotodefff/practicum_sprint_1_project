using System.ComponentModel.DataAnnotations;
using ProjectWork.Models;

namespace ProjectWork.Tests;

// Валидация данных события реализована в модели Event (атрибут [Required]
// и IValidatableObject.Validate), а не в сервисе — поэтому неуспешные
// сценарии с некорректными данными проверяются здесь.
public class EventValidationTests
{
    private static List<ValidationResult> Validate(Event eventItem)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(eventItem, new ValidationContext(eventItem), results, validateAllProperties: true);
        return results;
    }

    private static Event NewEvent() => Event.Create("Встреча", null,
        new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0), 100);

    [Fact]
    public void Validate_CorrectEvent_PassesValidation()
    {
        var eventItem = NewEvent();
        eventItem.Title = "Встреча";
        eventItem.StartAt = new DateTime(2026, 7, 10, 9, 0, 0);
        eventItem.EndAt = new DateTime(2026, 7, 10, 10, 0, 0);
        eventItem.TotalSeats = 100;

        var results = Validate(eventItem);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EmptyTitle_FailsValidation()
    {
        var eventItem = NewEvent();
        eventItem.Title = "";
        eventItem.StartAt = new DateTime(2026, 7, 10, 9, 0, 0);
        eventItem.EndAt = new DateTime(2026, 7, 10, 10, 0, 0);
        eventItem.TotalSeats = 100;

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.Title)));
    }

    [Fact]
    public void Validate_MissingDates_FailsValidation()
    {
        var eventItem = NewEvent();
        eventItem.Title = "Встреча";
        eventItem.TotalSeats = 100;
        eventItem.StartAt = default;
        eventItem.EndAt = default;

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.StartAt)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }

    [Fact]
    public void Validate_EndAtBeforeStartAt_FailsValidation()
    {
        var eventItem = NewEvent();
        eventItem.Title = "Встреча";
        eventItem.StartAt = new DateTime(2026, 7, 10, 10, 0, 0);
        eventItem.EndAt = new DateTime(2026, 7, 10, 9, 0, 0);
        eventItem.TotalSeats = 100;

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }

    [Fact]
    public void Validate_EndAtEqualsStartAt_FailsValidation()
    {
        var moment = new DateTime(2026, 7, 10, 9, 0, 0);
        var eventItem = NewEvent();
        eventItem.Title = "Встреча";
        eventItem.StartAt = moment;
        eventItem.EndAt = moment;
        eventItem.TotalSeats = 100;

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }
}
