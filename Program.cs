namespace newsVideoGenerator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);


            List <Article> articleList = new List<Article>();
            foreach (var url in args)
            {
                articleList.Add(scrapeArticle(url));
            }

            string script = GenerateScript(articleList);

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

        static string GenerateScript(List <Article> articleList)
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