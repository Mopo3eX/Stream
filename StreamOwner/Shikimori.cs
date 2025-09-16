using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamOwner
{
    // Shikimori myDeserializedClass = JsonConvert.DeserializeObject<Shikimori>(myJsonResponse);
    public class Genre
    {
        public int id { get; set; }
        public string name { get; set; }
        public string russian { get; set; }
        public string kind { get; set; }
        public string entry_type { get; set; }
    }

    public class Image
    {
        public string original { get; set; }
        public string preview { get; set; }
        public string x96 { get; set; }
        public string x48 { get; set; }
    }

    public class RatesScoresStat
    {
        public int name { get; set; }
        public int value { get; set; }
    }

    public class RatesStatusesStat
    {
        public string name { get; set; }
        public int value { get; set; }
    }

    public class Shikimori
    {
        public int id { get; set; }
        public string name { get; set; }
        public string russian { get; set; }
        public Image image { get; set; }
        public string url { get; set; }
        public string kind { get; set; }
        public string score { get; set; }
        public string status { get; set; }
        public int episodes { get; set; }
        public int episodes_aired { get; set; }
        public string aired_on { get; set; }
        public string released_on { get; set; }
        public string rating { get; set; }
        public List<string> english { get; set; }
        public List<string> japanese { get; set; }
        public List<string> synonyms { get; set; }
        public string license_name_ru { get; set; }
        public int duration { get; set; }
        public string description { get; set; }
        public string description_html { get; set; }
        public object description_source { get; set; }
        public string franchise { get; set; }
        public bool favoured { get; set; }
        public bool anons { get; set; }
        public bool ongoing { get; set; }
        public int thread_id { get; set; }
        public int topic_id { get; set; }
        public int myanimelist_id { get; set; }
        public List<RatesScoresStat> rates_scores_stats { get; set; }
        public List<RatesStatusesStat> rates_statuses_stats { get; set; }
        public DateTime updated_at { get; set; }
        public object next_episode_at { get; set; }
        public List<string> fansubbers { get; set; }
        public List<string> fandubbers { get; set; }
        public List<string> licensors { get; set; }
        public List<Genre> genres { get; set; }
        public List<Studio> studios { get; set; }
        public List<Video> videos { get; set; }
        public List<Screenshot> screenshots { get; set; }
        public object user_rate { get; set; }
    }

    public class Screenshot
    {
        public string original { get; set; }
        public string preview { get; set; }
    }

    public class Studio
    {
        public int id { get; set; }
        public string name { get; set; }
        public string filtered_name { get; set; }
        public bool real { get; set; }
        public string image { get; set; }
    }

    public class Video
    {
        public int id { get; set; }
        public string url { get; set; }
        public string image_url { get; set; }
        public string player_url { get; set; }
        public string name { get; set; }
        public string kind { get; set; }
        public string hosting { get; set; }
    }
}
