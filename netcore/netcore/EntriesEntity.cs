namespace netcore
{
    public class EntriesEntity
    {
        public decimal date { get; set; }

        public DateTime date_now { get; set; }
        public string date_str { get; set; }
        public string date_time { get; set; }
        public int date_step { get; set; }
        public int? sgv { get; set; }
        public double? sgv_str { get; set; }
        public string direction { get; set; }
        public string direction_str { get; set; }
        public bool isMask { get; set; }
        public string title { get; set; }
        public string saying { get; set; }
    }
}
