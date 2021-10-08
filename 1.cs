using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp7
{
    record WorkInfo(
        int Id,
        string Name,
        int Priority,
        Func<Task<bool>> Job
    ) : IComparable<WorkInfo>
    {
        public int CompareTo(WorkInfo other) => Priority.CompareTo(other.Priority);
    }

    class WorkJob
    {
        public int SucsessCount { get; set; } = 0;

        public int FailedCount { get; set; } = 0;

        public List<int> CompletedIdJobs { get; set; } = new ();
    }

    class Manager
    {
        private readonly int countWorkers;

        private readonly SortedSet<WorkInfo> jobs = new();

        private static readonly object lockObj = new();

        public Manager(int countWorkers)
        {
            this.countWorkers = countWorkers;
        }

        public async Task AddJob(WorkInfo job)
        {
            await Task.Run(() =>
            {
                lock (lockObj)
                {
                    jobs.Add(job);
                }
            });
        }

        public async Task<WorkJob[]> Run()
        {
            var result = Enumerable.Repeat(0, countWorkers).Select(_ => new WorkJob()).ToArray();

            var tasks = result.Select(async (data) =>
            {
                while (jobs.Count > 0)
                {
                    var job = PopJob();
                    if (job == null)
                        break;

                    var isSucsessul = await job.Job();

                    if (isSucsessul)
                        ++data.SucsessCount;
                    else
                        ++data.FailedCount;
                    data.CompletedIdJobs.Add(job.Id);
                }
                return data;
            });

            await Task.WhenAll(tasks);
            return result;
        }

        private WorkInfo PopJob()
        {
            lock (lockObj)
            {
                if (jobs.Count == 0)
                    return null;

                var result = jobs.Last();
                jobs.Remove(result);
                return result;
            }
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var manager = new Manager(10);

            var random = new Random();

            const int countJobs = 50;
            Enumerable.Range(0, countJobs).ToList().ForEach(async (i) =>
            {
                var job = new WorkInfo(
                    i + 1,
                    $"TEST{i + 1}",
                    i + 1,
                    new Func<Task<bool>>(async () =>
                    {
                        await Task.Delay(100);
                        return random.Next(2) == 1;
                    })
                );
                await manager.AddJob(job);
            });

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var result = await manager.Run();
            stopwatch.Stop();

            Console.WriteLine("Ms: " + stopwatch.ElapsedMilliseconds.ToString());
            Console.WriteLine(string.Join(Environment.NewLine,
                result.Select(x => $"{x.SucsessCount}, {x.FailedCount}, [{string.Join(",", x.CompletedIdJobs)}]")));
        }
    }
}
