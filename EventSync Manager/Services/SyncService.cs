using EventSync_Manager.Models;
using EventSync_Manager.Services;
using System.IO;

namespace EventSync_Manager.Services;

public class SyncService
{
    private readonly DumpService _dumpService;
    private readonly ConflictResolutionService _conflictService;
    private readonly EventLogService _eventLogService;
    private readonly DatabaseService _dbService;

    public SyncService(
        DumpService dumpService,
        ConflictResolutionService conflictService,
        EventLogService eventLogService,
        DatabaseService dbService)
    {
        _dumpService = dumpService;
        _conflictService = conflictService;
        _eventLogService = eventLogService;
        _dbService = dbService;
    }

    public async Task<SyncResult> ExportDumpAsync(
        int organizationId,
        string outputPath,
        ConflictResolutionService.ConflictResolutionStrategy conflictStrategy = ConflictResolutionService.ConflictResolutionStrategy.LastWriteWins)
    {
        var result = new SyncResult
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Проверяем существование организации
            var org = await _dbService.GetOrganizationAsync(organizationId);
            if (org == null)
            {
                result.ErrorMessage = $"Организация с ID {organizationId} не найдена";
                return result;
            }

            // Создаём дамп
            var dumpPath = await _dumpService.CreateDumpAsync(organizationId, outputPath);
            
            result.Success = true;
            result.DumpPath = dumpPath;
            result.Message = $"Дамп успешно создан: {dumpPath}";

            // Логируем операцию экспорта
            await _eventLogService.LogEventChangeAsync(
                0, // Специальный ID для системных операций
                null,
                "export",
                $"Экспорт дампа для организации {organizationId}",
                "manager",
                "export",
                "system"
            );

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Ошибка при создании дампа: {ex.Message}";
            return result;
        }
    }

    public async Task<SyncResult> ImportDumpAsync(
        string dumpPath,
        int organizationId,
        ConflictResolutionService.ConflictResolutionStrategy conflictStrategy = ConflictResolutionService.ConflictResolutionStrategy.LastWriteWins)
    {
        var result = new SyncResult
        {
            Success = false,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            // Импортируем дамп
            var dumpData = await _dumpService.ImportDumpAsync(dumpPath, organizationId);

            // Проверяем nonce для предотвращения повторного импорта
            // Примечание: для проверки nonce используйте NonceService

            // Разрешаем конфликты и сливаем данные
            var mergeResult = await _conflictService.MergeEventsAsync(
                dumpData.Events,
                organizationId,
                conflictStrategy
            );

            result.Success = true;
            result.EventsCreated = mergeResult.Created;
            result.EventsUpdated = mergeResult.Updated;
            result.EventsSkipped = mergeResult.Skipped;
            result.ConflictsCount = mergeResult.Conflicts.Count;
            result.Message = $"Импортировано: создано {mergeResult.Created}, обновлено {mergeResult.Updated}, пропущено {mergeResult.Skipped}";

            // Логируем операцию импорта
            await _eventLogService.LogEventChangeAsync(
                0,
                null,
                "import",
                $"Импорт дампа для организации {organizationId}. Создано: {mergeResult.Created}, Обновлено: {mergeResult.Updated}",
                "manager",
                "import",
                "system"
            );

            // Сохраняем nonce как использованный
            // Примечание: используйте NonceService.MarkNonceAsUsedAsync() после успешного импорта

            // Очищаем временные файлы
            if (Directory.Exists(dumpData.TempDirectory))
            {
                Directory.Delete(dumpData.TempDirectory, true);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Ошибка при импорте дампа: {ex.Message}";
            return result;
        }
    }

    public async Task<List<string>> FindDumpsOnUsbDrivesAsync()
    {
        var dumpFiles = new List<string>();
        var drives = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
            .ToList();

        foreach (var drive in drives)
        {
            try
            {
                var files = Directory.GetFiles(drive.RootDirectory.FullName, "eventsync_*.aes", SearchOption.AllDirectories);
                dumpFiles.AddRange(files);
            }
            catch
            {
                // Игнорируем ошибки доступа к диску
            }
        }

        return dumpFiles;
    }
}

public class SyncResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DumpPath { get; set; }
    public DateTime Timestamp { get; set; }
    public int EventsCreated { get; set; }
    public int EventsUpdated { get; set; }
    public int EventsSkipped { get; set; }
    public int ConflictsCount { get; set; }
}

