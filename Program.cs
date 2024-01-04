namespace newsVideoGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);

            Article article = scrapeArticle("https://sex.com");

            string script = GenerateScript(article);

            GenerateAudio(script,id);

            MakeVideo(id);

        }

        public class Article
        {
            public string? url { get; set; }
            public string? title { get; set; }
            public string? content { get; set; }
        }
        static Article scrapeArticle(string url)
        {
            Article article = new Article();
            Console.WriteLine($"Scraping {url}");


            article.url = url;
            article.title = "dasklda";
            article.content = "dasdkamkdmaksdmkamdkmaskdmaksmdkamsdkmams";

            return article;
        }

        static string GenerateScript(Article article)
        {
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