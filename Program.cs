using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CoreTweet;
using DevUpTweet.Properties;

namespace DevUpTweet
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //TODO: replace these keys
            var consumerKey = Settings1.Default["consumerKey"].ToString();
            var consumerSecret = Settings1.Default["consumerSecret"].ToString();

            //if we don't have these stores yet, get them from the console
            if (consumerKey == string.Empty || consumerSecret == string.Empty)
            {
                Console.WriteLine("Enter your consumerKey:");
                consumerKey = Console.ReadLine();

                Console.WriteLine("Enter your consumerSecret:");
                consumerSecret = Console.ReadLine();

            }


            var session = OAuth.Authorize(consumerKey, consumerSecret);
            //Properties.Settings1.Default.Reset();
            //read from the local settings to see if we already have the authentication.
            var accessToken = Settings1.Default["accessToken"].ToString();
            var accessSecret = Settings1.Default["accessSecret"].ToString();
            long userID = (long)Settings1.Default["userID"];
            var screenName = Settings1.Default["screenName"].ToString();
            var strLastTweetSentTime = Settings1.Default["lastTweetSentTime"].ToString();


            Tokens tokens = new Tokens();

            if (consumerKey != string.Empty && consumerSecret != string.Empty && accessToken != string.Empty && accessSecret != string.Empty)
            {
                //if we already have the settings, let's create the auth token
                //Create(string consumerKey, string consumerSecret, string accessToken, string accessSecret, long userID = 0, string screenName = null);
                tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessSecret, userID, screenName);
            }
            else
            {
                //need to authenticate, and then get a pin code from Twitter to store/use
                System.Diagnostics.Process.Start(session.AuthorizeUri.AbsoluteUri);
                Console.WriteLine("@DevUpBot Enter the Pin code from Twitter");
                string pin = Console.ReadLine();
                tokens = session.GetTokens(pin);
                //save the token values
                Settings1.Default["consumerKey"] = tokens.ConsumerKey;
                Settings1.Default["consumerSecret"] = tokens.ConsumerSecret;
                Settings1.Default["accessToken"] = tokens.AccessToken;
                Settings1.Default["accessSecret"] = tokens.AccessTokenSecret;
                Settings1.Default["userID"] = tokens.UserId;
                Settings1.Default["screenName"] = tokens.ScreenName;
                Settings1.Default.Save();
            }

            //TODO: now let's look at getting the bot doing something

            Console.WriteLine("Tweet History:");

            //we don't want to retweet or reply to tweets that we've already touched, so we do that by keeping track of our last time
            DateTime lastTweetTime = DateTime.Now.AddMinutes(-5);
            DateTime lastTweetSentTime = DateTime.Now.AddMinutes(-5);

            if (Settings1.Default["lastTweetTime"].ToString() != string.Empty)
            {
                lastTweetTime = Convert.ToDateTime(Settings1.Default["lastTweetTime"]);
            }
            if (strLastTweetSentTime != string.Empty)
            {
                lastTweetSentTime = Convert.ToDateTime(strLastTweetSentTime);
            }

            long lastTweetId = (long)Settings1.Default["lastTweetId"]; // 921273009989672960;
            

            //This is where you need to start being careful of how often you do things
            if (DateTime.Now >= lastTweetTime.AddMinutes(1))
            {
                //list of strings to add to the replies
                var listYes = new List<string> { "@{0} enjoy the conference!", "@{0} will you have fun?", "@{0} what do you think you will like best?", "@{0} is this your first #DevUpConf?"
                    , "@{0} have fun!"
                };

                int index = new Random().Next(listYes.Count);
                var status = listYes[index];
                
                var res = tokens.Search.Tweets("\"devup2019\" -kill -death -suicide -shoot -stab -kms -die -jump", null, null, null, null, null, null, lastTweetId, null, null, null, null);
                foreach (Status r in res.OrderBy(x => x.Id))
                {
                    //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                    //TODO: change screenname == christoc to != devupbot before conference
                    if (r.Id != lastTweetId && r.User.ScreenName == "christoc" && r.RetweetedStatus == null)
                    {
                        lastTweetId = r.Id;
                        Settings1.Default["lastTweetId"] = lastTweetId;
                        Settings1.Default.Save();
                        status = string.Format(status, r.User.ScreenName);
                        Status s = tokens.Statuses.Update(
                            status: status
                            , in_reply_to_status_id: lastTweetId
                        );

                        break;
                    }
                }


                lastTweetTime = DateTime.Now;
                Settings1.Default["lastTweetTime"] = lastTweetTime.ToString();
                Settings1.Default.Save();

                Console.WriteLine(lastTweetTime.ToString());

                //TODO: let's look at retweeting #devupconf posts
            }
            
        }
    }
}
