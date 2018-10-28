using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Swarm.Basic;
using Swarm.Basic.Entity;

namespace Swarm.Client
{
    /// <summary>
    /// Scheduler.NET Api
    /// </summary>
    public class SwarmApi
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly string _host;
        private readonly string _version;
        private readonly string _token;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="url">Scheduler.NET 服务地址</param>
        /// <param name="token">访问的 Token</param>
        /// <param name="version">Api 版本</param>
        public SwarmApi(string url, string token = null, string version = "v1.0")
        {
            _host = new Uri(url).ToString();
            _version = version;
            _token = token;
        }

        /// <summary>
        /// 创建普通任务
        /// </summary>
        /// <param name="job">任务信息</param>
        /// <param name="cron"></param>
        /// <returns>任务编号</returns>
        public async Task<ApiResult> Create(Job job, IDictionary<string, string> properties)
        {
            var query = string.Join("&", properties.Select(p => $"{p.Key.ToLower()}={p.Value}"));
            var url = $"{_host}swarm/{_version}/job?{query}";
            var msg = new HttpRequestMessage(HttpMethod.Post, url);
            AddAccessTokenHeader(msg);
            msg.Content = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json");
            var response = await HttpClient.SendAsync(msg);
            return await CheckResult(response);
        }


        /// <summary>
        /// 更新普通任务
        /// </summary>
        /// <param name="job">任务</param>
        public async Task<ApiResult> Update(Job job, IDictionary<string, string> properties)
        {
            var query = string.Join("&", properties.Select(p => $"{p.Key.ToLower()}={p.Value}"));
            var url = $"{_host}swarm/{_version}/job?{query}";
            var msg = new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(job), Encoding.UTF8, "application/json")
            };
            AddAccessTokenHeader(msg);
            var response = await HttpClient.SendAsync(msg);
            return await CheckResult(response);
        }

        /// <summary>
        /// 删除普通任务
        /// </summary>
        /// <param name="id">任务编号</param>
        public async Task<ApiResult> Delete(string id)
        {
            var url = $"{_host}swarm/{_version}/job?id={id}";
            var msg = new HttpRequestMessage(HttpMethod.Delete, url);
            AddAccessTokenHeader(msg);
            var response = await HttpClient.SendAsync(msg);
            return await CheckResult(response);
        }

        /// <summary>
        /// 触发普通任务
        /// </summary>
        /// <param name="id">任务编号</param>
        public async Task<ApiResult> Trigger(string id)
        {
            var url = $"{_host}swarm/{_version}/job?id={id}";
            var msg = new HttpRequestMessage(HttpMethod.Post, url);
            AddAccessTokenHeader(msg);
            var response = await HttpClient.SendAsync(msg);
            return await CheckResult(response);
        }

        private async Task<ApiResult> CheckResult(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            var str = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResult>(str);
            if (result.Code != ApiResult.SuccessCode)
            {
                throw new SwarmClientException(result.Msg);
            }

            return result;
        }

        private void AddAccessTokenHeader(HttpRequestMessage msg)
        {
            if (!string.IsNullOrWhiteSpace(_token))
            {
                msg.Headers.Add(SwarmConts.AccessTokenHeader, _token);
            }
        }
    }
}