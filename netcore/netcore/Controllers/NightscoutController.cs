using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.Json;
using MongoDB.Driver;
using MongoDB.Bson;
using static System.Net.Mime.MediaTypeNames;

namespace netcore.Controllers
{
    [ApiController]
    [Route("api/ns")]
    public class NightscoutController : ControllerBase
    {
        private readonly ILogger<NightscoutController> _logger;
        private readonly IConfiguration configuration;

        public NightscoutController(ILogger<NightscoutController> logger, IConfiguration _configuration)
        {
            _logger = logger;
            configuration = _configuration;
        }
        [HttpGet]
        [Route("CodeLogin")]
        public async Task<MessageModel<WeChatModel>> CodeLogin(string code)
        {
            var appid = configuration.GetValue<string>("appid");
            var secret = configuration.GetValue<string>("secret");
            HttpClient httpClient = new HttpClient();
            var res = await httpClient.GetStringAsync($"https://api.weixin.qq.com/sns/jscode2session?appid={appid}&secret={secret}&js_code={code}&grant_type=authorization_code");
            
            var data = JsonHelper.JsonToObj<WeChatModel>(res);
            if (data.errcode.Equals(0))
            {
                return MessageModel<WeChatModel>.Success(string.Empty, data);
            }
            else
            {
                return MessageModel<WeChatModel>.Fail(string.Empty, data);
            }
            
        }


        [HttpGet]
        [Route("GetCurBloodSugar")]
        public async Task<MessageModel<EntriesEntity>> GetCurBloodSugar(string serviceName)
        {
            var Host = configuration.GetValue<string>("Host");
            var Port = configuration.GetValue<string>("Port");
            var LoginName = configuration.GetValue<string>("LoginName");
            var LoginPasswd = configuration.GetValue<string>("LoginPasswd");

            var grantConnectionMongoString = $"mongodb://{LoginName}:{LoginPasswd}@{Host}:{Port}";
            var client = new MongoClient(grantConnectionMongoString);
            var database = client.GetDatabase(serviceName);
            var collection = database.GetCollection<BsonDocument>("entries"); // 替换为你的集合名称
            var find = new MongoDB.Driver.FindOptions<BsonDocument>
            {
                Sort = Builders<BsonDocument>.Sort.Descending("_id"), // 根据 _id 字段倒序排序
                Limit = 1 // 只获取一条数据
            };
            var filter = Builders<BsonDocument>.Filter.Empty; // 获取所有数据
            //var filter = Builders<BsonDocument>.Filter.Eq("fieldName", "fieldValue"); // 构建查询条件

            var projection = Builders<BsonDocument>.Projection.Include("date").Include("sgv").Include("direction").Exclude("_id");


            //var ls = collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(300).Project(projection).ToList();

            var result = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(1).Project(projection).FirstOrDefaultAsync();
            var txt = result.ToJson();
            var dd = JsonHelper.JsonToObj<EntriesEntity>(txt);
            FormatDate(dd);

            return MessageModel<EntriesEntity>.Success("", dd);

        }

        [HttpGet]
        [Route("GetBloodSugars")]
        public async Task<MessageModel<List<EntriesEntity>>> GetBloodSugars(string serviceName,int day)
        {
            var Host = configuration.GetValue<string>("Host");
            var Port = configuration.GetValue<string>("Port");
            var LoginName = configuration.GetValue<string>("LoginName");
            var LoginPasswd = configuration.GetValue<string>("LoginPasswd");

            var grantConnectionMongoString = $"mongodb://{LoginName}:{LoginPasswd}@{Host}:{Port}";
            var client = new MongoClient(grantConnectionMongoString);
            var database = client.GetDatabase(serviceName);
            var collection = database.GetCollection<BsonDocument>("entries"); // 替换为你的集合名称

            var filter = Builders<BsonDocument>.Filter.Empty; // 获取所有数据
            var projection = Builders<BsonDocument>.Projection.Include("date").Include("sgv").Include("direction").Exclude("_id");


            var ls = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(300).Project(projection).ToListAsync();
            var sugers = ls.ToJson();

            var dd = JsonHelper.JsonToObj<List<EntriesEntity>>(sugers);

            foreach (var item in dd)
            {
                FormatDate(item);
            }
            var flagDate = DateTime.Now.Date.AddDays(day);
            dd = dd.Where(t => t.date_now.Date == flagDate).ToList();

            dd.Reverse();
            if (dd.Count > 0)
            {
                var last = dd[dd.Count - 1];
                if (last.date_now.Hour <= 20)
                {
                    var curHour = last.date_now.Hour;
                    while (true)
                    {
                        curHour += 3;
                        if (curHour > 23) break;
                        dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
                    }
                }
                else if (last.date_now.Hour >= 21 && last.date_now.Hour <= 23)
                {
                    var curHour = last.date_now.Hour;
                    while (true)
                    {
                        curHour += 1;
                        if (curHour > 23) break;
                        if(curHour == 23 && last.date_now.Hour==22)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
                            }
                        }else if (curHour == 23 && last.date_now.Hour == 21)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
                            }
                        }
                    }
                }
            }
            return MessageModel<List<EntriesEntity>>.Success("", dd);

        }


        private void FormatDate(EntriesEntity dd)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)dd.date);
            DateTime dateTime = dateTimeOffset.UtcDateTime.AddHours(8);
            dd.date_now = dateTime;
            dd.date_str = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            dd.date_step = (int)(DateTime.Now - dateTime).TotalMinutes;

            dd.sgv_str = Math.Round(1.0 * dd.sgv / 18, 1);

            dd.direction_str = GetFlag(dd.direction);
        }
        private string GetFlag(string flag)
        {
            switch (flag)
            {
                case "NONE":
                    return "⇼";
                case "TripleUp":
                    return "⤊";
                case "DoubleUp":
                    return "⇈";
                case "SingleUp":
                    return "↑";
                case "FortyFiveUp":
                    return "↗";
                case "Flat":
                    return "→";
                case "FortyFiveDown":
                    return "↘";
                case "SingleDown":
                    return "↓";
                case "DoubleDown":
                    return "⇊";
                case "TripleDown":
                    return "⤋";
                case "NOT COMPUTABLE":
                    return "-";
                case "RATE OUT OF RANGE":
                    return "⇕";
                default:
                    return "未知";
            }
        }
    }
}