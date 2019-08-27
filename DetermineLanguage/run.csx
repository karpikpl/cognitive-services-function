#r "Newtonsoft.Json"
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

public static async Task<IActionResult> Run (HttpRequest request, ILogger log) {
    log.LogInformation ("DetermineLanguage function processed a request.");

    try {
        string requestBody = await new StreamReader (request.Body).ReadToEndAsync ();
        dynamic bodyData = JsonConvert.DeserializeObject (requestBody);
        using (HttpClient client = new HttpClient ()) {
            client.BaseAddress = new Uri (System.Environment.GetEnvironmentVariable ("EndpointUrl", EnvironmentVariableTarget.Process) + "/", UriKind.Absolute);
            client.DefaultRequestHeaders.Add ("Ocp-Apim-Subscription-Key", System.Environment.GetEnvironmentVariable ("EndpointKey", EnvironmentVariableTarget.Process));
            log.LogInformation ("Calling service at " + client.BaseAddress);
            log.LogInformation ("Determining language for " + bodyData.text as string);

            string postBody = JsonConvert.SerializeObject (
                new {
                    Documents = new [] {
                        new {
                            Id = Guid.NewGuid ().ToString ().ToLower (),
                            Text = bodyData.text
                        }
                    }
                }
            );
            log.LogInformation ("Ready to send: " + postBody);

            HttpResponseMessage response = await client.PostAsync ($"languages", new StringContent (postBody, Encoding.UTF8, "application/json"));
            var responseString = await response.Content.ReadAsStringAsync ();
            log.LogInformation ("Response from cognitive services: " + responseString);
            dynamic result = JsonConvert.DeserializeObject (responseString);
            string detectedLanguage = result.documents[0].detectedLanguages[0].iso6391Name.ToObject<string> ();
            log.LogInformation ("The result is " + detectedLanguage + " for " + bodyData.text as string);

            return new OkObjectResult (
                new {
                    language = detectedLanguage,
                        text = bodyData.text as string
                }
            );
        }
    } catch (System.Exception ex) {
        log.LogError (ex, "ups");
        throw;
    }
}
