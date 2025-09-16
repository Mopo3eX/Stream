using Google.Protobuf;
using Newtonsoft.Json.Linq;
using NHibernate.Linq;
using Stream;
using Stream.Classes;
using Stream.Helpers;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Xml.Linq;

class FilteredConsoleWriter : TextWriter
{
    private readonly TextWriter _original;

    public FilteredConsoleWriter(TextWriter original)
    {
        _original = original;
    }

    public override Encoding Encoding => _original.Encoding;

    public override void WriteLine(string value)
    {
        if (value != null && !value.Contains("Unsupported Event: InputSettingsChanged"))
        {
            Program.OnMessage(value);
            _original.WriteLine(value);
        }

    }

    public override void Write(string value)
    {
        if (value != null && !value.Contains("Unsupported Event: InputSettingsChanged"))
        {
            Program.OnMessage(value);
            _original.Write(value);
        }
    }
}

internal class Program
{
    public static Timer _timer = null;
    public static bool Play = false;
    public static OBSWebsocketDotNet.OBSWebsocket obs = new OBSWebsocketDotNet.OBSWebsocket();
    public static Episode CurrentEpisode;
    static void Main(string[] args)
    {
        Console.SetOut(new FilteredConsoleWriter(Console.Out));
        
        _timer = new Timer(TimerCallback, null, 0, 1000);
        obs.ConnectAsync("ws://127.0.0.1:4455", null);
        obs.Connected += Obs_Connected;
        obs.Disconnected += Obs_Disconnected;
        while (true)
        {
            Console.ReadLine();
        }
    }

    private static void Obs_Disconnected(object? sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
    {
        obs.ConnectAsync("ws://127.0.0.1:4455", null);
    }

    static bool waiting = true;
    public static void OnMessage(string text)
    {
        if(text == "Трансляция завершена." && waiting == false)
        {
            obs.SetCurrentProgramScene("Waiting");
            waiting = true;
        }
        else if (text.StartsWith("frame=") && !text.Contains("time=N/A") && !text.Contains("fps=0.") && waiting == true)
        {
            obs.SetCurrentProgramScene("Playing");
            waiting = false;
        }
    }
    public static string LastTime = "";
    public static Episode LastEpisode;

    static void OBSClient()
    {
        while (true)
        {
            try
            {
                if (LastTime != DateTime.Now.TimeOfDay.ToString("hh\\:mm"))
                {
                    LastTime = DateTime.Now.TimeOfDay.ToString("hh\\:mm");
                    var settings = new JObject
                {
                    { "text", LastTime }
                };

                    obs.SetInputSettings("Time", settings, false);
                }
                if (CurrentEpisode != null)
                {
                    if (LastEpisode != CurrentEpisode)
                    {
                        LastEpisode = CurrentEpisode;
                        var settings = new JObject
                {
                    { "text", $"{CurrentEpisode.Serial.RussianName} - {CurrentEpisode.Serial.EpisodesDb.IndexOf(CurrentEpisode)+1} Серия" }

                };

                        obs.SetInputSettings("Title", settings, false);
                    }
                }
            }
            catch { }
            Thread.Sleep(1000);
        }
    }
    static Thread th;
    private static void Obs_Connected(object? sender, EventArgs e)
    {
        if(!Play)
            obs.SetCurrentProgramScene("Waiting");
        if (th == null)
        {
            th = new Thread(OBSClient);
            th.Start();
        }
    }

    private static void TimerCallback(Object o)
    {
        if (Play)
            return;
        Play = true;
        using (var session = NHibernateHelper.OpenSession())
        {
            using (var transaction = session.BeginTransaction())
            {
                var schedules2 = session.Query<Schedule>();//.Where(s => s.Start>=(DateTime.Now.AddHours(-1)) && s.Start <=(DateTime.Now.AddHours(1)));
                var schedules = schedules2.ToList();
                for(int i = 0; i<3; i++)
                {
                    var st = DateTime.Now.Date.AddDays(i);
                    var nd = DateTime.Now.Date.AddDays(i + 1);
                    var tmp = schedules.Where(x => x.Start > st && x.Start < nd);
                    var tmp2 = tmp.ToList();
                    if (tmp2.Count == 0)
                    {
                        WeeklyScheduleGenerator.GenerateWeeklySchedule(st);
                        Play = false;
                        return;
                    }
                }
                schedules = schedules.Where(x => x.Start <= DateTime.Now && x.Start + x.Episode.Duration > DateTime.Now).ToList();
                if(schedules.Count == 0)
                {
                    Play = false;
                    return;
                }
                var CurentVideo = schedules[0];
                if (CurentVideo.Start == DateTime.Now)
                {
                    Play = true;
                    StreamingVideo(CurentVideo.Episode, new TimeSpan(00, 00, 00));
                }
                else
                {
                    Play = true;
                    TimeSpan proshlo = DateTime.Now - CurentVideo.Start;
                    //TimeSpan TimeStart = new TimeSpan(0, 0, (int)(CurentVideo.Length.TotalSeconds - proshlo.TotalSeconds));
                    StreamingVideo(CurentVideo.Episode, proshlo);
                }
                transaction.Commit();
            }
        }
    }

    public static async Task StreamingVideo(Episode video, TimeSpan ss)
    {
        CurrentEpisode = video;
        foreach (Process prc in Process.GetProcessesByName("ffmpeg"))
        {
            //prc.Close();
            prc.Kill();
        }
        string startTimeStr = ss.ToString(@"hh\:mm\:ss");
        // Адрес RTMP-сервера. Замените YOUR_SERVER_IP на реальный IP или домен.
        string rtmpUrl = "rtmp://127.0.0.1/live/livestream";

        // Формируем аргументы для FFmpeg:
        // - -re: передача видео с оригинальной скоростью
        // - -i: входной файл
        // - -c:v libx264 -preset fast -b:v 2000k: кодирование видео с нужными параметрами
        // - -c:a aac -b:a 128k: кодирование аудио
        // - -f flv: формат потока (для RTMP)

        //string ForAudio = "";
        //string ForAudio2 = "";
        string ForAudio2 = "-map 0:v";
        string ForAudio = "";

        if (video.EpisodeAudio == null)
        {
            // Если аудио не задано отдельно, берём аудио из видео (если оно есть)
            ForAudio = "";
            ForAudio2 = "";
        }
        else
        {
            // Если аудиофайл задан, отключаем аудио из видеофайла и используем только аудио из второго входа
            ForAudio = $"-i \"{Servers.URLServers[video.ServerID]}{video.EpisodeAudio}\" -map 1:a";
        }
        


        string ForSub = "";
        string subtitleLocalPath = null;
        if (video.EpisodeSubs != null)
        {
            subtitleLocalPath = GetRandomizedFileName(); // Локальное имя для файла субтитров
            //https\\://storage.yandexcloud.net/animebacket/{video.EpisodeSubs}
            using (var client = new WebClient())
            {
                Console.WriteLine("Начало загрузки субтитров...");
                await client.DownloadFileTaskAsync(new Uri($"{Servers.URLServers[video.ServerID]}{video.EpisodeSubs}"), subtitleLocalPath);
                Console.WriteLine("Субтитры успешно скачаны.");
            }
            string absoluteSubPath = Path.GetFullPath(subtitleLocalPath).Replace("\\", "\\\\").Replace(":", "\\:").Replace("'", "\\'");
            ForSub = $"-vf \"ass='{absoluteSubPath}'\"";
            //ForSub = $"-vf \"ass='https\\://storage.yandexcloud.net/animebacket/{video.EpisodeSubs}'\"";
        }
        //-preset fast
        string scaleFilter = "-vf \"scale=w=1280:h=720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2\"";

        string arguments = $"-re -copyts -ss {startTimeStr} -i \"{Servers.URLServers[video.ServerID]}{video.EpisodeVideo}\" {ForAudio} {ForSub} {ForAudio2} {scaleFilter} -c:v libx264 -b:v 2000k -c:a aac -b:a 128k -f flv {rtmpUrl}";
        //string arguments = $"-re -copyts -ss {startTimeStr} -i \"{Servers.URLServers[video.ServerID]}{video.EpisodeVideo}\" {ForAudio} {ForSub} {ForAudio2} -c:v libx264 -b:v 2000k -c:a aac -b:a 128k -flags +global_header -muxpreload 0 -muxdelay 0 -async 1 -f mpegts udp://127.0.0.1:1234";
        //string arguments = $"-re -copyts -ss {startTimeStr} -i \"{Servers.URLServers[video.ServerID]}{video.EpisodeVideo}\" {ForAudio} {ForSub} {ForAudio2} -c:v libx264 -b:v 2000k -c:a aac -b:a 128k -f mpegts udp://127.0.0.1:1234";
        //string arguments = $"-re -copyts -ss {startTimeStr} -i \"{Servers.URLServers[video.ServerID]}{video.EpisodeVideo}\" {ForAudio} {ForSub} {ForAudio2} -c:v libx264 -b:v 2000k -c:a aac -b:a 128k -mpegts_flags +resend_headers -f mpegts srt://127.0.0.1:1234?mode=caller";
        //string arguments = $"-re -ss {startTimeStr} -i \"{Servers.URLServers[video.ServerID]}{video.EpisodeVideo}\" {ForAudio} {ForSub} {ForAudio2} -c:v libx264 -b:v 2000k -c:a aac -b:a 128k -flags +global_header -muxpreload 0 -muxdelay 0 -async 1 -f mpegts udp://127.0.0.1:1234";

        Console.WriteLine("Запуск трансляции...");

        // Создаем и настраиваем процесс для FFmpeg
        var ffmpegProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Подписываемся на поток ошибок для вывода логов FFmpeg
        ffmpegProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(e.Data);
                File.AppendAllText("log.log", $"{e.Data}\r\n");
            }

        };
        ffmpegProcess.OutputDataReceived += (sender, e) =>
        {
            
            if (!string.IsNullOrEmpty(e.Data))
            {
                File.AppendAllText("log.log", $"{e.Data}\r\n");
            }
        };
        // Запускаем процесс
        ffmpegProcess.Start();
        ffmpegProcess.BeginErrorReadLine();
        ffmpegProcess.BeginOutputReadLine();

        // Дожидаемся завершения процесса трансляции (либо можно реализовать логику для принудительного завершения)
        ffmpegProcess.WaitForExit();
        ffmpegProcess.Close();
        if (File.Exists(subtitleLocalPath))
        {
            File.Delete(subtitleLocalPath);
        }
        Console.WriteLine("Трансляция завершена.");
        Play = false;
    }
    public static string GetRandomizedFileName()
    {
        // Генерируем случайное имя файла с помощью GUID
        string randomName = Guid.NewGuid().ToString();

        // Формируем новое имя файла, объединяя случайное имя и расширение
        string newFileName = randomName + ".ass";
        return newFileName;
    }
}