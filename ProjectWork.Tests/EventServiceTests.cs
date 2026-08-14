using System.ComponentModel.DataAnnotations;
using ProjectWork.DTO;
using ProjectWork.Exceptions;
using ProjectWork.Models;
using ProjectWork.Services;

namespace ProjectWork.Tests;

public class EventServiceTests
{
    private readonly EventService _service = new();

    private static CreateEvent NewRequest(string title, DateTime startAt, DateTime endAt, int totalSeats = 100) => new()
    {
        Title = title,
        StartAt = startAt,
        EndAt = endAt,
        TotalSeats = totalSeats
    };

    private static UpdateEvent NewUpdate(string title, DateTime startAt, DateTime endAt, int totalSeats = 100) => new()
    {
        Title = title,
        StartAt = startAt,
        EndAt = endAt,
        TotalSeats = totalSeats
    };

    private async Task<EventInfo> AddEvent(string title, DateTime startAt, DateTime endAt, int totalSeats = 100)
    {
        return await _service.CreateEventAsync(NewRequest(title, startAt, endAt, totalSeats));
    }

    private PaginatedResult<Event> GetAll() => _service.GetEvents(null, null, null, 1, 100);

    // ----- Успешные сценарии -----

    [Fact]
    public async Task CreateEventAsync_AssignsNewIdAndStoresEvent()
    {
        var created = await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        Assert.NotEqual(Guid.Empty, created.Id);
        var stored = Assert.Single(GetAll().Items);
        Assert.Equal("Встреча", stored.Title);
    }

    [Fact]
    public async Task CreateEventAsync_MakesAllSeatsAvailable()
    {
        var created = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 150);

        Assert.Equal(150, created.TotalSeats);
        Assert.Equal(150, created.AvailableSeats);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task CreateEventAsync_NonPositiveTotalSeats_ThrowsValidationException(int totalSeats)
    {
        var request = NewRequest("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), totalSeats);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateEventAsync(request));
        Assert.Empty(GetAll().Items);
    }

    [Fact]
    public async Task CreateEventAsync_EndAtBeforeStartAt_ThrowsValidationException()
    {
        var request = NewRequest("Концерт", new DateTime(2026, 9, 1, 22, 0, 0), new DateTime(2026, 9, 1, 19, 0, 0));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateEventAsync(request));
    }

    [Fact]
    public async Task GetEvents_ReturnsAllEvents()
    {
        await AddEvent("Первое", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        await AddEvent("Второе", new DateTime(2026, 7, 2, 9, 0, 0), new DateTime(2026, 7, 2, 10, 0, 0));
        await AddEvent("Третье", new DateTime(2026, 7, 3, 9, 0, 0), new DateTime(2026, 7, 3, 10, 0, 0));

        var result = GetAll();

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count);
    }

    [Fact]
    public async Task GetEventById_ExistingId_ReturnsEvent()
    {
        var added = await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var found = _service.GetEventById(added.Id);

        Assert.NotNull(found);
        Assert.Equal(added.Id, found.Id);
        Assert.Equal("Встреча", found.Title);
    }

    [Fact]
    public async Task UpdateEvent_ExistingId_UpdatesFieldsAndReturnsTrue()
    {
        var added = await AddEvent("Старое название", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        var newData = NewUpdate("Новое название", new DateTime(2026, 7, 11, 12, 0, 0), new DateTime(2026, 7, 11, 13, 0, 0));
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
    public async Task DeleteEvent_ExistingId_RemovesEventAndReturnsTrue()
    {
        var added = await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var deleted = _service.DeleteEvent(added.Id);

        Assert.True(deleted);
        Assert.Null(_service.GetEventById(added.Id));
        Assert.Empty(GetAll().Items);
    }

    [Fact]
    public async Task GetEvents_FilterByTitle_IsCaseInsensitiveAndMatchesPartially()
    {
        await AddEvent("Встреча с командой", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        await AddEvent("встреча с заказчиком", new DateTime(2026, 7, 20, 15, 0, 0), new DateTime(2026, 7, 20, 16, 0, 0));
        await AddEvent("Отпуск", new DateTime(2026, 8, 1, 0, 0, 0), new DateTime(2026, 8, 15, 0, 0, 0));

        var result = _service.GetEvents("ВСТРЕЧА", null, null, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, e => Assert.Contains("встреча", e.Title, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetEvents_FilterByFrom_ReturnsEventsStartingAtOrAfterDate()
    {
        await AddEvent("Раннее", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        await AddEvent("Граничное", new DateTime(2026, 7, 15, 0, 0, 0), new DateTime(2026, 7, 15, 1, 0, 0));
        await AddEvent("Позднее", new DateTime(2026, 7, 20, 9, 0, 0), new DateTime(2026, 7, 20, 10, 0, 0));

        var result = _service.GetEvents(null, new DateTime(2026, 7, 15, 0, 0, 0), null, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Раннее");
    }

    [Fact]
    public async Task GetEvents_FilterByTo_ReturnsEventsEndingAtOrBeforeDate()
    {
        await AddEvent("Раннее", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        await AddEvent("Граничное", new DateTime(2026, 7, 14, 23, 0, 0), new DateTime(2026, 7, 15, 0, 0, 0));
        await AddEvent("Позднее", new DateTime(2026, 7, 20, 9, 0, 0), new DateTime(2026, 7, 20, 10, 0, 0));

        var result = _service.GetEvents(null, null, new DateTime(2026, 7, 15, 0, 0, 0), 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.DoesNotContain(result.Items, e => e.Title == "Позднее");
    }

    [Fact]
    public async Task GetEvents_Pagination_ReturnsRequestedPageAndTotalCount()
    {
        for (var day = 1; day <= 12; day++)
        {
            await AddEvent($"Событие {day:00}", new DateTime(2026, 7, day, 9, 0, 0), new DateTime(2026, 7, day, 10, 0, 0));
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
    public async Task GetEvents_PageBeyondRange_ReturnsEmptyItemsButKeepsTotalCount()
    {
        await AddEvent("Единственное", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var result = _service.GetEvents(null, null, null, 99, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEvents_CombinedFilters_AppliesAllTogether()
    {
        await AddEvent("Встреча с командой", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        await AddEvent("встреча с заказчиком", new DateTime(2026, 7, 20, 15, 0, 0), new DateTime(2026, 7, 20, 16, 0, 0));
        await AddEvent("Встреча выпускников", new DateTime(2026, 8, 5, 18, 0, 0), new DateTime(2026, 8, 5, 21, 0, 0));
        await AddEvent("Отпуск", new DateTime(2026, 7, 21, 0, 0, 0), new DateTime(2026, 7, 25, 0, 0, 0));

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
    public async Task GetEventById_UnknownId_ReturnsNull()
    {
        await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var found = _service.GetEventById(Guid.NewGuid());

        Assert.Null(found);
    }

    [Fact]
    public void UpdateEvent_UnknownId_ReturnsFalse()
    {
        var newData = NewUpdate("Новое название", new DateTime(2026, 7, 11, 12, 0, 0), new DateTime(2026, 7, 11, 13, 0, 0));

        var updated = _service.UpdateEvent(Guid.NewGuid(), newData);

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdateEvent_ChangingTotalSeats_KeepsOccupiedSeats()
    {
        var added = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 100);
        _service.TryReserveSeats(added.Id, 40);

        // Зал расширили до 150 — занятые 40 мест должны сохраниться
        var updated = _service.UpdateEvent(added.Id,
            NewUpdate("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 150));

        Assert.True(updated);
        var stored = _service.GetEventById(added.Id);
        Assert.NotNull(stored);
        Assert.Equal(150, stored.TotalSeats);
        Assert.Equal(110, stored.AvailableSeats);
    }

    [Fact]
    public async Task UpdateEvent_TotalSeatsBelowOccupied_ThrowsValidationException()
    {
        var added = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 10);
        _service.TryReserveSeats(added.Id, 6);

        Assert.Throws<ValidationException>(() => _service.UpdateEvent(added.Id,
            NewUpdate("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 5)));

        // Событие осталось нетронутым
        var stored = _service.GetEventById(added.Id);
        Assert.NotNull(stored);
        Assert.Equal(10, stored.TotalSeats);
        Assert.Equal(4, stored.AvailableSeats);
    }

    [Fact]
    public async Task UpdateEvent_DoesNotResetOccupiedSeats()
    {
        var added = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 10);
        _service.TryReserveSeats(added.Id, 3);

        // Клиент присылает те же 10 мест — свободных всё равно должно остаться 7
        _service.UpdateEvent(added.Id,
            NewUpdate("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 10));

        Assert.Equal(7, _service.GetEventById(added.Id)!.AvailableSeats);
    }

    [Fact]
    public async Task DeleteEvent_UnknownId_ReturnsFalse()
    {
        await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var deleted = _service.DeleteEvent(Guid.NewGuid());

        Assert.False(deleted);
        Assert.Single(GetAll().Items);
    }

    // ----- Граничные случаи (edge cases) -----

    [Fact]
    public void GetEvents_EmptyStorage_ReturnsEmptyResult()
    {
        var result = GetAll();

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetEvents_EmptyOrWhitespaceTitle_IgnoresFilter(string title)
    {
        await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));
        await AddEvent("Отпуск", new DateTime(2026, 8, 1, 0, 0, 0), new DateTime(2026, 8, 15, 0, 0, 0));

        var result = _service.GetEvents(title, null, null, 1, 100);

        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetEvents_TitleWithoutMatches_ReturnsEmptyResult()
    {
        await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var result = _service.GetEvents("абракадабра", null, null, 1, 100);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEvents_EventExactlyMatchingFromAndTo_IsIncluded()
    {
        var startAt = new DateTime(2026, 7, 10, 9, 0, 0);
        var endAt = new DateTime(2026, 7, 10, 10, 0, 0);
        await AddEvent("Граничное", startAt, endAt);

        // Границы диапазона совпадают с датами события — сравнение нестрогое
        var result = _service.GetEvents(null, startAt, endAt, 1, 100);

        var found = Assert.Single(result.Items);
        Assert.Equal("Граничное", found.Title);
    }

    [Fact]
    public async Task GetEvents_FromGreaterThanTo_ReturnsEmptyResult()
    {
        await AddEvent("Встреча", new DateTime(2026, 7, 10, 9, 0, 0), new DateTime(2026, 7, 10, 10, 0, 0));

        var result = _service.GetEvents(null, new DateTime(2026, 8, 1, 0, 0, 0), new DateTime(2026, 7, 1, 0, 0, 0), 1, 100);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetEvents_LastPage_ReturnsOnlyRemainingItems()
    {
        for (var day = 1; day <= 12; day++)
        {
            await AddEvent($"Событие {day:00}", new DateTime(2026, 7, day, 9, 0, 0), new DateTime(2026, 7, day, 10, 0, 0));
        }

        var result = _service.GetEvents(null, null, null, 3, 5);

        Assert.Equal(12, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Событие 11", result.Items.First().Title);
        Assert.Equal("Событие 12", result.Items.Last().Title);
    }

    [Fact]
    public async Task GetEvents_PageSizeLargerThanTotal_ReturnsAllItems()
    {
        await AddEvent("Первое", new DateTime(2026, 7, 1, 9, 0, 0), new DateTime(2026, 7, 1, 10, 0, 0));
        await AddEvent("Второе", new DateTime(2026, 7, 2, 9, 0, 0), new DateTime(2026, 7, 2, 10, 0, 0));

        var result = _service.GetEvents(null, null, null, 1, 100);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    // ----- Места на событии -----

    [Fact]
    public void TryReserveSeats_EnoughSeats_DecreasesAvailableSeats()
    {
        var eventItem = Event.Create("Концерт", null, new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 10);

        var reserved = eventItem.TryReserveSeats(3);

        Assert.True(reserved);
        Assert.Equal(7, eventItem.AvailableSeats);
        Assert.Equal(10, eventItem.TotalSeats);
    }

    [Fact]
    public void TryReserveSeats_NotEnoughSeats_ReturnsFalseAndKeepsCounter()
    {
        var eventItem = Event.Create("Концерт", null, new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 2);

        var reserved = eventItem.TryReserveSeats(3);

        Assert.False(reserved);
        Assert.Equal(2, eventItem.AvailableSeats);
    }

    [Fact]
    public async Task Service_TryReserveSeats_DecreasesAvailableSeats()
    {
        var created = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 5);

        var reserved = _service.TryReserveSeats(created.Id, 2);

        Assert.True(reserved);
        Assert.Equal(3, _service.GetEventById(created.Id)!.AvailableSeats);
    }

    [Fact]
    public async Task Service_TryReserveSeats_NotEnoughSeats_ReturnsFalse()
    {
        var created = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 1);

        Assert.False(_service.TryReserveSeats(created.Id, 2));
        Assert.Equal(1, _service.GetEventById(created.Id)!.AvailableSeats);
    }

    [Fact]
    public void Service_TryReserveSeats_UnknownEvent_ThrowsNotFound()
    {
        Assert.Throws<NotFoundException>(() => _service.TryReserveSeats(Guid.NewGuid()));
    }

    [Fact]
    public async Task Service_ReleaseSeats_ReturnsSeatToPool()
    {
        var created = await AddEvent("Концерт", new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 5);
        _service.TryReserveSeats(created.Id, 3);

        _service.ReleaseSeats(created.Id, 2);

        Assert.Equal(4, _service.GetEventById(created.Id)!.AvailableSeats);
    }

    [Fact]
    public void Service_ReleaseSeats_UnknownEvent_DoesNothing()
    {
        // Событие могли удалить — компенсация не должна падать
        _service.ReleaseSeats(Guid.NewGuid());
    }

    [Fact]
    public void ReleaseSeats_ReturnsSeatsBackWithoutExceedingTotal()
    {
        var eventItem = Event.Create("Концерт", null, new DateTime(2026, 9, 1, 19, 0, 0), new DateTime(2026, 9, 1, 22, 0, 0), 5);
        eventItem.TryReserveSeats(2);

        eventItem.ReleaseSeats();
        Assert.Equal(4, eventItem.AvailableSeats);

        eventItem.ReleaseSeats(10);
        Assert.Equal(5, eventItem.AvailableSeats);
    }
}
