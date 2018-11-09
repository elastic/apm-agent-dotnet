using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Elastic.Agent.Core.Model.Payload;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

//TODO: It'd be nice to move this into the .csproj
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Agent.AspNetCore")]

namespace Elastic.Agent.Core.Report
{
    internal class PayloadSender
    {
        public String ServerUrlBase { get; set; } = "http://127.0.0.1:8200";
        public async Task SendPayload(Payload payload)
        {
            HttpClient httpClient = new HttpClient();
            string json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var result = await httpClient.PostAsync(ServerUrlBase + "/v1/transactions", content);

                var isSucc = result.IsSuccessStatusCode;
                var str = await result.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                //TODO: log
            }
        }
    }
}
