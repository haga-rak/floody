using fluxzy.bench.kestrel;
using Microsoft.AspNetCore.Mvc;

namespace floody.server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            app.MapMethods("/", ["GET", "POST", "PUT", "PATCH", "DELETE"],
                (HttpContext _,
                [FromQuery] int length = 0) => Results.Stream(new FakeReadStream(length)));

            app.Run();
        }
    }
}
