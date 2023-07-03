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
        public async Task<String> CodeLogin(string code)
        {
            var appid = configuration.GetValue<string>("appid");
            var secret = configuration.GetValue<string>("secret");
            HttpClient httpClient = new HttpClient();
            var data = await httpClient.GetStringAsync($"https://api.weixin.qq.com/sns/jscode2session?appid={appid}&secret={secret}&js_code={code}&grant_type=authorization_code");
            return data;
        }


        [HttpGet]
        public async Task<String> Get(string serviceName)
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
            var result = (await collection.FindAsync(filter, find)).FirstOrDefault();
            return "";
            
        }
    }
}