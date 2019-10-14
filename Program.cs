using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CoreTweet;
using DevUpTweet.Properties;

namespace DevUpTweet
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            
            //uncomment this to clear user settings and reauthorize
            //Properties.Settings1.Default.Reset();

            //load a the twitter application settings to be used later, these come from a user settings file, and will be blank by default
            var consumerKey = Settings1.Default["consumerKey"].ToString();
            var consumerSecret = Settings1.Default["consumerSecret"].ToString();
            

            //if we don't have these stores yet, get them from the console, these are stored in a user settings file for subsequent calls
            if (consumerKey == string.Empty || consumerSecret == string.Empty)
            {
                //we need to get these from our application in twitter
                Console.WriteLine("Enter your consumerKey:");
                consumerKey = Console.ReadLine();

                Console.WriteLine("Enter your consumerSecret:");
                consumerSecret = Console.ReadLine();
            }

            //load the rest of the settings
            var session = OAuth.Authorize(consumerKey, consumerSecret);
            var accessToken = Settings1.Default["accessToken"].ToString();
            var accessSecret = Settings1.Default["accessSecret"].ToString();
            long userId = (long)Settings1.Default["userID"];
            var screenName = Settings1.Default["screenName"].ToString();
            
            Tokens tokens = new Tokens();
            
            if (consumerKey != string.Empty && consumerSecret != string.Empty && accessToken != string.Empty && accessSecret != string.Empty)
            {
                //if we already have the settings, let's create the auth token
                //Create(string consumerKey, string consumerSecret, string accessToken, string accessSecret, long userID = 0, string screenName = null);
                tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessSecret, userId, screenName);
            }
            else
            {
                //If we don't have these settings populated already we need to go authenticate the user with twitter and get a pin code
                //need to authenticate, and then get a pin code from Twitter to store/use
                System.Diagnostics.Process.Start(session.AuthorizeUri.AbsoluteUri); //this will open up a URL for the user to login/authorize the app
                Console.WriteLine("@DevUpBot Enter the Pin code from Twitter");
                string pin = Console.ReadLine();

                tokens = session.GetTokens(pin);
                //save the token values to our settings
                Settings1.Default["consumerKey"] = tokens.ConsumerKey;
                Settings1.Default["consumerSecret"] = tokens.ConsumerSecret;
                Settings1.Default["accessToken"] = tokens.AccessToken;
                Settings1.Default["accessSecret"] = tokens.AccessTokenSecret;
                Settings1.Default["userID"] = tokens.UserId;
                Settings1.Default["screenName"] = tokens.ScreenName;
                Settings1.Default.Save();
            }

            //now let's look at getting the bot doing something
            Console.WriteLine("Tweet History:");

            //we don't want to retweet or reply to tweets that we've already touched, so we do that by keeping track of our last time
            DateTime lastTweetTime = DateTime.Now.AddMinutes(-5);
            DateTime lastRetweetTime = DateTime.Now.AddMinutes(-5);


            if (Settings1.Default["lastTweetTime"].ToString() != string.Empty)
            {
                lastTweetTime = Convert.ToDateTime(Settings1.Default["lastTweetTime"]);
            }

            if (Settings1.Default["lastRetweetTime"].ToString() != string.Empty)
            {
                lastRetweetTime = Convert.ToDateTime(Settings1.Default["lastRetweetTime"]);
            }

            long lastTweetId = (long)Settings1.Default["lastTweetId"]; // 921273009989672960;

            long lastRetweetId = (long)Settings1.Default["lastRetweetId"]; // 921273009989672960;

            //This is where you need to start being careful of how often you do things, don't abuse! This thing will run until you kill the console app with Control-C
            do
            {
                try
                {
                    //check every minute to see if there is something new
                    if (DateTime.Now >= lastTweetTime.AddMinutes(1))
                    {
                        //list of strings to add to the replies
                        var listYes = new List<string> { "Enjoy the conference @{0}!", "Will you have fun @{0}?", "What do you think you will like best?", "Is this your first #DevUpConf?"
                    , "@{0} Have fun!"
                };

                        int index = new Random().Next(listYes.Count);
                        var status = listYes[index]; //randomize what text to use as a reply

                        //look for tweets with devup2019 in the body, but ignore any that have some bad keywords in them
                        var res = tokens.Search.Tweets("\"devup2019\" -kill -death -suicide -shoot -stab -kms -die -jump", null, null, null, null, null, null, lastTweetId, null, null, null, null);
                        foreach (Status r in res.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                            //TODO: change screenname == christoc to != devupbot before conference
                            if (r.Id != lastTweetId && r.User.ScreenName != "devupbot" && r.RetweetedStatus == null)
                            {
                                lastTweetId = r.Id;
                                Settings1.Default["lastTweetId"] = lastTweetId;
                                Settings1.Default.Save();
                                status = string.Format(status, r.User.ScreenName);
                                Status s = tokens.Statuses.Update(
                                    status: status
                                    , in_reply_to_status_id: lastTweetId
                                );

                                Console.WriteLine("Reply to tweet from:" + r.User.ScreenName);

                                break;
                            }
                        }

                        //store the last tweet time so we don't reply to that again in the future
                        lastTweetTime = DateTime.Now;
                        Settings1.Default["lastTweetTime"] = lastTweetTime.ToString();
                        Settings1.Default.Save();
                        Console.WriteLine("Last reply time: " + lastTweetTime.ToString());

                        //let's look at retweeting #devupconf posts
                        res = tokens.Search.Tweets("\"devup2019\" -kill -death -suicide -shoot -stab -kms -die -jump", null, null, null, null, null, null, lastRetweetId, null, null, null, null);
                        foreach (Status r in res.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                            //TODO: change screenname == christoc to != devupbot before conference
                            if (r.Id != lastRetweetId && r.User.ScreenName != "devupbot" && r.RetweetedStatus == null)
                            {
                                lastRetweetId = r.Id;
                                Settings1.Default["lastRetweetId"] = lastRetweetId;
                                Settings1.Default.Save();
                                status = string.Format(status, r.User.ScreenName);

                                Status rt = tokens.Statuses.Retweet(r.Id,false,false);

                                Console.WriteLine("Retweet of:" + r.User.ScreenName);

                                break;
                            }
                        }


                        lastRetweetTime = DateTime.Now;
                        Settings1.Default["lastRetweetTime"] = lastRetweetTime.ToString();
                        Settings1.Default.Save();
                        Console.WriteLine("Last retweet time: " + lastRetweetTime.ToString());
                        

                    }
                }
                catch (Exception ex)
                {
                    //if there was an error, write it to the screen, save the last tweet time and continue the do/while so that app doesn't completely crash.
                    Console.WriteLine(ex.InnerException);
                    Console.WriteLine(ex.Message);
                    lastTweetTime = DateTime.Now;
                    Settings1.Default["lastTweetTime"] = lastTweetTime.ToString();
                    Settings1.Default.Save();

                    Console.WriteLine("Error at:" + lastTweetTime.ToString());
                }
            } while (true);
        }
    }
}
