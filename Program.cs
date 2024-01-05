using HtmlAgilityPack;
using DotNetEnv;

namespace newsVideoGenerator
{
    public class Article
    {
        public string? url { get; set; }
        public string? title { get; set; }
        public string? content { get; set; }
    }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            DotNetEnv.Env.Load();

            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);

            List<Article> articleList = new List<Article>();
            foreach (var url in args)
            {
                // Await the asynchronous method call
                Article article = await scrapeArticleAsync(url);
                articleList.Add(article);
            }

            string script = GenerateScript(articleList);

            GenerateAudio(script, id);

            MakeVideo(id);
        }


        static async Task<Article> scrapeArticleAsync(string url)
        {
            Article article = new Article();
            Console.WriteLine($"Scraping {url}");


            article.url = url;


            HttpClient client = new HttpClient();
            string html = await client.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            Console.WriteLine($"{url} source scraped");

            switch (true)
            {
                case var _ when url.Contains("bbc"):
                    Console.WriteLine("BBC article found");
                    article = mineBBC(article, htmlDocument);
                    break;
                default:
                    Console.WriteLine("Source unknown");
                    break;
            }



            return article;
        }

        static Article mineBBC(Article article, HtmlDocument htmlDocument)
        {
            article.title = htmlDocument.DocumentNode.SelectSingleNode("//title").InnerText;
            //Console.WriteLine($"Title: {article.title}");

            var contentNodes = htmlDocument.DocumentNode.SelectNodes("//p[@class='ssrcss-1q0x1qg-Paragraph e1jhz7w10']");
            if (contentNodes != null)
            {
                article.content = string.Join("\n", contentNodes.Select(node => node.InnerText));
                //Console.WriteLine($"Content: {article.content}");
            }
            else
            {
                Console.WriteLine("Content paragraphs not found.");
            }



            return article;
        }


        static string GenerateScript(List <Article> articleList)
        {
            string? openai_api = Environment.GetEnvironmentVariable("OPENAI_API");
            Console.WriteLine(openai_api);
            Console.WriteLine("Generating script");

            string script = "djasujndja";

            return script;
        }

        static void GenerateAudio(string script, int id)
        {
            Console.WriteLine("Generating audio");
        }
        static void MakeVideo(int id)
        {
            Console.WriteLine("Making final video");
        }
    }
}