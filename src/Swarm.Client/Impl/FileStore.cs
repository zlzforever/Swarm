using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Swarm.Client.Impl
{
    /// <summary>
    /// 用于保存当前节点在执行的进程信息, 需要作为单例使用
    /// 在程序启动时扫描所有进程并判断是否为 Swarm 进程, 如果不是则删除
    /// TODO： 现在的判断是否 ProcessName 相同
    /// </summary>
    public class FileStore : IProcessStore
    {
        private readonly string _folder;
        //   private readonly Dictionary<ProcessKey, JobProcess> _dic = new Dictionary<ProcessKey, JobProcess>();

        public static FileStore Instance = new FileStore();

        private FileStore()
        {
            _folder = Path.Combine(AppContext.BaseDirectory, "process");
            InitStorage();
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool Exists(ProcessKey key)
        {
            var path = Path.Combine(_folder, key.ToString());
            return File.Exists(path);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public int Count(string jobId)
        {
            var files = Directory.GetFiles(_folder);
            return files.Count(f => f.StartsWith(jobId));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<JobProcess> GetProcesses(string jobId)
        {
            var files = Directory.GetFiles(_folder);
            return files.Where(f => f.StartsWith(jobId))
                .Select(f => JsonConvert.DeserializeObject<JobProcess>(File.ReadAllText(f))).ToList();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public List<JobProcess> GetProcesses()
        {
            var files = Directory.GetFiles(_folder);
            return files.Select(f => JsonConvert.DeserializeObject<JobProcess>(File.ReadAllText(f))).ToList();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public JobProcess GetProcess(ProcessKey key)
        {
            var path = Path.Combine(_folder, key.ToString());
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<JobProcess>(File.ReadAllText(path));
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(JobProcess jobProcess)
        {
            if (string.IsNullOrWhiteSpace(jobProcess.TraceId))
            {
                throw new ArgumentNullException(nameof(jobProcess.TraceId));
            }

            if (string.IsNullOrWhiteSpace(jobProcess.JobId))
            {
                throw new ArgumentNullException(nameof(jobProcess.JobId));
            }

            if (jobProcess.Sharding <= 0)
            {
                throw new ArgumentException($"{nameof(jobProcess.Sharding)} should larger than 0");
            }

            if (jobProcess.ProcessId <= 0)
            {
                throw new ArgumentException($"{nameof(jobProcess.ProcessId)} should larger than 0");
            }

            if (string.IsNullOrWhiteSpace(jobProcess.Application))
            {
                throw new ArgumentNullException(nameof(jobProcess.Application));
            }

            var key = new ProcessKey(jobProcess.JobId, jobProcess.TraceId, jobProcess.Sharding);
            var path = Path.Combine(_folder, key.ToString());
            File.WriteAllText(path, JsonConvert.SerializeObject(jobProcess));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Remove(ProcessKey key)
        {
            var path = Path.Combine(_folder, key.ToString());
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private void InitStorage()
        {
            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
            }

            var files = Directory.GetFiles(_folder);
            foreach (var file in files)
            {
                var process = JsonConvert.DeserializeObject<JobProcess>(File.ReadAllText(file));

                try
                {
                    var sysProcess = Process.GetProcessById(process.ProcessId);
                    if (sysProcess.ProcessName != $"{process.Application} {process.Arguments}")
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    File.Delete(file);
                }
            }
        }
    }
}