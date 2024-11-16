using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

List<Book> books = new List<Book>()
{
    new Book("Book 1", "Music", 201),
    new Book("Book 2", "StandUp", 200),
    new Book("Book 3", "Dance", 222),
    new Book("Book 4", "Music", 300),
    new Book("Book 5", "Music", 400)
};

app.UseMiddleware<FreeRoutingMiddleware>(books);
app.UseMiddleware<TokenMiddleware>("token12345",books);

app.Run(async (context) =>
{
    context.Response.StatusCode = 400;
    await context.Response.WriteAsync("page not found");
});

app.Run();


public class FreeRoutingMiddleware
{
    readonly IEnumerable<Book> books;
    readonly RequestDelegate next;

    public FreeRoutingMiddleware(IEnumerable<Book> books, RequestDelegate next)
    {
        this.books = books;
        this.next = next;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        string path = context.Request.Path;
        if(path == "/")
        {
            await context.Response.WriteAsync("hello, it's Welcome Page!");
        }
        else if(path == "/allbooks")
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Helpers.GenerateHtmlPage(Helpers.BuildHtmlTable(books), "all Books"));
        }
        else
        {
            await next.Invoke(context);
        }
    }
}


public class TokenMiddleware
{
    readonly IEnumerable<Book> books;
    private readonly RequestDelegate next;
    string pattern;

    public TokenMiddleware(IEnumerable<Book> books, RequestDelegate next, string pattern)
    {
        this.books = books;
        this.next = next;
        this.pattern = pattern;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if(context.Request.Path == "/getBooks")
        {
            var token = context.Request.Query["token"];
            if(token != pattern)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("token is invalid");
            }
            else
            {
                var booksCategory = context.Request.Query["category"];
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(Helpers.GenerateHtmlPage(
                    Helpers.BuildHtmlTable(books.Where(e => e.Category.Equals(booksCategory, StringComparison.OrdinalIgnoreCase))),
                    $"Category: {booksCategory}"
                    ));
            }
        }
        else
        {
            await next.Invoke(context);
        }
    }
}

public record Book(string Name, string Category, decimal Price);


public static class Helpers
{
    public static string GenerateHtmlPage(string body, string header)
    {
        string html = $"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8" />
            <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0-alpha3/dist/css/bootstrap.min.css" rel="stylesheet" 
            integrity="sha384-KK94CHFLLe+nY2dmCWGMq91rCGa5gtU4mk92HdvYe+M/SXH301p5ILy+dN9+nJOZ" crossorigin="anonymous">
            <title>{header}</title>
        </head>
        <body>
        <div class="container">
        <h2 class="d-flex justify-content-center">{header}</h2>
        <div class="mt-5">
        <a href="/Html/addUsers.html" class="btn btn-primary">Add User<a>
        </div>
        {body}
            <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0-alpha3/dist/js/bootstrap.bundle.min.js" 
            integrity="sha384-ENjdO4Dr2bkBIFxQpeoTz1HIcje39Wm4jDKdf19U8gI4ddQ3GYNS7NTKfAdVQSZe" crossorigin="anonymous"></script>
        </div>
        </body>
        </html>
        """;
        return html;
    }

    public static string BuildHtmlTable<T>(IEnumerable<T> collection)
    {
        StringBuilder tableHtml = new StringBuilder();
        tableHtml.Append("<table class=\"table\">");
        PropertyInfo[] properties = typeof(T).GetProperties();

        tableHtml.Append("<tr>");
        foreach (PropertyInfo property in properties)
        {
            tableHtml.Append($"<th>{property.Name}</th>");
        }
        tableHtml.Append("<th> Action </th>");

        tableHtml.Append("</tr>");
        foreach (T item in collection)
        {
            tableHtml.Append("<tr>");
            foreach (PropertyInfo property in properties)
            {
                object value = property.GetValue(item);
                tableHtml.Append($"<td>{value}</td>");
            }
            tableHtml.Append("</tr>");
        }

        tableHtml.Append("</table>");
        return tableHtml.ToString();
    }
}