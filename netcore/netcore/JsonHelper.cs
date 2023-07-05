using System;
using System.Collections.Generic;


public class JsonHelper
{
    /// <summary>
    /// 对象序列化
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="isUseTextJson">是否使用textjson</param>
    /// <returns>返回json字符串</returns>
    public static string ObjToJson(object obj, bool isUseTextJson = false)
    {
        if (isUseTextJson)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
        else
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
    }
    /// <summary>
    /// json反序列化obj
    /// </summary>
    /// <typeparam name="T">反序列类型</typeparam>
    /// <param name="strJson">json</param>
    /// <param name="isUseTextJson">是否使用textjson</param>
    /// <returns>返回对象</returns>
    public static T JsonToObj<T>(string strJson, bool isUseTextJson = false)
    {
        if (isUseTextJson)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(strJson);
        }
        else
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(strJson);
        }
    }
}

