using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Фоновый сервис: периодически опрашивает хранилище и обрабатывает брони,
/// ожидающие подтверждения. Брони обрабатываются параллельно
/// </summary>
public class BookingProcessingService : BackgroundService
{
    // Имитация обращения к внешней системе
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    // Пауза между опросами хранилища
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IBookingService _bookingService;
    private readonly IEventService _eventService;
    private readonly ILogger<BookingProcessingService> _logger;

    // Пропускает к записи в хранилище только одну задачу за раз
    private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

    /// <summary>
    /// Создаёт фоновый сервис обработки бронирований
    /// </summary>
    public BookingProcessingService(IBookingService bookingService,
        IEventService eventService,
        ILogger<BookingProcessingService> logger)
    {
        _bookingService = bookingService;
        _eventService = eventService;
        _logger = logger;
    }

    /// <summary>
    /// Цикл обработки: работает, пока приложение не получит сигнал остановки
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновая обработка бронирований запущена");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pendingBookings = _bookingService.GetPendingBookings();

                if (pendingBookings.Count > 0)
                {
                    // Все брони обрабатываются одновременно
                    var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
                    await Task.WhenAll(tasks);
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Штатная остановка приложения — выходим из цикла без ошибки
                break;
            }
            catch (Exception ex)
            {
                // Необработанное исключение остановило бы фоновый сервис навсегда,
                // поэтому логируем и продолжаем работу
                _logger.LogError(ex, "Ошибка при обработке бронирований");
                await Task.Delay(PollInterval, CancellationToken.None);
            }
        }

        _logger.LogInformation("Фоновая обработка бронирований остановлена");
    }

    /// <summary>
    /// Обработать одну бронь: дождаться "внешней системы" и записать результат
    /// </summary>
    private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
    {
        try
        {
            // Задержка до захвата семафора: имитации внешнего вызова идут параллельно
            await Task.Delay(ProcessingDelay, stoppingToken);

            await _processingSemaphore.WaitAsync(stoppingToken);
            try
            {
                var eventItem = _eventService.GetEventById(booking.EventId);

                if (eventItem is null)
                {
                    // Событие удалили, пока бронь ждала обработки.
                    // Возвращать место некуда — самого события больше нет
                    booking.Reject();

                    _logger.LogWarning("Бронь {BookingId} отклонена: событие {EventId} не найдено",
                        booking.Id, booking.EventId);
                    return;
                }

                booking.Confirm();

                _logger.LogInformation("Бронь {BookingId} переведена в статус {Status}",
                    booking.Id, booking.Status);
            }
            finally
            {
                _processingSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Приложение останавливается: бронь остаётся Pending
            // и будет обработана при следующем запуске
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось обработать бронь {BookingId}, бронь отклонена", booking.Id);

            await RejectAndReleaseSeatAsync(booking);
        }
    }

    /// <summary>
    /// Освобождает семафор при остановке приложения
    /// </summary>
    public override void Dispose()
    {
        _processingSemaphore.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Компенсация после сбоя: отклонить бронь и вернуть место в пул
    /// </summary>
    private async Task RejectAndReleaseSeatAsync(Booking booking)
    {
        // Токен не передаём: компенсацию нужно выполнить даже при остановке приложения
        await _processingSemaphore.WaitAsync(CancellationToken.None);
        try
        {
            booking.Reject();
            _eventService.ReleaseSeats(booking.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось откатить бронь {BookingId}", booking.Id);
        }
        finally
        {
            _processingSemaphore.Release();
        }
    }
}
