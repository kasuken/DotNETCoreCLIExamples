using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleHero
{
    class Program
    {
        private static InvocationContext _invocationContext;
        private static ConsoleRenderer _consoleRenderer;

        static async Task<int> Main(InvocationContext invocationContext, string[] args)
        {
            _invocationContext = invocationContext;
            _consoleRenderer = new ConsoleRenderer(
              invocationContext.Console,
              mode: invocationContext.BindingContext.OutputMode(),
              resetAfterRender: true);

            var cmd = new RootCommand();
            cmd.AddCommand(helloWorld());
            cmd.AddCommand(helloWorldWithName());
            cmd.AddCommand(helloWorldWithSwitch());
            cmd.AddCommand(richList());
            return await cmd.InvokeAsync(args);
        }

        private static Command helloWorld()
        {
            var cmd = new Command("greeting", "Shows a greeting to the World");
            cmd.Handler = CommandHandler.Create(() => {
                Console.WriteLine("Hello world!!!");
            });
            return cmd;
        }

        private static Command helloWorldWithName()
        {
            var cmd = new Command("greetingWithName", "Greets the specified person");
            cmd.AddOption(new Option(new[] { "--name", "-n" }, "Name of the person to greet")
            {
                Argument = new Argument<string>
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            });
            cmd.Handler = CommandHandler.Create<string>((name) => {
                Console.WriteLine($"Hello {name}");
            });
            return cmd;
        }

        private static Command helloWorldWithSwitch()
        {
            var cmd = new Command("helloWorldWithSwitch", "Greets the World but with a switch");
            cmd.AddOption(new Option(new[] { "--name", "-n" }, "Name of the person to greet")
            {
                Argument = new Argument<string>
                {
                    Arity = ArgumentArity.ExactlyOne
                }
            });
            cmd.AddOption(new Option("--german", "Show a polite greeting"));
            cmd.Handler = CommandHandler.Create<string, bool>((name, german) => {
                Console.WriteLine($"{(german ? "Hallo" : "Hello")} {name}");
            });
            return cmd;
        }

        private static Command richList()
        {
            List<Post> Posts = new List<Post>();
            var client = new HttpClient();

            var task = client.GetAsync("https://jsonplaceholder.typicode.com/posts")
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  Posts = JsonConvert.DeserializeObject<List<Post>>(jsonString.Result);

              });
            task.Wait();

            var cmd = new Command("list");
            cmd.Handler = CommandHandler.Create(() => {
                var table = new TableView<Post>
                {
                    Items = Posts.Take(20).ToList()                    
                };
                table.AddColumn(user => user.ID, "ID");
                table.AddColumn(user => user.Title, "Title");

                var screen = new ScreenView(_consoleRenderer, _invocationContext.Console) { Child = table };
                screen.Render();
            });
            return cmd;
        }

        class Post
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
        }
    }
}