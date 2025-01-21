using fluxzy.bench.kestrel;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace floody.server
{
    public class Program
    {
        [RequiresUnreferencedCode()]
        [RequiresDynamicCode()]
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            app.MapMethods("/", ["GET", "POST", "PUT", "PATCH", "DELETE"],
                (HttpContext _,
                [FromQuery] int length = 0) => Results.Stream(new FakeReadStream(length)));

            await app.StartAsync();

            // get pid from args 

            await WaitShutdownOrParentToDie(args, app);
        }

        private static async Task WaitShutdownOrParentToDie(string[] args, WebApplication app)
        {
            var haltTasks = new List<Task>()
            {
                app.WaitForShutdownAsync()
            };

            var parentId = args.FirstOrDefault(a => a.StartsWith($"--pid="));

            if (parentId != null)
            {
                var pid = int.Parse(parentId.Split("=")[1]);

                Process parentProcess = Process.GetProcessById(pid);


                haltTasks.Add(parentProcess.WaitForExitAsync());
            }

            await Task.WhenAny(haltTasks);
        }
    }
}
