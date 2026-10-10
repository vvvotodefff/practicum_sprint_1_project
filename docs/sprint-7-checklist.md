# Итоговая проверка седьмого спринта

Дата технической проверки: 2026-10-10 (UTC). Ветка: `sprint-7`.
Источник требований: сохранённое задание `sprint-7.mhtml`, этапы 7–8 и чек-лист ревью.

Итог: все технические критические требования выполнены. Полная пересборка —
0 ошибок, 0 предупреждений; 206 тестов прошли, без пропусков.
Это проверка перед итоговым commit/push, а не результат ревью преподавателя.

## Условия допуска к проверке

| Требование | Результат и подтверждение |
|---|---|
| Репозиторий доступен | [GitHub](https://github.com/vvvotodefff/practicum_sprint_1_project): анонимный GitHub API вернул `private: false` |
| Проект собирается | `dotnet build ProjectWork.slnx --no-restore -t:Rebuild` — 0 ошибок, 0 предупреждений |
| Проект запускается | `dotnet run --no-build --no-launch-profile --project ProjectWork.csproj -- --urls http://127.0.0.1:5261` — запуск успешен; Swagger и запросы работают |
| Тесты запускаются и проходят | `dotnet test ProjectWork.slnx --no-build --no-restore` — 112 + 81 + 13 = 206, неудачных/пропущенных нет |
| README обновлён | Назначение слоёв, таблица ProjectReference, запуск, миграции и три тестовых проекта описаны в корневом README |

## Критические требования

| Требование | Проверено |
|---|---|
| Четыре отдельные производственные сборки | `ProjectWork.Domain`, `ProjectWork.Application`, `ProjectWork.Infrastructure`, `ProjectWork` (Presentation); ещё три проекта — тестовые |
| Сущности и исключения в Domain | `Event`, `Booking`, `BookingStatus`, `DomainValidationException`, `NotFoundException`, `NoAvailableSeatsException` |
| Бизнес-логика в Application/Domain | Сервисы событий и бронирований в Application; инварианты сущностей в Domain |
| Порты в Application | `Application/Abstractions/Repositories/IEventRepository`, `IBookingRepository` |
| Реализации в Infrastructure | Контекст, Fluent API, миграции, EF-репозитории и фоновый сервис находятся в Infrastructure |
| Application не зависит от Infrastructure | Единственная ссылка на проект — Domain; архитектурный тест проверяет ссылки скомпилированной сборки |
| Composition root в Presentation | `Program.cs` вызывает `AddApplicationServices`, `AddInfrastructureServices`, `AddPresentationServices` |
| Контроллеры тонкие | Только приём параметров, вызовы сервисов, DTO и HTTP-результаты; нет DbContext, EF-запросов и правил резервирования/статусов |
| Все тесты проходят | 112 тестов ядра/архитектуры, 81 интеграционный, 13 Presentation |
| Новая структура описана | Таблица зависимостей и структура каталогов в README |

## Хорошие практики и дополнительные критерии

| Критерий | Подтверждение |
|---|---|
| Extension-методы для DI | Есть отдельная регистрация Application, Infrastructure и Presentation |
| Domain без сторонних фреймворков | Нет `ProjectReference`, `PackageReference`, `FrameworkReference`; используются только .NET и собственные типы |
| Тесты зависят от проверяемых слоёв | Общие и PostgreSQL-тесты ссылаются на Domain/Application/Infrastructure, их `.deps.json` не содержит веб-сборку. Только тесты контроллеров/middleware ссылаются на Presentation |
| Однозначные пространства имён | `ProjectWork.Domain.*`, `ProjectWork.Application.*`, `ProjectWork.Infrastructure.*`; HTTP-слой сохраняет `ProjectWork.Controllers` и `ProjectWork.Middleware` |
| Направление зависимостей | Задано ссылками проектов; четыре `ArchitectureTests` защищают границы Domain, Application, Infrastructure и основных тестов |
| История по этапам | Domain: `d5bfe6c`; Application: `1def8b6`; Infrastructure: `099a5c1`; Presentation: `9e09875`; Tests: `38cd2f3` |
| Нет инфраструктуры в ядре | В Domain/Application нет EF Core, ASP.NET Core, DbContext или ссылок на Infrastructure. DI Abstractions в Application используется только для регистрации сервисов |
| Чистая сборка | Добавлены XML-комментарии и исправлен вызов Assert.Single; предупреждения не отключались |

## Проверка API и Swagger

`scripts/Verify-Api.ps1` успешно проверил все семь операций, Swagger JSON/UI,
автоматическую валидацию, ответы 400/404/409 и `application/problem+json`,
CRUD, фильтры и пагинацию. Из 20 параллельных бронирований на 5 мест:
5 ответов 202 и 15 ответов 409. Проверены сохранение данных после перезапуска,
обработка Pending → Confirmed и каскадное удаление.

Отдельно через настоящий Swagger UI в headless Chrome нажаты `Try it out` и `Execute`:

1. `POST /events` → 201, Location и DTO события; свободно 3 места.
2. `POST /events/{id}/book` → 202, Location и статус Pending.
3. `GET /events/{id}` → 200, свободно 2 места.
4. `GET /bookings/{id}` → 200; при повторном чтении Confirmed и заполненный ProcessedAt.

DTO не содержат навигационных свойств. Ошибок JavaScript не зафиксировано.
Созданные проверками события и брони удалены; временные API и браузер закрыты.

## Миграции и ограничения

Команды с `--project ProjectWork.Infrastructure/ProjectWork.Infrastructure.csproj`
и `--startup-project ProjectWork.csproj` успешно обнаруживают
`20261006154025_InitialCreate`. `has-pending-model-changes` не обнаружил изменений.
Схема и идентификатор миграции сохранены, рабочая база не пересоздавалась.
Интеграционные тесты пересоздают только свою временную PostgreSQL-базу Testcontainers.

Локальный `dotnet-ef` 10.0.9 предупреждает о более новых пакетах EF Core 10.0.12,
но обе проверочные команды завершились успешно. Команда обновления инструмента
приведена в README; глобальные инструменты пользователя не изменялись.

Учебный общий SemaphoreSlim защищает только один экземпляр API.
Межпроцессная синхронизация на уровне БД — отдельная будущая задача,
а не требование архитектурного рефакторинга этого спринта.

## Что остаётся после этапа 8

Этап 9 выполняется отдельно: создать PR из `sprint-7` в `main` и отправить ссылку
на проверку в Практикуме. Проверки выше не заменяют одобрение ревьюера;
PR и слияние в main в рамках этапов 7–8 не выполняются.
