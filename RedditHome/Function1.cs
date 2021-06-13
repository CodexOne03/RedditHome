using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Alexa.NET.Request.Type;
using Alexa.NET;
using Reddit;

namespace RedditHome
{
    public static class Function1
    {
        static RedditClient reddit = new RedditClient("CqclYbmQ9HhZmQ", "181668065546-O_pxBB20ItJS-raoehHr4ZW5E4M");

        [FunctionName("reddit-home-skill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string json = await req.ReadAsStringAsync();
            var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);
            return ProcessRequest(skillRequest);
        }
        private static IActionResult ProcessRequest(SkillRequest skillRequest)
        {
            var requestType = skillRequest.GetRequestType();
            SkillResponse response = null;
            if (requestType == typeof(LaunchRequest))
            {
                response = ResponseBuilder.Tell("Benvenuto in Reddit Home!");
                response.Response.ShouldEndSession = false;
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = skillRequest.Request as IntentRequest;
                if (intentRequest.Intent.Name == "ReadRandomPost")
                {
                    var subredditName = intentRequest.Intent.Slots["subreddit"].Value;
                    var subreddit = reddit.Subreddit(subredditName);
                    Random random = new Random();

                    var posts = subreddit.Posts.Hot;
                    var post = posts[random.Next(0, posts.Count)];
                    var title = Translator.Program.TranslateTextRequest(post.Title).Result;

                    var comments = post.Comments.Top;
                    var randomComment = comments[random.Next(0, comments.Count)];
                    var comment = Translator.Program.TranslateTextRequest(randomComment.Body).Result;

                    if (title.Contains('?'))
                    {
                        if (comment.Contains('?') || comment.Contains('.'))
                        {
                            response = ResponseBuilder.Tell(post.Author + " chiede: " + title + " " + randomComment.Author + " ha risposto: " + comment);
                        }
                        else
                        {
                            response = ResponseBuilder.Tell(post.Author + " chiede: " + title + " " + randomComment.Author + " ha risposto: " + comment + ".");
                        }
                    }
                    if (title.Contains('.'))
                    {
                        response = ResponseBuilder.Tell(post.Author + " dice: " + title);
                    }
                    if (!title.Contains('.') && !title.Contains('?'))
                    {
                        response = ResponseBuilder.Tell("Non riesco a leggere il post che ho trovato.");
                    }
                    response.Response.ShouldEndSession = false;
                }
            }
            else if (requestType == typeof(SessionEndedRequest))
            {
                response = ResponseBuilder.Tell("Arrivederci!");
                response.Response.ShouldEndSession = true;
            }
            return new OkObjectResult(response);
        }
    }
}
