using MediaInfo.DotNetWrapper.Enumerations;
using Stream.Classes;
using Stream.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Security.Policy;
using Newtonsoft.Json;
using MySqlX.XDevAPI;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Diagnostics;
//using MediaInfoLib;
namespace StreamOwner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, EventArgs e)
        {
            //if(selectVideoDialog.ShowDialog() == DialogResult.OK)
            //{
            //    string file = selectVideoDialog.FileName;
            //    string fileAudio = null;
            //    string fileSub = null;
            //    if (selectAudioDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        fileAudio = selectAudioDialog.FileName;
            //    }
            //    if (selectSubDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        fileSub = selectSubDialog.FileName;
            //    }
            //    string name = videoName.Text;
            //    bool newVideo = newCheckBox.Checked;
            //    TimeSpan AdTime = TimeSpan.Parse(adVideoTime.Text);
            //    DateTime Start;
            //    TimeSpan Length = GetVideoDuration(file);
            //    if (nextBox.Checked)

            //    {
            //        Start = DateTime.Today;
            //    }
            //    else
            //    {
            //        Start = dateTimePicker1.Value;
            //    }

            //    using (var session = NHibernateHelper.OpenSession())
            //    {
            //        using (var transaction = session.BeginTransaction())
            //        {
            //            var videos = session.Query<Video>().ToList();
            //            if(Start == DateTime.Today)
            //            {
            //                Start = videos.Last().Start + videos.Last().Length;
            //            }
            //            Video nw = new Video();
            //            nw.Start = Start;
            //            nw.Length = Length;
            //            nw.Name = name;
            //            nw.Ads = null;
            //            nw.Eyecatch = AdTime;
            //            nw.New = newVideo;
            //            nw.VideoDir = file;
            //            nw.AudioDir = fileAudio;
            //            nw.SubDir = fileSub;
            //            Clipboard.SetText($"{nw.Start.ToString("dd.MM.yyyy HH:mm")} - {nw.Name}");
            //            session.Save(nw);
            //            transaction.Commit();
            //        }
            //    }
            //}
        }
        static TimeSpan GetVideoDuration(string filePath)
        {
            using (var mediaInfo = new MediaInfo.DotNetWrapper.MediaInfo())
            {
                mediaInfo.Open(filePath);
                string durationStr = mediaInfo.Get(StreamKind.General, 0, "Duration");
                if (double.TryParse(durationStr, out double durationMs))
                {
                    return TimeSpan.FromMilliseconds(durationMs);
                }
                return TimeSpan.Zero;
            }
        }

        private void nextBox_CheckedChanged(object sender, EventArgs e)
        {
            
        }
        private const string bucketName = "animebacket";
        private const string accessKey = "YCAJEdcnJTXhK5-Xby-_aUWeQ";
        private const string secretKey = "YCNzAJ-LZVUFSjCjbLYjICXi30acsTDan6R6RGZB";
        private const string serviceUrl = "https://storage.yandexcloud.net"; // Yandex Object Storage

        private void button1_Click(object sender, EventArgs e)
        {
            int ShikimoriID = (int)numericUpDown1.Value;
            string shiki = "";
            using (var webClient = new WebClient())
            {
                webClient.Encoding = Encoding.UTF8;
                // Выполняем запрос по адресу и получаем ответ в виде строки
                shiki = webClient.DownloadString($"https://shikimori.one/api/animes/{ShikimoriID}");
            }
            Shikimori myDeserializedClass = JsonConvert.DeserializeObject<Shikimori>(shiki);
            using (var session = NHibernateHelper.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var Serials = session.Query<Serial>().ToList();
                    if (Serials.Where(x => x.ShikimoriId == ShikimoriID).Count() > 0)

                    {
                        MessageBox.Show($"Такой сериал уже есть ({Serials.Where(x => x.ShikimoriId == ShikimoriID).ToList()[0].Id})");
                        return;
                    }
                    Serial serial = new Serial();
                    serial.Description = myDeserializedClass.description;
                    serial.State = State.Created;
                    serial.Episodes = myDeserializedClass.episodes;
                    serial.Episodes_Aired = myDeserializedClass.episodes_aired;
                    serial.Name = myDeserializedClass.name;
                    serial.RussianName = myDeserializedClass.russian;
                    serial.ShikimoriId = ShikimoriID;
                    
                    session.Save(serial);
                    transaction.Commit();
                    numericUpDown2.Value = serial.Id;

                }
            }

        }
        static TimeSpan GetVideoDuration2(string videoUrl)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = "-i \"" + videoUrl + "\" -show_entries format=duration -v quiet -of csv=\"p=0\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            process.Dispose();

            int seconds;
            if (int.TryParse(output.Split('.')[0], out seconds))
            {
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                return time;
            }

            return TimeSpan.Zero;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl, // Указываем URL сервиса
                ForcePathStyle = true    // Включаем поддержку path-style
            };

            var s3Client = new AmazonS3Client(accessKey, secretKey, config);
            var request = new ListObjectsV2Request
            {
                BucketName = Servers.Backets[(int)numericUpDown3.Value],
                Prefix = $"{numericUpDown2.Value}/"
            };
            var response = s3Client.ListObjectsV2(request);
            List<string> Files = new List<string>();
            foreach (var entry in response.S3Objects)
            {
                Files.Add(entry.Key);
                //Console.WriteLine($"Файл: {entry.Key} (размер: {entry.Size} байт)");
            }

            using (var session = NHibernateHelper.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var Serials = session.Query<Serial>().ToList();
                    if (Serials.Where(x => x.Id == numericUpDown2.Value).Count() == 1)
                    {
                        Serial ser = Serials.Where(x => x.Id == numericUpDown2.Value).ToList()[0];
                        for(int i = 1; i<= ser.Episodes;i++)
                        {
                            
                            string Video = null;
                            if(Files.Where(x=> x.StartsWith($"{numericUpDown2.Value}/{i}v.")).Count()==1)
                            {
                                Video = Files.Where(x => x.StartsWith($"{numericUpDown2.Value}/{i}v.")).ToList()[0];
                            }
                            if(ser.EpisodesDb.Count() > 0 && ser.EpisodesDb.Where(x => x.EpisodeVideo == Video).Count() == 1 || Video == null)
                            {
                                continue;
                            }
                            
                            string Audio = null;
                            if (Files.Where(x => x.StartsWith($"{numericUpDown2.Value}/{i}a.")).Count() == 1)
                            {
                                Audio = Files.Where(x => x.StartsWith($"{numericUpDown2.Value}/{i}a.")).ToList()[0];
                            }
                            string Subs = null;
                            if (Files.Where(x => x.StartsWith($"{numericUpDown2.Value}/{i}s.")).Count() == 1)
                            {
                                Subs = Files.Where(x => x.StartsWith($"{numericUpDown2.Value}/{i}s.")).ToList()[0];
                            }
                            Episode ep = new Episode();
                            ep.EpisodeVideo = Video;
                            ep.EpisodeAudio = Audio;
                            ep.EpisodeSubs = Subs;
                            ep.ServerID = (int)numericUpDown3.Value;
                            ep.Duration = GetVideoDuration2($"{Servers.URLServers[ep.ServerID]}{Video}");
                            ep.Serial = ser;
                            ep.State = State.Created;
                            ser.EpisodesDb.Add(ep);
                        }
                        session.Save(ser);
                        transaction.Commit();
                    }


                    //session.Save(serial);
                    //transaction.Commit();
                }
            }
            //if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            //{
            //    List<TmpSerial> serials = new List<TmpSerial>();
            //    foreach(var dir in Directory.GetDirectories(folderBrowserDialog1.SelectedPath))
            //    {
            //        TmpSerial tmpSerial = new TmpSerial();
            //        tmpSerial.Name = File.ReadAllText(dir + "\\Name.txt");
            //        if(File.Exists(dir+ "\\StartSerie.txt"))
            //        {
            //            tmpSerial.StartSeria = int.Parse(File.ReadAllText(dir + "\\StartSerie.txt"));
            //        }
            //        int start = tmpSerial.StartSeria;
            //        while(File.Exists(dir + $"\\{start}v.mkv") || File.Exists(dir + $"\\{start}v.avi"))
            //        {
            //            TmpSeria tmpSeria = new TmpSeria();
            //            tmpSeria.Video = File.Exists(dir + $"\\{start}v.mkv") ? dir + $"\\{start}v.mkv" : dir + $"\\{start}v.avi";
            //            tmpSeria.Subs = File.Exists(dir + $"\\{start}s.ass") ? dir + $"\\{start}s.ass" : null;
            //            tmpSeria.Audio = File.Exists(dir + $"\\{start}a.mka") ? dir + $"\\{start}a.mka" : null;
            //            tmpSeria.Length = GetVideoDuration(tmpSeria.Video);
            //            tmpSerial.Series.Add(start, tmpSeria);
            //            start++;
            //        }
            //        serials.Add(tmpSerial);
            //    }
            //    int NumSeria = 0;
            //    var Start = DateTime.Now;
            //    bool Adding = true;
            //    string Teleprog = "";
            //    using (var session = NHibernateHelper.OpenSession())
            //    {
            //        using (var transaction = session.BeginTransaction())
            //        {
            //            while(Adding)
            //            {
            //                Adding = false;
            //                foreach (var seria in serials)
            //                {
            //                    if(seria.Series.ContainsKey(seria.StartSeria + NumSeria))
            //                    {
            //                        Adding = true;
            //                        Video nw = new Video();
            //                        nw.Start = Start;
            //                        nw.Length = seria.Series[seria.StartSeria + NumSeria].Length;
            //                        nw.Name = $"{seria.Name} | {seria.StartSeria + NumSeria} Серия";
            //                        nw.Ads = null;
            //                        nw.Eyecatch = new TimeSpan(0,12,0);
            //                        nw.New = false;
            //                        nw.VideoDir = seria.Series[seria.StartSeria + NumSeria].Video;
            //                        nw.AudioDir = seria.Series[seria.StartSeria + NumSeria].Audio;
            //                        nw.SubDir = seria.Series[seria.StartSeria + NumSeria].Subs;
            //                        Teleprog += $"{nw.Start.ToString("dd.MM.yyyy HH:mm")} - {nw.Name}\r\n";

            //                        session.Save(nw);
            //                        Start = Start + nw.Length;
            //                    }
            //                }
            //                NumSeria++;
            //            }
            //            transaction.Commit();
            //        }
            //    }
            //    Clipboard.SetText(Teleprog);
            //}
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var session = NHibernateHelper.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var Episodes = session.Query<Episode>().ToList();
                    Episodes = Episodes.Where(x => x.Duration == TimeSpan.Zero).ToList();
                    for(int i  = 0; i < Episodes.Count; i++)
                    {
                        Episodes[i].Duration = GetVideoDuration2($"https://storage.yandexcloud.net/animebacket/{Episodes[i].EpisodeVideo}");
                        session.Save(Episodes[i]);
                    }
                    //session.Save(serial);
                    transaction.Commit();
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (checkBox1.Checked)
                {
                    int num = (int)numericUpDown1.Value;
                    foreach (string file in Directory.GetFiles(folderBrowserDialog1.SelectedPath))
                    {
                        if (file.EndsWith(".mkv"))
                        {
                            string nw_file_name = folderBrowserDialog1.SelectedPath + $"\\{num}v.mkv";
                            File.Move(file, nw_file_name);
                            num++;
                        }
                        if (file.EndsWith(".avi"))
                        {
                            string nw_file_name = folderBrowserDialog1.SelectedPath + $"\\{num}v.avi";
                            File.Move(file, nw_file_name);
                            num++;
                        }
                    }
                }
                if (checkBox3.Checked)
                {
                    int num = (int)numericUpDown1.Value;
                    foreach (string file in Directory.GetFiles(folderBrowserDialog1.SelectedPath))
                    {
                        if (file.EndsWith(".ass"))
                        {
                            string nw_file_name = folderBrowserDialog1.SelectedPath + $"\\{num}s.ass";
                            File.Move(file, nw_file_name);
                            num++;
                        }
                    }
                }
                if (checkBox2.Checked)
                {
                    int num = (int)numericUpDown1.Value;
                    foreach (string file in Directory.GetFiles(folderBrowserDialog1.SelectedPath))
                    {
                        if (file.EndsWith(".mka"))
                        {
                            string nw_file_name = folderBrowserDialog1.SelectedPath + $"\\{num}a.mka";
                            File.Move(file, nw_file_name);
                            num++;
                        }
                    }
                }
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl, // Указываем URL сервиса
                ForcePathStyle = true    // Включаем поддержку path-style
            };

            var s3Client = new AmazonS3Client(accessKey, secretKey, config);
            var request = new ListObjectsV2Request
            {
                BucketName = Servers.Backets[(int)numericUpDown3.Value],
                Prefix = $"{numericUpDown2.Value}/"
            };
            var response = s3Client.ListObjectsV2(request);
            List<string> Files = new List<string>();
            foreach (var entry in response.S3Objects)
            {
                Files.Add(entry.Key);
            }
            foreach(var file in Files)
            {
                string filename = $"F:\\Stream\\{file.Replace('/', '\\')}";
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int ShikimoriID = (int)numericUpDown1.Value;
            string shiki = "";
            using (var webClient = new WebClient())
            {
                webClient.Encoding = Encoding.UTF8;
                // Выполняем запрос по адресу и получаем ответ в виде строки
                shiki = webClient.DownloadString($"https://shikimori.one/api/animes/{ShikimoriID}");
            }
            Shikimori myDeserializedClass = JsonConvert.DeserializeObject<Shikimori>(shiki);
            using (var session = NHibernateHelper.OpenSession())
            {
                using (var transaction = session.BeginTransaction())
                {
                    var serial = session.Query<Serial>().Where(x => x.Id == numericUpDown2.Value).ToList()[0];
                    serial.Description = myDeserializedClass.description;
                    serial.State = State.Created;
                    serial.Episodes = myDeserializedClass.episodes;
                    serial.Episodes_Aired = myDeserializedClass.episodes_aired;
                    serial.Name = myDeserializedClass.name;
                    serial.RussianName = myDeserializedClass.russian;
                    serial.ShikimoriId = ShikimoriID;

                    session.Save(serial);
                    transaction.Commit();
                }
            }
        }
    }

    public class TmpSerial
    {
        public string Name = "";
        public int StartSeria = 1;
        public Dictionary<int, TmpSeria> Series = new Dictionary<int, TmpSeria>();
    }

    public class TmpSeria
    {
        public string Video = null;
        public string Audio = null;
        public string Subs = null;
        public TimeSpan Length;
        public bool Added = false;
    }
}
