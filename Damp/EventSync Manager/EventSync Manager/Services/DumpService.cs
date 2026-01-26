using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ICSharpCode.SharpZipLib.Zip;
using EventSync_Manager.Models;
using EventSync_Manager.Services;

namespace EventSync_Manager.Services;

public class DumpService
{
    private readonly DatabaseService _dbService;
    private readonly EncryptionService _encryptionService;
    private const int BatchSize = 1000; // Оптимизация: батчинг для больших объёмов данных

    public DumpService(DatabaseService dbService)
    {
        _dbService = dbService;
        _encryptionService = new EncryptionService();
    }

    public async Task<string> CreateDumpAsync(int organizationId, string outputPath)
    {
        var org = await _dbService.GetOrganizationAsync(organizationId);
        if (org == null)
            throw new ArgumentException($"Организация с ID {organizationId} не найдена");

        var timestamp = DateTime.UtcNow;
        var dumpFileName = $"eventsync_{organizationId}_{timestamp:yyyyMMddHHmmss}.aes";
        var dumpPath = Path.Combine(outputPath, dumpFileName);

        // Создаём временную директорию для сборки дампа
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 1. Оптимизированная выгрузка данных из БД (батчинг)
            var events = await _dbService.GetEventsForOrganizationAsync(organizationId);
            var eventIds = events.Select(e => e.Id).ToList();
            var attachments = await _dbService.GetFileAttachmentsForEventsAsync(eventIds);

            // 2. Формирование JSONL файла (оптимизированная запись)
            var jsonlPath = Path.Combine(tempDir, "dump.jsonl");
            await WriteJsonlFileAsync(jsonlPath, events, attachments);

            // 3. Копирование файлов в папку files/
            var filesDir = Path.Combine(tempDir, "files");
            Directory.CreateDirectory(filesDir);
            await CopyFilesAsync(attachments, filesDir);

            // 4. Создание manifest.json
            var manifest = new DumpManifest
            {
                OrganizationId = organizationId,
                Timestamp = timestamp,
                EventsCount = events.Count,
                FilesCount = attachments.Count
            };

            // Вычисляем контрольную сумму дампа
            var dumpChecksum = await CalculateDumpChecksumAsync(tempDir);
            manifest.Checksum = dumpChecksum;

            var manifestPath = Path.Combine(tempDir, "manifest.json");
            await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            // 5. Создание ZIP архива
            var zipPath = Path.Combine(tempDir, "dump.zip");
            await CreateZipArchiveAsync(tempDir, zipPath, manifest);

            // 6. Шифрование архива AES-256-GCM
            var zipData = await File.ReadAllBytesAsync(zipPath);
            var encryptedData = EncryptionService.EncryptData(zipData, org.EncryptionKey);

            // 7. Цифровая подпись (опционально, если есть RSA ключ)
            // В реальной системе нужно хранить RSA ключи для каждой организации
            // var signature = EncryptionService.SignData(encryptedData, rsaPrivateKey);
            // await File.WriteAllBytesAsync(Path.Combine(tempDir, "signature.dat"), signature);

            // 8. Сохранение финального дампа
            await File.WriteAllBytesAsync(dumpPath, encryptedData);

            return dumpPath;
        }
        finally
        {
            // Очистка временных файлов
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private async Task WriteJsonlFileAsync(string jsonlPath, List<Event> events, List<FileAttachment> attachments)
    {
        await using var writer = new StreamWriter(jsonlPath, false, Encoding.UTF8);

        // Записываем события батчами для оптимизации
        foreach (var batch in events.Chunk(BatchSize))
        {
            foreach (var evt in batch)
            {
                var json = JsonSerializer.Serialize(evt, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await writer.WriteLineAsync(json);
            }
            await writer.FlushAsync(); // Принудительная запись батча
        }

        // Записываем вложения
        foreach (var attachment in attachments)
        {
            var json = JsonSerializer.Serialize(attachment, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await writer.WriteLineAsync(json);
        }
    }

    private async Task CopyFilesAsync(List<FileAttachment> attachments, string targetDir)
    {
        var copyTasks = attachments.Select(async attachment =>
        {
            if (File.Exists(attachment.Filepath))
            {
                var targetPath = Path.Combine(targetDir, attachment.Hash);
                var targetDirPath = Path.GetDirectoryName(targetPath);
                if (targetDirPath != null && !Directory.Exists(targetDirPath))
                {
                    Directory.CreateDirectory(targetDirPath);
                }
                await Task.Run(() => File.Copy(attachment.Filepath, targetPath, true));
            }
        });

        await Task.WhenAll(copyTasks);
    }

    private async Task<string> CalculateDumpChecksumAsync(string dumpDir)
    {
        var files = Directory.GetFiles(dumpDir, "*", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var combinedStream = new MemoryStream();

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(dumpDir, file);
            var pathBytes = Encoding.UTF8.GetBytes(relativePath);
            combinedStream.Write(pathBytes, 0, pathBytes.Length);

            var fileData = await File.ReadAllBytesAsync(file);
            combinedStream.Write(fileData, 0, fileData.Length);
        }

        combinedStream.Position = 0;
        var hash = sha256.ComputeHash(combinedStream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task CreateZipArchiveAsync(string sourceDir, string zipPath, DumpManifest manifest)
    {
        await Task.Run(() =>
        {
            using var zipStream = new FileStream(zipPath, FileMode.Create);
            using var zip = new ZipOutputStream(zipStream);

            zip.SetLevel(6); // Компрессия уровня 6 (баланс скорости и размера)

            var files = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(sourceDir, file);
                var entry = new ZipEntry(relativePath.Replace('\\', '/'))
                {
                    DateTime = File.GetLastWriteTime(file)
                };
                zip.PutNextEntry(entry);

                using var fileStream = File.OpenRead(file);
                fileStream.CopyTo(zip);
                zip.CloseEntry();
            }
        });
    }

    public async Task<DumpData> ImportDumpAsync(string dumpPath, int organizationId)
    {
        var org = await _dbService.GetOrganizationAsync(organizationId);
        if (org == null)
            throw new ArgumentException($"Организация с ID {organizationId} не найдена");

        // 1. Чтение и расшифровка дампа
        var encryptedData = await File.ReadAllBytesAsync(dumpPath);
        byte[] decryptedData;
        try
        {
            decryptedData = EncryptionService.DecryptData(encryptedData, org.EncryptionKey);
        }
        catch
        {
            throw new InvalidOperationException("Не удалось расшифровать дамп. Проверьте ключ шифрования.");
        }

        // 2. Распаковка ZIP
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var zipPath = Path.Combine(tempDir, "dump.zip");
            await File.WriteAllBytesAsync(zipPath, decryptedData);

            await Task.Run(() =>
            {
                using var zipStream = new FileStream(zipPath, FileMode.Open);
                using var zip = new ZipInputStream(zipStream);
                ZipEntry? entry;
                while ((entry = zip.GetNextEntry()) != null)
                {
                    var entryPath = Path.Combine(tempDir, entry.Name);
                    var entryDir = Path.GetDirectoryName(entryPath);
                    if (entryDir != null && !Directory.Exists(entryDir))
                    {
                        Directory.CreateDirectory(entryDir);
                    }

                    if (!entry.IsDirectory)
                    {
                        using var fileStream = File.Create(entryPath);
                        zip.CopyTo(fileStream);
                    }
                }
            });

            // 3. Проверка manifest
            var manifestPath = Path.Combine(tempDir, "manifest.json");
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException("Manifest не найден в дампе");

            var manifestJson = await File.ReadAllTextAsync(manifestPath);
            var manifest = JsonSerializer.Deserialize<DumpManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (manifest == null)
                throw new InvalidOperationException("Не удалось прочитать manifest");

            // 4. Проверка контрольной суммы
            var calculatedChecksum = await CalculateDumpChecksumAsync(tempDir);
            if (calculatedChecksum != manifest.Checksum)
                throw new InvalidOperationException("Контрольная сумма дампа не совпадает. Возможна порча данных.");

            // 5. Чтение JSONL
            var jsonlPath = Path.Combine(tempDir, "dump.jsonl");
            var events = new List<Event>();
            var attachments = new List<FileAttachment>();

            if (File.Exists(jsonlPath))
            {
                var lines = await File.ReadAllLinesAsync(jsonlPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Пытаемся определить тип объекта
                    if (line.Contains("\"eventId\"") || line.Contains("\"event_id\""))
                    {
                        var attachment = JsonSerializer.Deserialize<FileAttachment>(line, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (attachment != null) attachments.Add(attachment);
                    }
                    else
                    {
                        var evt = JsonSerializer.Deserialize<Event>(line, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (evt != null) events.Add(evt);
                    }
                }
            }

            // 6. Копирование файлов
            var filesDir = Path.Combine(tempDir, "files");
            if (Directory.Exists(filesDir))
            {
                // В реальной системе нужно скопировать файлы в постоянное хранилище
            }

            return new DumpData
            {
                Manifest = manifest,
                Events = events,
                Attachments = attachments,
                TempDirectory = tempDir
            };
        }
        catch
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            throw;
        }
    }
}

public class DumpData
{
    public DumpManifest Manifest { get; set; } = null!;
    public List<Event> Events { get; set; } = new();
    public List<FileAttachment> Attachments { get; set; } = new();
    public string TempDirectory { get; set; } = string.Empty;
}

