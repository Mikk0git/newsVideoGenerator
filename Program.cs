using HtmlAgilityPack;
using DotNetEnv;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;

namespace NewsVideoGenerator
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
            Env.Load();

            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);

            List<Article> articleList = new List<Article>();
            foreach (var url in args)
            {
                // Await the asynchronous method call
                Article article = await ScrapeArticleAsync(url);
                articleList.Add(article);
            }

            string script = await GenerateScriptAsync(articleList);

            GenerateAudio(script, id);

            MakeVideo(id);
        }


        static async Task<Article> ScrapeArticleAsync(string url)
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
                    article = MineBBC(article, htmlDocument);
                    break;
                default:
                    Console.WriteLine("Source unknown");
                    break;
            }



            return article;
        }

        static Article MineBBC(Article article, HtmlDocument htmlDocument)
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


        static async Task<string> GenerateScriptAsync(List <Article> articleList)
        {
            Console.WriteLine("Generating script");

            string combinedArticles = "";
            foreach (var article in articleList) {
                combinedArticles = combinedArticles + article.title + article.content;
            }


            var openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API") ?? ""
            });

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem("Write a short summary of this articles that will be presented as a short form (under a minute) video." +
                    " It must start with an interesting litte clickbait. The video must be under a minute, about 140 words." +
                    " Write only the script, NO hashtags, NO greatings, NO `???`, NO `#' etc. Remember to DO NOT ADD # hashtags at the end"),
                    ChatMessage.FromUser($"{combinedArticles}"),
                },
                Model = Models.Gpt_3_5_Turbo,
            });
            string script = "";
            if (completionResult.Successful)
            {
                script = completionResult.Choices.First().Message.Content ?? "";
                Console.WriteLine();
            }



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