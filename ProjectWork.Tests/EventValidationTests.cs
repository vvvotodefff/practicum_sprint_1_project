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

    [Fact]
    public void Validate_CorrectEvent_PassesValidation()
    {
        var eventItem = new Event
        {
            Title = "Встреча",
            StartAt = new DateTime(2026, 7, 10, 9, 0, 0),
            EndAt = new DateTime(2026, 7, 10, 10, 0, 0)
        };

        var results = Validate(eventItem);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EmptyTitle_FailsValidation()
    {
        var eventItem = new Event
        {
            Title = "",
            StartAt = new DateTime(2026, 7, 10, 9, 0, 0),
            EndAt = new DateTime(2026, 7, 10, 10, 0, 0)
        };

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.Title)));
    }

    [Fact]
    public void Validate_MissingDates_FailsValidation()
    {
        var eventItem = new Event
        {
            Title = "Встреча"
            // StartAt и EndAt не заданы — остаются default
        };

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.StartAt)));
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }

    [Fact]
    public void Validate_EndAtBeforeStartAt_FailsValidation()
    {
        var eventItem = new Event
        {
            Title = "Встреча",
            StartAt = new DateTime(2026, 7, 10, 10, 0, 0),
            EndAt = new DateTime(2026, 7, 10, 9, 0, 0)
        };

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }

    [Fact]
    public void Validate_EndAtEqualsStartAt_FailsValidation()
    {
        var moment = new DateTime(2026, 7, 10, 9, 0, 0);
        var eventItem = new Event
        {
            Title = "Встреча",
            StartAt = moment,
            EndAt = moment
        };

        var results = Validate(eventItem);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Event.EndAt)));
    }
}
