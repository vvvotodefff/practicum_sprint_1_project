using ProjectWork.Models;

namespace ProjectWork.Services;

/// <summary>
/// Фоновый сервис: периодически опрашивает хранилище и обрабатывает брони,
/// ожидающие подтверждения
/// </summary>
public class BookingProcessingService : BackgroundService
{
    // Имитация обращения к внешней системе
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    // Пауза между опросами хранилища
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IBookingService _bookingService;
    private readonly ILogger<BookingProcessingService> _logger;

    /// <summary>
    /// Создаёт фоновый сервис обработки бронирований
    /// </summary>
    public BookingProcessingService(IBookingService bookingService,
        ILogger<BookingProcessingService> logger)
    {
        _bookingService = bookingService;
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
                foreach (var booking in _bookingService.GetPendingBookings())
                {
                    await Task.Delay(ProcessingDelay, stoppingToken);

                    _bookingService.MarkAsProcessed(booking.Id, BookingStatus.Confirmed);

                    _logger.LogInformation("Бронь {BookingId} переведена в статус {Status}",
                        booking.Id, BookingStatus.Confirmed);
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
}
