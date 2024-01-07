using HtmlAgilityPack;
using DotNetEnv;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using FFMpegCore;

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

            OpenAIService openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API") ?? ""
            });

            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);
            Console.WriteLine($"ID: {id}");


            string outputPath = $"output/{id}";
            System.IO.Directory.CreateDirectory(outputPath);

            List<Article> articleList = new List<Article>();
            foreach (var url in args)
            {
                // Await the asynchronous method call
                Article article = await ScrapeArticleAsync(url);
                articleList.Add(article);
            }

            string script = await GenerateScriptAsync(articleList, openAiService);

            await GenerateAudioAsync(script, id, openAiService);

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


        static async Task<string> GenerateScriptAsync(List <Article> articleList, OpenAIService openAiService)
        {
            Console.WriteLine("Generating script");

            string combinedArticles = "";
            foreach (var article in articleList) {
                combinedArticles = combinedArticles + article.title + article.content;
            }




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
                Console.WriteLine(script);
            }



            return script;
        }

        static async Task GenerateAudioAsync(string script, int id, OpenAIService openAiService)
        {
            Console.WriteLine("Generating audio");


            var completionResult = await openAiService.Audio.CreateSpeech<Stream>(new AudioCreateSpeechRequest
            {
                Model = Models.Tts_1,
                Input  = script,
                Voice = StaticValues.AudioStatics.Voice.Onyx,
                ResponseFormat = StaticValues.AudioStatics.CreateSpeechResponseFormat.Mp3,
                Speed = 1.3f
            });
            if (completionResult.Successful)
            {
                var audio = completionResult.Data!;
                await using var fileStream = File.Create($"output/{id}/{id}.mp3");
                await audio.CopyToAsync(fileStream);

                Console.WriteLine($"Audio {id}.mp3 generated successfully");
            }

        }
        static void MakeVideo(int id)
        {
            Console.WriteLine("Making final video");
            
            string videoPath = $"";
            string audioPath = $"output/{id}/{id}.mp3";
            string outputPath = $"output/{id}";
            string videoTrimmedPath = $"{outputPath}/{id}Trimmed.mp4";
            string videoScaled = $"{outputPath}/{id}Scaled.mp4";
            string videoNoAudioPath = $"{outputPath}/{id}NoAudio.mp4";
            string videoFinalPath = $"{outputPath}/{id}.mp4";

            var audioInfo = FFProbe.Analyse(audioPath);
            TimeSpan audioDuration = audioInfo.Duration;
            double audioTotalSeconds = audioDuration.TotalSeconds;
            //Console.WriteLine($"Audio duration: {audioTotalSeconds}");

            var videoInfo = FFProbe.Analyse(videoPath);
            TimeSpan videoDuration = videoInfo.Duration;
            double videoTotalSeconds = videoDuration.TotalSeconds;
            //Console.WriteLine($"Video duration: {videoTotalSeconds}");

            Random random = new Random();
            int randomTime = random.Next(10, (int)videoTotalSeconds - ((int)audioTotalSeconds) + 10);


            FFMpeg.SubVideo(videoPath, videoTrimmedPath,TimeSpan.FromSeconds(randomTime),TimeSpan.FromSeconds(randomTime+audioTotalSeconds));
            Console.WriteLine("Video trimmed");
            FFMpeg.Mute(videoTrimmedPath, videoNoAudioPath);
            Console.WriteLine("Video muted");
            FFMpeg.ReplaceAudio(videoNoAudioPath, audioPath, videoFinalPath);
            Console.WriteLine("Video combined with audio");
            Console.WriteLine($"Final video: {videoFinalPath}");

        }
    }
}