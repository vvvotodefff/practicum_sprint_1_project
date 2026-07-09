using ProjectWork.Models;
using ProjectWork.Services;

namespace ProjectWork.Tests;

public class EventServiceTests
{
    private readonly EventService _service = new();

    private static Event CreateEvent(string title, DateTime startAt, DateTime endAt) => new()
    {
        Title = title,
        StartAt = startAt,
        EndAt = endAt
    };

    private Event AddEvent(string title, DateTime startAt, DateTime endAt)
    {
        var eventItem = CreateEvent(title, startAt, endAt);
        _service.AddEvent(eventItem);
        return eventItem;
    }

    private PaginatedResult<Event> GetAll() => _service.GetEvents(null, null, null, 1, 100);

    // ----- Успешные сценарии -----

    [Fact]
    public void AddEvent_AssignsNewIdAndStoresEvent()
    {
        var eventItem = CreateEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        _service.AddEvent(eventItem);

        Assert.NotEqual(Guid.Empty, eventItem.Id);
        var stored = Assert.Single(GetAll().Items);
        Assert.Equal("Встреча", stored.Title);
    }

    [Fact]
    public void GetEvents_ReturnsAllEvents()
    {
        AddEvent("Первое", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        AddEvent("Второе", new DateTime(2026, 7, 2, 9, 0, 0), new DateTime(2026, 7, 2, 10, 0, 0));
        AddEvent("Третье", new DateTime(2026, 7, 3, 9, 0, 0), new DateTime(2026, 7, 3, 10, 0, 0));

        var result = GetAll();

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public void GetEventById_ExistingId_ReturnsEvent()
    {
        var added = AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var found = _service.GetEventById(added.Id);

        Assert.NotNull(found);
        Assert.Equal(added.Id, found.Id);
        Assert.Equal("Встреча", found.Title);
    }

    [Fact]
    public void UpdateEvent_ExistingId_UpdatesFieldsAndReturnsTrue()
    {
        var added = AddEvent("Старое название", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        var newData = CreateEvent("Новое название", new DateTime(2026, 7, 11, 12, 0, 0), new DateTime(2026, 7, 11, 13, 0, 0));
        newData.Description = "Обновлённое описание";

        var updated = _service.UpdateEvent(added.Id, newData);

        Assert.True(updated);
        var stored = _service.GetEventById(added.Id);
        Assert.NotNull(stored);
        Assert.Equal("Новое название", stored.Title);
        Assert.Equal("Обновлённое описание", stored.Description);
        Assert.Equal(new DateTime(2026, 7, 11, 12, 0, 0), stored.StartAt);
        Assert.Equal(new DateTime(2026, 7, 11, 13, 0, 0), stored.EndAt);
    }

    [Fact]
    public void DeleteEvent_ExistingId_RemovesEventAndReturnsTrue()
    {
        var added = AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var deleted = _service.DeleteEvent(added.Id);

        Assert.True(deleted);
        Assert.Null(_service.GetEventById(added.Id));
        Assert.Empty(GetAll().Items);
    }

    [Fact]
    public void GetEvents_FilterByTitle_IsCaseInsensitiveAndMatchesPartially()
    {
        AddEvent("Встреча с командой", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        AddEvent("встреча с заказчиком", new DateTime(2026, 7, 20, 15, 0, 0), new DateTime(2026, 7, 20, 16, 0, 0));
        AddEvent("Отпуск", new DateTime(2026, 8, 1, 0, 0, 0), new DateTime(2026, 8, 15, 0, 0, 0));

        var result = _service.GetEvents("ВСТРЕЧА", null, null, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, e => Assert.Contains("встреча", e.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetEvents_FilterByFrom_ReturnsEventsStartingAtOrAfterDate()
    {
        AddEvent("Раннее", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        AddEvent("Граничное", new DateTime(2026, 7, 15, 0, 0, 0), new DateTime(2026, 7, 15, 1, 0, 0));
        AddEvent("Позднее", new DateTime(2026, 7, 20, 9, 0, 0), new DateTime(2026, 7, 20, 10, 0, 0));

        var result = _service.GetEvents(null, new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Раннее");
    }

    [Fact]
    public void GetEvents_FilterByTo_ReturnsEventsEndingAtOrBeforeDate()
    {
        AddEvent("Раннее", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        AddEvent("Граничное", new DateTime(2026, 7, 14, 23, 0, 0), new DateTime(2026, 7, 15, 0, 0, 0));
        AddEvent("Позднее", new DateTime(2026, 7, 20, 9, 0, 0), new DateTime(2026, 7, 20, 10, 0, 0));

        var result = _service.GetEvents(null, null, new DateTime(2026, 7, 15, 0, 0, 0), 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Позднее");
    }

    [Fact]
    public void GetEvents_Pagination_ReturnsRequestedPageAndTotalCount()
    {
        for (var day = 1; day <= 12; day++)
        {
            AddEvent($"Событие {day:00}", new DateTime(2026, 7, day, 9, 0, 0), new DateTime(2026, 7, day, 10, 0, 0));
        }

        var result = _service.GetEvents(null, null, null, 2, 5);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal("Событие 06", result.Items.First().Title);
        Assert.Equal("Событие 10", result.Items.Last().Title);
    }

    [Fact]
    public void GetEvents_PageBeyondRange_ReturnsEmptyItemsButKeepsTotalCount()
    {
        AddEvent("Единственное", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var result = _service.GetEvents(null, null, null, 99, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void GetEvents_CombinedFilters_AppliesAllTogether()
    {
        AddEvent("Встреча с командой", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        AddEvent("встреча с заказчиком", new DateTime(2026, 7, 20, 15, 0, 0), new DateTime(2026, 7, 20, 16, 0, 0));
        AddEvent("Встреча выпускников", new DateTime(2026, 8, 5, 18, 0, 0), new DateTime(2026, 8, 5, 21, 0, 0));
        AddEvent("Отпуск", new DateTime(2026, 7, 21, 0, 0, 0), new DateTime(2026, 7, 25, 0, 0, 0));

        var result = _service.GetEvents(
            "встреча",
            new DateTime(2026, 7, 15, 0, 0, 0),
            new DateTime(2026, 7, 31, 0, 0, 0),
            1,
            100);

        var found = Assert.Single(result.Items);
        Assert.Equal("встреча с заказчиком", found.Title);
        Assert.Equal(1, result.TotalCount);
    }

    // ----- Неуспешные сценарии -----

    [Fact]
    public void GetEventById_UnknownId_ReturnsNull()
    {
        AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var found = _service.GetEventById(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void UpdateEvent_UnknownId_ReturnsFalse()
    {
        var newData = CreateEvent("Новое название", new DateTime(2026, 7, 11, 12, 0, 0), new DateTime(2026, 7, 11, 13, 0, 0));

        var updated = _service.UpdateEvent(Guid.NewGuid(), newData);

        Assert.False(updated);
    }

    [Fact]
    public void DeleteEvent_UnknownId_ReturnsFalse()
    {
        AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var deleted = _service.DeleteEvent(Guid.NewGuid());

        Assert.False(deleted);
        Assert.Single(GetAll().Items);
    }
}
