using Newtonsoft.Json;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchClipper.Helpers;
using TwitchClipper.Models;

namespace TwitchClipper.Services
{
    public interface IArchivingService
    {
        Task LoadLogs();
        Task Log(TwitchClipModel model, string clipSavePath, AsyncLock asyncLock);
    }

    public class ArchivingService : IArchivingService
    {
        private List<ArchiveModel> _logs { get; set; }
        private string _path => Path.Combine(Directory.GetCurrentDirectory(), "logs.json");
        private bool _firstRun = true;
        private FileStream _fs;

        public async Task LoadLogs()
        {
            _logs = new List<ArchiveModel>();

            if (File.Exists(_path))
            {
                var json = await File.ReadAllTextAsync(_path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    await File.WriteAllTextAsync(_path, $"[{Environment.NewLine}]");
                }

                try
                {
                    _logs = JsonConvert.DeserializeObject<List<ArchiveModel>>(json);

                    //do this so that just in case anyone messes up the JSON formatting, we save it correctly again
                    if (_logs.Any())
                    {
                        await File.WriteAllTextAsync(_path, JsonConvert.SerializeObject(_logs, Formatting.Indented));

                        _firstRun = false;
                    }
                }
                catch
                {
                    await ErrorHelper.LogAndExit("Seems like the JSON in logs.json is incorrect. Either fix the file's content or delete it. If this is not intentional, please create an issue: https://github.com/mortenmoulder/TwitchClipper/issues");
                }
            }
            else
            {
                await File.WriteAllTextAsync(_path, $"[{Environment.NewLine}]");
            }

            _fs = File.OpenWrite(_path);
        }

        public async Task Log(TwitchClipModel model, string clipSavePath, AsyncLock asyncLock)
        {
            using (await asyncLock.LockAsync())
            {
                if (_logs.Any(x => x.Id == model.Id))
                {
                    return;
                }

                var data = new ArchiveModel
                {
                    Id = model.Id,
                    Broadcaster = model.BroadcasterName,
                    Created = model.CreatedAt,
                    CreatedBy = model.CreatorName,
                    Title = model.Title,
                    Url = model.Url,
                    ClipSavePath = clipSavePath,
                    DownloadedAt = DateTime.Now
                };

                _logs.Add(data);

                await SaveLog(data);
            }
        }

        private async Task SaveLog(ArchiveModel model)
        {
            //write to logs.json file using the current filestream, so we don't have to serialize the data over and over again - this is really, really fast
            var serialized = JsonConvert.SerializeObject(model, Formatting.Indented);
            serialized = string.Join('\n', serialized.Split('\n').Select(line => $"  {line}"));
            var byteContent = Encoding.UTF8.GetBytes(serialized);
            var firstWrite = Encoding.UTF8.GetBytes("," + Environment.NewLine);

            if (_firstRun)
            {
                firstWrite = Encoding.UTF8.GetBytes(Environment.NewLine);
            }

            _fs.Seek(-3, SeekOrigin.End);

            await _fs.WriteAsync(firstWrite, 0, firstWrite.Length);
            await _fs.WriteAsync(byteContent, 0, byteContent.Length);
            await _fs.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine));
            await _fs.WriteAsync(Encoding.UTF8.GetBytes("]"));

            _firstRun = false;
        }
    }
}
