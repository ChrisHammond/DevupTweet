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
            Properties.Settings1.Default.Reset();

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

            Tokens tokens;

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
                Console.WriteLine("To Activate @DevUpBot Enter the Pin code from Twitter");
                string pin = Console.ReadLine();

                tokens = session.GetTokens(pin);
                //save the token values to our settings, when saved properly we don't have to reauthorize the app when starting it up again.
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

            //we don't want to retweet or reply to tweets that we've already touched,
            //so we do that by keeping track of our last time, in addition to later storing last tweet id
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

            long lastBadTagId = (long)Settings1.Default["lastBadTagId"]; // 921273009989672960;


            //This is where you need to start being careful of how often you do things, don't abuse!
            //This thing will run until you kill the console app with Control-C
            do
            {
                try
                {
                    //check every minute to see if there is something new
                    if (DateTime.Now >= lastTweetTime.AddMinutes(1))
                    {
                        //list of strings to add to the replies
                        var listYes = new List<string> { 
                            
                            "@{0} ProTip for exiting the garage: It is hard. Go the wrong way to get to the EXIT 3 rows from the west side" +
                            ", or go all the way west and cut across then back down the proper direction."
                            //, "@{0} Are you going to see @christoc's sessions? #DevUp2019"
                            //, "@{0} The closing keynote on Wednesday is at 3:45pm #DevUp2019"
                            //, "@{0} what did you think of @donasarkar's keynote?"
                            //, "@{0} you can learn more about me at 1pm Wednesday in the Success room #FTW #DevUp2019"
                            //, "@{0} any particular session you recommend?"
                            //, "@{0} did you learn anything life changing yet?"
                            , "@{0} I've got less than 12 hours before I'm shut down!"
                            , "@{0} Hope you had a great time!"
                            , "@{0} Will we see you here next year?"
                           
                            ///, "@{0} Today's the final day, enjoy!"
                            
                            //"@{0} Enjoy the conference", "@{0} Will you have fun?"
                            // , "@{0} What do you think you will like best?"
                            // , "@{0} Is this your first #DevUp2019?"
                            // , "@{0} Don't have too much fun! #DevUp2019?"
                            // , "@{0} The Keynote on Tuesday is at 8am #DevUp2019"
                            // , "@{0} Are you excited about any particular session #DevUp2019?"
                            // , "@{0} Enjoy St. Charles! #DevUp2019"
                            // , "@{0} Have fun!"
                            // , "@{0} Are you #ReadyDeveloperOne?"
                            // , "@{0} Do you remember when it was called Day of .Net? How about Days? #DevUp2019"
                            // , "@{0} Don't forget about the new entrance in parking garage on Level 3 #DevUp2019"

                };

                        //randomize what text to use as a reply
                        int index = new Random().Next(listYes.Count);
                        var status = listYes[index]; 

                        //look for tweets with devup2019 in the body, but ignore any that have some bad keywords in them
                        var res = tokens.Search.Tweets("\"devup2019\" -kill -death -suicide -shoot -stab -kms -die -jump"
                            , null, null, null, null
                            , null, null, lastTweetId
                            , null, null
                            , null
                            , null);
                        foreach (Status r in res.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                            if (r.Id != lastTweetId && r.User.ScreenName.ToLower() != "devupbot" && r.RetweetedStatus == null)
                            {

                                index = new Random().Next(listYes.Count);
                                status = listYes[index]; //randomize what text to use as a reply

                                lastTweetId = r.Id;
                                Settings1.Default["lastTweetId"] = lastTweetId;
                                Settings1.Default.Save();
                                status = string.Format(status, r.User.ScreenName);
                                tokens.Statuses.Update(
                                    status: status
                                    , in_reply_to_status_id: lastTweetId
                                );

                                Console.WriteLine("Reply to tweet from:" + r.User.ScreenName);

                             //   break;
                            }
                        }

                        //store the last tweet time so we don't reply to that again in the future
                        lastTweetTime = DateTime.Now;
                        Settings1.Default["lastTweetTime"] = lastTweetTime.ToString();
                        Settings1.Default.Save();
                        Console.WriteLine("Last reply time: " + lastTweetTime);

                        #region BadHashtag

                        //call out the wrong hashtag
                        var resTag = tokens.Search.Tweets("\"#devupconf\" " +
                                                          "-kill -death -suicide -shoot -stab -kms -die -jump"
                            , null, null, null, null, null, 
                            null, lastBadTagId
                            
                            , null, null, null, null);
                        foreach (Status r in resTag.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                            if (r.Id != lastBadTagId && r.User.ScreenName.ToLower() != "devupbot" && r.RetweetedStatus == null)
                            {
                                var tweetText = "@{0} You might try the #DevUp2019 tag instead!";
                                lastBadTagId = r.Id;
                                Settings1.Default["lastBadTagId"] = lastBadTagId;
                                Settings1.Default.Save();
                                tweetText = string.Format(tweetText, r.User.ScreenName);
                                tokens.Statuses.Update(
                                    status: tweetText
                                    , in_reply_to_status_id: lastBadTagId
                                );

                                Console.WriteLine("Reply to tweet from:" + r.User.ScreenName + " TweetId: " + r.Id);

                                //break;
                            }
                        }
                        #endregion

                        #region Retweet code


                        //let's look at retweeting #DevUp2019 posts
                        res = tokens.Search.Tweets("\"devup2019\" -kill -death -suicide -shoot -stab -kms -die -jump", null, null, null, null, null, null, lastRetweetId, null, null, null, null);
                        foreach (Status r in res.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to ourselves
                            if (r.Id != lastRetweetId && r.User.ScreenName.ToLower() != "devupbot" && r.RetweetedStatus == null)
                            {
                                lastRetweetId = r.Id;
                                Settings1.Default["lastRetweetId"] = lastRetweetId;
                                Settings1.Default.Save();
                                //status = string.Format(status, r.User.ScreenName);

                                tokens.Statuses.Retweet(r.Id, false, false);

                                Console.WriteLine("Retweet of:" + r.User.ScreenName);

                                // break;
                            }
                        }

                        lastRetweetTime = DateTime.Now;
                        Settings1.Default["lastRetweetTime"] = lastRetweetTime.ToString();
                        Settings1.Default.Save();
                        Console.WriteLine("Last retweet time: " + lastRetweetTime);

                        #endregion
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
                    //update the last retweet time too just to be safe 
                    Settings1.Default["lastRetweetTime"] = lastTweetTime.ToString();
                    Settings1.Default.Save();

                    Console.WriteLine("Error at:" + lastTweetTime);
                }
            } while (true);
        }
    }
}
