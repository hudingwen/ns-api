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
        public async Task<MessageModel<SugarDTO>> GetCurBloodSugar(string serviceName)
        {
            SugarDTO sugarDTO = new SugarDTO();
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


            var ls = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(900).Project(projection).ToListAsync();


            var sugers = JsonHelper.JsonToObj<List<EntriesEntity>>(ls.ToJson());

            foreach (var item in sugers)
            {
                FormatDate(item);
            }

            if (sugers.Count > 0)
            {
                sugarDTO.curBlood = sugers[0];

                {
                    //今天
                    var flagDate = DateTime.Now.Date;
                    sugarDTO.day0 = HandleSugarList(sugers, flagDate);
                    
                }
                {
                    //昨天
                    var flagDate = DateTime.Now.Date.AddDays(-1);
                    sugarDTO.day1 = HandleSugarList(sugers, flagDate);
                    
                }
                {
                    //前天
                    var flagDate = DateTime.Now.Date.AddDays(-2);
                    sugarDTO.day2 = HandleSugarList(sugers, flagDate);
                    
                }
            }
            return MessageModel<SugarDTO>.Success("", sugarDTO);
            //var Host = configuration.GetValue<string>("Host");
            //var Port = configuration.GetValue<string>("Port");
            //var LoginName = configuration.GetValue<string>("LoginName");
            //var LoginPasswd = configuration.GetValue<string>("LoginPasswd");

            //var grantConnectionMongoString = $"mongodb://{LoginName}:{LoginPasswd}@{Host}:{Port}";
            //var client = new MongoClient(grantConnectionMongoString);
            //var database = client.GetDatabase(serviceName);
            //var collection = database.GetCollection<BsonDocument>("entries"); // 替换为你的集合名称
            //var find = new MongoDB.Driver.FindOptions<BsonDocument>
            //{
            //    Sort = Builders<BsonDocument>.Sort.Descending("_id"), // 根据 _id 字段倒序排序
            //    Limit = 1 // 只获取一条数据
            //};
            //var filter = Builders<BsonDocument>.Filter.Empty; // 获取所有数据
            ////var filter = Builders<BsonDocument>.Filter.Eq("fieldName", "fieldValue"); // 构建查询条件

            //var projection = Builders<BsonDocument>.Projection.Include("date").Include("sgv").Include("direction").Exclude("_id");
            //var result = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(1).Project(projection).FirstOrDefaultAsync();
            //var txt = result.ToJson();
            //var dd = JsonHelper.JsonToObj<EntriesEntity>(txt);
            //FormatDate(dd);

            //return MessageModel<EntriesEntity>.Success("", dd);

        }

        private  List<EntriesEntity> HandleSugarList(List<EntriesEntity> sugers, DateTime flagDate)
        {
            List<EntriesEntity> flagList = sugers.Where(t => t.date_now.Date == flagDate).ToList();
            //flagList.Reverse();
            
            var ls = new List<EntriesEntity>();
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(0).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(3).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(6).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(9).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(12).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(15).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(18).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(21).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            ls.Add(new EntriesEntity { date = (decimal)(flagDate.AddHours(23).AddHours(-8) - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds, isMask = true });
            foreach (var item in ls)
            {
                FormatDate(item);
            }
            flagList.AddRange(ls);
            return flagList.AsEnumerable().OrderBy(s => s.date_now).ToList();
        }

        [HttpGet]
        [Route("GetBloodSugars")]
        public async Task<MessageModel<List<EntriesEntity>>> GetBloodSugars(string serviceName,int day)
        {
            return null;
            //var Host = configuration.GetValue<string>("Host");
            //var Port = configuration.GetValue<string>("Port");
            //var LoginName = configuration.GetValue<string>("LoginName");
            //var LoginPasswd = configuration.GetValue<string>("LoginPasswd");

            //var grantConnectionMongoString = $"mongodb://{LoginName}:{LoginPasswd}@{Host}:{Port}";
            //var client = new MongoClient(grantConnectionMongoString);
            //var database = client.GetDatabase(serviceName);
            //var collection = database.GetCollection<BsonDocument>("entries"); // 替换为你的集合名称

            //var filter = Builders<BsonDocument>.Filter.Empty; // 获取所有数据
            //var projection = Builders<BsonDocument>.Projection.Include("date").Include("sgv").Include("direction").Exclude("_id");


            //var ls = await collection.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("_id")).Limit(900).Project(projection).ToListAsync();
            //var sugers = ls.ToJson();

            //var dd = JsonHelper.JsonToObj<List<EntriesEntity>>(sugers);

            //foreach (var item in dd)
            //{
            //    FormatDate(item);
            //}
            //var flagDate = DateTime.Now.Date.AddDays(day);
            //dd = dd.Where(t => t.date_now.Date == flagDate).ToList();

            //dd.Reverse();
            //if (dd.Count > 0)
            //{
            //    var last = dd[dd.Count - 1];
            //    if (last.date_now.Hour <= 20)
            //    {
            //        var curHour = last.date_now.Hour;
            //        while (true)
            //        {
            //            curHour += 3;
            //            if (curHour > 23) break;
            //            dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
            //        }
            //    }
            //    else if (last.date_now.Hour >= 21 && last.date_now.Hour <= 23)
            //    {
            //        var curHour = last.date_now.Hour;
            //        while (true)
            //        {
            //            curHour += 1;
            //            if (curHour > 23) break;
            //            if(curHour == 23 && last.date_now.Hour==22)
            //            {
            //                for (int i = 0; i < 10; i++)
            //                {
            //                    dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
            //                }
            //            }else if (curHour == 23 && last.date_now.Hour == 21)
            //            {
            //                for (int i = 0; i < 5; i++)
            //                {
            //                    dd.Add(new EntriesEntity { sgv_str = null, date_str = last.date_now.Date.AddHours(curHour).ToString("yyyy-MM-dd HH:mm:ss") });
            //                }
            //            }
            //        }
            //    }
            //}
            //return MessageModel<List<EntriesEntity>>.Success("", dd);

        }


        private void FormatDate(EntriesEntity dd)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)dd.date);
            DateTime dateTime = dateTimeOffset.UtcDateTime.AddHours(8);
            dd.date_now = dateTime;
            dd.date_str = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
            dd.date_time = dateTime.ToString("HH:mm:ss");

            dd.date_step = (int)(DateTime.Now - dateTime).TotalMinutes;

            if(dd.sgv != null)
                dd.sgv_str = Math.Round(1.0 * dd.sgv.Value / 18, 1);


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