using HtmlAgilityPack;
using DotNetEnv;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels;
using FFMpegCore;
using System.Text.Json;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;


namespace NewsVideoGenerator
{
    public class Article
    {
        public string? url { get; set; }
        public string? title { get; set; }
        public string? content { get; set; }
    }

    public class Config
    {
        public List<string>? articles { get; set; }
        public string videoDirectory { get; set; }
        public string gptModel { get; set; }
        public string ttsModel { get; set; }
        public string ttsVoice { get; set; }
        public float ttsSpeed { get; set; }
        public string gptPrompt { get; set; }
        public string ffmpegDirectory { get; set; }
        public string? openaiAPI { get; set; }
    }

    public class SubtitleElement
    {
        public TimeSpan start { get; set; }
        public TimeSpan end { get; set; }
        public string text { get; set; }
    }
    internal class Program
    {

        static async Task Main(string[] args)
        {
            Env.Load();

            string configJsonString = "";
            try
            {
                configJsonString = File.ReadAllText(args[0]);
            }
            catch {
                configJsonString = File.ReadAllText("config.json");
            }
            Config configJson = JsonSerializer.Deserialize<Config>(configJsonString);
            

            OpenAIService openAiService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API") ?? configJson.openaiAPI ?? ""
            });

            Console.WriteLine("---NEWS VIDEO GEN---");

            Random random = new Random();
            int id = random.Next(10000, 100000);
            Console.WriteLine($"ID: {id}");


            string outputPath = $"output/{id}";
            System.IO.Directory.CreateDirectory(outputPath);

            List<Article> articleList = new List<Article>();
            foreach (var url in configJson.articles)
            {
                if(string.IsNullOrWhiteSpace(url)) continue;
                Article article = await ScrapeArticleAsync(url);
                articleList.Add(article);
            }

            string script = await GenerateScriptAsync(articleList, openAiService, configJson.gptModel, configJson.gptPrompt);

            await GenerateAudioAsync(script, id, openAiService, configJson.ttsModel, configJson.ttsVoice, configJson.ttsSpeed);

            await GenerateSubtitlesAsync(id,openAiService, $"{outputPath}/{id}.mp3");

            MakeVideo(id , configJson.videoDirectory, configJson.ffmpegDirectory, outputPath);
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
                case var _ when url.Contains("cnn"):
                    Console.WriteLine("CNN article found");
                    article = MineCNN(article, htmlDocument);
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

        static Article MineCNN(Article article, HtmlDocument htmlDocument)
        {
            article.title = htmlDocument.DocumentNode.SelectSingleNode("//title").InnerText;
            Console.WriteLine($"Title: {article.title}");

            var contentNodes = htmlDocument.DocumentNode.SelectNodes("//p[@class='paragraph inline-placeholder']");
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


        static async Task<string> GenerateScriptAsync(List <Article> articleList, OpenAIService openAiService,string gptModelString , string gptPrompt)
        {
            Console.WriteLine("Generating script");

            string combinedArticles = "";
            foreach (var article in articleList) {
                combinedArticles = combinedArticles + article.title + article.content;
            }


            var gptModel = "";
            switch (gptModelString)
            {
                case ("Gpt_3_5_Turbo"):
                    gptModel = Models.Gpt_3_5_Turbo;
                    break;
                case ("Gpt_4"):
                    gptModel = Models.Gpt_4;
                    break;
            }

            var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromSystem(gptPrompt),
                    ChatMessage.FromUser($"{combinedArticles}"),
                },
                Model = gptModel,
            });
            string script = "";
            if (completionResult.Successful)
            {
                script = completionResult.Choices.First().Message.Content ?? "";
                Console.WriteLine(script);
            }



            return script;
        }

        static async Task GenerateAudioAsync(string script, int id, OpenAIService openAiService, string ttsModelString, string ttsVoiceString , float ttsSpeed)
        {
            Console.WriteLine("Generating audio");


            var ttsVoice = "";
            switch (ttsVoiceString)
            {
                case "Alloy":
                    ttsVoice = StaticValues.AudioStatics.Voice.Alloy;
                    break;
                case "Echo":
                    ttsVoice = StaticValues.AudioStatics.Voice.Echo;
                    break;
                case "Fable":
                    ttsVoice = StaticValues.AudioStatics.Voice.Fable;
                    break;
                case "Onyx":
                    ttsVoice = StaticValues.AudioStatics.Voice.Onyx;
                    break;
                case "Nova":
                    ttsVoice = StaticValues.AudioStatics.Voice.Nova;
                    break;
                case "Shimmer":
                    ttsVoice = StaticValues.AudioStatics.Voice.Shimmer;
                    break;
            }

            var ttsModel = "";
            switch (ttsModelString)
            {
                case "Tts_1":
                    ttsModel = Models.Tts_1;
                    break;
                case "Tts_1_hd":
                    ttsModel = Models.Tts_1_hd;
                    break;
            }


            var completionResult = await openAiService.Audio.CreateSpeech<Stream>(new AudioCreateSpeechRequest
            {
                Model = ttsModel,
                Input  = script,
                Voice = ttsVoice,
                ResponseFormat = StaticValues.AudioStatics.CreateSpeechResponseFormat.Mp3,
                Speed = ttsSpeed
            });
            if (completionResult.Successful)
            {
                var audio = completionResult.Data!;
                await using var fileStream = File.Create($"output/{id}/{id}.mp3");
                await audio.CopyToAsync(fileStream);

                Console.WriteLine($"Audio generated ");
            }

        }

        static async Task GenerateSubtitlesAsync(int id, OpenAIService openAiService, string fileName)
        {
            Console.WriteLine("Generating subtitles");
            var sampleFile = await File.ReadAllBytesAsync($"{fileName}");
            var audioResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest
            {
                FileName = fileName,
                File = sampleFile,
                Model = Models.WhisperV1,
                ResponseFormat = StaticValues.AudioStatics.ResponseFormat.Srt
            });
            if (audioResult.Successful)
            {
                string transcripton = string.Join("\n", audioResult.Text);
                //Console.WriteLine(transcripton);
                File.WriteAllText($"output/{id}/{id}.srt", transcripton);
                AtomizeSubtitles(transcripton);
            }
            else
            {
                if (audioResult.Error == null)
                {
                    throw new Exception("Unknown Error");
                }
                Console.WriteLine($"{audioResult.Error.Code}: {audioResult.Error.Message}");
            }
        }

        static void AtomizeSubtitles(string originalSubtitles)
        {
            //save srt file to originalSubtitleList
            //not perfect but working (i hope)
            using (StringReader reader = new StringReader(originalSubtitles))
            {
                string line;
                int index = 1;
                int mode = 0; // 0 = index, 1 = time, 2 = text 
                SubtitleElement subtitleElement = null;
                List<SubtitleElement> originalSubtitleList = [];
                while ((line = reader.ReadLine()) != null)
                {
                    if(line == index.ToString())
                    {
                        if (subtitleElement != null)
                        {
                            originalSubtitleList.Add(subtitleElement);
                        }
                        subtitleElement = new SubtitleElement();
                        index++;
                        mode = 1;
                        
                    }
                    else if(mode == 1) 
                    {
                        string[] parts = line.Split(new string[] { " --> " }, StringSplitOptions.None);

                        TimeSpan startTime = TimeSpan.ParseExact(parts[0].Replace(',', '.'), "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
                        subtitleElement.start = startTime;

                        TimeSpan endTime = TimeSpan.ParseExact(parts[1].Replace(',', '.'), "hh\\:mm\\:ss\\.fff", CultureInfo.InvariantCulture);
                        subtitleElement.end = endTime;

                        mode = 2;
                    }
                    else if(mode == 2)
                    {
                        subtitleElement.text = subtitleElement.text +  line;
                    }
                }
                originalSubtitleList.Add(subtitleElement); //this line is needed because for loop ends before adding the last element to the list
                for (int i = 0; i < originalSubtitleList.Count; i++) {
                    Console.WriteLine($"index {i}");
                    Console.WriteLine($"start {originalSubtitleList[i].start}");
                    Console.WriteLine($"end {originalSubtitleList[i].end}");
                    Console.WriteLine($"text {originalSubtitleList[i].text}");
                }
            }



        }

        static string ffmpeg(string ffmpegPath, string arguments)
        {
            string result = String.Empty;

            using (Process proc = new Process())
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.FileName = ffmpegPath;
                proc.StartInfo.Arguments = arguments;
                proc.Start();
                proc.WaitForExit();

                result = proc.StandardOutput.ReadToEnd();
            }
            return result;
        }
        static void MakeVideo(int id, string videoPath,string ffmpegPath, string outputPath)
        {
            Console.WriteLine("Making final video");

            string audioPath = $"{outputPath}/{id}.mp3";
            string subtitlesPath = $"{outputPath}/{id}.srt";
            string videoTrimmedPath = $"{outputPath}/{id}Trimmed.mp4";
            string videoNoAudioPath = $"{outputPath}/{id}NoAudio.mp4";
            string videoSubtitlesPath = $"{outputPath}/{id}Subtitles.mp4";
            string videoFinalPath = $"{outputPath}/{id}.mp4";

            var audioInfo = FFProbe.Analyse(audioPath);
            TimeSpan audioDuration = audioInfo.Duration;
            double audioTotalSeconds = audioDuration.TotalSeconds;
            Console.WriteLine($"Audio duration: {audioTotalSeconds}");
            Console.Write($"{TimeSpan.FromSeconds(audioTotalSeconds)}");

            var videoInfo = FFProbe.Analyse(videoPath);
            TimeSpan videoDuration = videoInfo.Duration;
            double videoTotalSeconds = videoDuration.TotalSeconds;
            Console.WriteLine($"Video duration: {videoTotalSeconds}");

            Random random = new Random();
            int randomTime = random.Next(10, (int)videoTotalSeconds - ((int)audioTotalSeconds) + 10);



            ffmpeg(ffmpegPath, $" -i {videoPath} -ss {randomTime} -t {audioTotalSeconds} -c copy {videoTrimmedPath}");
            Console.WriteLine("Video trimmed");

            ffmpeg(ffmpegPath, $" -i {videoTrimmedPath}  -an {videoNoAudioPath}");
            Console.WriteLine("Video muted");

            ffmpeg(ffmpegPath, $" -i {videoNoAudioPath} -vf subtitles={subtitlesPath} {videoSubtitlesPath}");
            Console.WriteLine("Subtitles added");

            ffmpeg(ffmpegPath, $"-i {videoSubtitlesPath} -i {audioPath} -c:v copy -c:a aac -strict experimental {videoFinalPath}");
            Console.WriteLine("Video combined with audio");

            Console.WriteLine($"Final video: {videoFinalPath}");

        }
    }
}