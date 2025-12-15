using EventSync_Manager.Models;
using EventSync_Manager.Services;

namespace EventSync_Manager.Examples;

/// <summary>
/// Пример использования системы синхронизации EventSync Manager
/// </summary>
public class UsageExample
{
    private const string ConnectionString = "Host=localhost;Database=eventsync;Username=postgres;Password=password";

    public static async Task ExampleExportDump()
    {
        // Инициализация сервисов
        var dbService = new DatabaseService(ConnectionString);
        await dbService.InitializeDatabaseAsync();

        var dumpService = new DumpService(dbService);
        var eventLogService = new EventLogService(ConnectionString);
        var conflictService = new ConflictResolutionService(ConnectionString, eventLogService);
        var syncService = new SyncService(dumpService, conflictService, eventLogService, dbService);

        // Экспорт дампа для организации с ID = 1
        int organizationId = 1;
        string outputPath = @"C:\Dumps";

        var result = await syncService.ExportDumpAsync(
            organizationId,
            outputPath,
            ConflictResolutionService.ConflictResolutionStrategy.LastWriteWins
        );

        if (result.Success)
        {
            Console.WriteLine($"Дамп успешно создан: {result.DumpPath}");
        }
        else
        {
            Console.WriteLine($"Ошибка: {result.ErrorMessage}");
        }
    }

    public static async Task ExampleImportDump()
    {
        // Инициализация сервисов
        var dbService = new DatabaseService(ConnectionString);
        await dbService.InitializeDatabaseAsync();

        var dumpService = new DumpService(dbService);
        var eventLogService = new EventLogService(ConnectionString);
        var conflictService = new ConflictResolutionService(ConnectionString, eventLogService);
        var syncService = new SyncService(dumpService, conflictService, eventLogService, dbService);
        var nonceService = new NonceService(ConnectionString);
        await nonceService.InitializeNonceTableAsync();

        // Импорт дампа
        string dumpPath = @"C:\Dumps\eventsync_1_20241201120000.aes";
        int organizationId = 1;

        // Проверяем nonce перед импортом
        var dumpData = await dumpService.ImportDumpAsync(dumpPath, organizationId);
        var isUsed = await nonceService.IsNonceUsedAsync(dumpData.Manifest.Nonce);

        if (isUsed)
        {
            Console.WriteLine("Этот дамп уже был импортирован ранее!");
            return;
        }

        var result = await syncService.ImportDumpAsync(
            dumpPath,
            organizationId,
            ConflictResolutionService.ConflictResolutionStrategy.LastWriteWins
        );

        if (result.Success)
        {
            // Отмечаем nonce как использованный
            await nonceService.MarkNonceAsUsedAsync(dumpData.Manifest.Nonce, organizationId, dumpPath);

            Console.WriteLine($"Импорт завершён:");
            Console.WriteLine($"  Создано событий: {result.EventsCreated}");
            Console.WriteLine($"  Обновлено событий: {result.EventsUpdated}");
            Console.WriteLine($"  Пропущено событий: {result.EventsSkipped}");
            Console.WriteLine($"  Конфликтов: {result.ConflictsCount}");
        }
        else
        {
            Console.WriteLine($"Ошибка импорта: {result.ErrorMessage}");
        }
    }

    public static async Task ExampleFindUsbDumps()
    {
        var dbService = new DatabaseService(ConnectionString);
        var dumpService = new DumpService(dbService);
        var eventLogService = new EventLogService(ConnectionString);
        var conflictService = new ConflictResolutionService(ConnectionString, eventLogService);
        var syncService = new SyncService(dumpService, conflictService, eventLogService, dbService);

        // Поиск дампов на USB-носителях
        var dumpFiles = await syncService.FindDumpsOnUsbDrivesAsync();

        Console.WriteLine($"Найдено дампов на USB: {dumpFiles.Count}");
        foreach (var dumpFile in dumpFiles)
        {
            Console.WriteLine($"  - {dumpFile}");
        }
    }

    public static async Task ExampleViewEventLogs()
    {
        var eventLogService = new EventLogService(ConnectionString);

        // Получение истории изменений для события
        int eventId = 1;
        var logs = await eventLogService.GetEventHistoryAsync(eventId);

        Console.WriteLine($"История изменений для события {eventId}:");
        foreach (var log in logs)
        {
            Console.WriteLine($"  [{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Action} " +
                            $"от {log.User ?? "system"} ({log.Source})");
            if (!string.IsNullOrEmpty(log.StatusOld) && !string.IsNullOrEmpty(log.StatusNew))
            {
                Console.WriteLine($"    Статус: {log.StatusOld} -> {log.StatusNew}");
            }
            if (!string.IsNullOrEmpty(log.Comment))
            {
                Console.WriteLine($"    Комментарий: {log.Comment}");
            }
        }
    }

    public static async Task ExampleConflictResolution()
    {
        var dbService = new DatabaseService(ConnectionString);
        var eventLogService = new EventLogService(ConnectionString);
        var conflictService = new ConflictResolutionService(ConnectionString, eventLogService);

        // Пример: слияние событий с разными стратегиями
        var incomingEvents = new List<Event>
        {
            new Event
            {
                Id = 1,
                Title = "Тестовое событие",
                Status = "in_progress",
                Version = 2,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Стратегия: последняя запись побеждает
        var result1 = await conflictService.MergeEventsAsync(
            incomingEvents,
            organizationId: 1,
            ConflictResolutionService.ConflictResolutionStrategy.LastWriteWins
        );

        Console.WriteLine($"Стратегия LastWriteWins:");
        Console.WriteLine($"  Создано: {result1.Created}, Обновлено: {result1.Updated}, Конфликтов: {result1.Conflicts.Count}");

        // Стратегия: слияние изменений
        var result2 = await conflictService.MergeEventsAsync(
            incomingEvents,
            organizationId: 1,
            ConflictResolutionService.ConflictResolutionStrategy.Merge
        );

        Console.WriteLine($"Стратегия Merge:");
        Console.WriteLine($"  Создано: {result2.Created}, Обновлено: {result2.Updated}, Конфликтов: {result2.Conflicts.Count}");
    }
}

