#r "Newtonsoft.Json"
using Newtonsoft.Json;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage request, TraceWriter log)
{
    log.Info("DetermineLanguage function processed a request.");
    dynamic bodyData = await request.Content.ReadAsAsync<object>();
    using (HttpClient client = new HttpClient())
    {
        client.BaseAddress = new Uri(ConfigurationManager.AppSettings["EndpointUrl"] + "/", UriKind.Absolute);
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ConfigurationManager.AppSettings["EndpointKey"]);
        string postBody = JsonConvert.SerializeObject(
            new
            {
                Documents = new[]
                {
                    new
                    {
                        Id = Guid.NewGuid().ToString().ToLower(),
                        Text = bodyData.text
                    }
                }
            }
        );
        HttpResponseMessage response = await client.PostAsync($"languages", new StringContent(postBody, Encoding.UTF8, "application/json"));
        dynamic result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
        string detectedLanguage = result.documents[0].detectedLanguages[0].iso6391Name.ToObject<string>();
        return request.CreateResponse(
            HttpStatusCode.OK, 
            new 
            {
                language = detectedLanguage,
                text = bodyData.text
            }
        );
    }
}
