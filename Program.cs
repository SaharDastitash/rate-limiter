using System;
using System.Collections.Generic;

namespace RateLimiterConsole
{
    // ** Developer Name: Sahar Dastitash - Date: 02/23/21 10:36 a.m. **
    // ** Rate-limiting pattern **

    // Rate limiting involves restricting the number of requests that can be made by a client.
    // A client is identified with an access token, which is used for every request to a resource.
    // To prevent abuse of the server, APIs enforce rate-limiting techniques.
    // Based on the client, the rate-limiting application can decide whether to allow the request to go through or not.
    // The client makes an API call to a particular resource; the server checks whether the request for this client is within the limit.
    // If the request is within the limit, then the request goes through.
    // Otherwise, the API call is restricted.
    class MainClass
    {
        // Use simple in-memory data structures to store the data; don't rely on a particular database.
        public static Dictionary<string, DateTime> LastCallTimeForClients = new Dictionary<string, DateTime>();
        public static Dictionary<string, DateTime> FirstCallTimeForClientsInTimeSpan = new Dictionary<string, DateTime>();
        public static Dictionary<string, int> CountCallsForClientsInTimspan = new Dictionary<string, int>();

        // a class library with a set of tests is more than enough.
        // Don't worry about the API itself, including auth token generation - there is no real API environment required.
        public static void Main(string[] args)
        {
            // *** Test Case 1: Allows 3 requests in 60 seconds and then 120 seconds passed since last call for US-based tokens *** //
            string authToken1 = "US-abcd123";
            // try the first 4 request in 60 seconds
            CallAPI(authToken1);
            Console.WriteLine();
            CallAPI(authToken1);
            Console.WriteLine();
            CallAPI(authToken1);
            Console.WriteLine();
            CallAPI(authToken1); // This call will show error since token already made 3 calls within 60 seconds
            Console.WriteLine();

            //System.Threading.Thread.Sleep(1200000);// wait for 120 second to make it or not it will show error
            CallAPI(authToken1);
            Console.WriteLine();
            // *** /Test Case 1 *** //

            // *** Test Case 2: Allows a request 60 seconds passed since last call for EU-based tokens *** //
            string authToken2 = "EU-jhte456";
            CallAPI(authToken2);
            Console.WriteLine(); // try this call within 60 seconds from first call, it will show error
            CallAPI(authToken2);
            Console.WriteLine();
            CallAPI(authToken2); // try this call after 60 seconds from last call, it will go through
            Console.WriteLine();
            // *** /Test Case 2 *** //
            Console.ReadLine();
        }

        //For simplicity, you can implement the API resource as a simple C# method accepting a user token,
        //and at the very beginning of the method, you set up your classes and ask whether further execution is allowed for this particular callee.
        public static void CallAPI(string AuthToken)
        {
            TimeSpan timeSpanPastSinceLastCall = TimeSpan.FromSeconds(1);
            TimeSpan allowedTimeSpan = TimeSpan.FromSeconds(60);
            int allowedrequestCountsPerTimespan = 0;
            int countCalls = 0;

            if (AuthToken.Contains("US"))
            {
                timeSpanPastSinceLastCall = TimeSpan.FromSeconds(120);
                allowedTimeSpan = TimeSpan.FromSeconds(60);
                allowedrequestCountsPerTimespan = 3;

                if (CountCallsForClientsInTimspan.ContainsKey(AuthToken))
                {
                    countCalls = CountCallsForClientsInTimspan[AuthToken];
                }
            }
            else if (AuthToken.Contains("EU"))
            {
                timeSpanPastSinceLastCall = TimeSpan.FromSeconds(60);
            }

            RateLimiter client = new RateLimiter(AuthToken, timeSpanPastSinceLastCall, allowedTimeSpan, allowedrequestCountsPerTimespan);
            Console.WriteLine("This is the client's Auth Token: " + client.AuthToken);
            DateTime now = DateTime.Now;

            if (AuthToken.Contains("US"))
            {
                if (client.ReachedMaxReuestPerTimeSpan(now) && !client.ReachedTimeSpanPassedSinceLastCall(now))
                {
                    Console.WriteLine("The API call is restricted for the client with Access Token:" + client.AuthToken);
                    Console.WriteLine("Total request calls: " + CountCallsForClientsInTimspan[client.AuthToken]);
                    Console.WriteLine("First request time: " + FirstCallTimeForClientsInTimeSpan[client.AuthToken]);
                    Console.WriteLine("Expected wait time after 3rd request: 120 seconds. Time Now: " + now);
                    Console.WriteLine("Last request time: " + LastCallTimeForClients[client.AuthToken]);
                }
                else if (!client.ReachedMaxReuestPerTimeSpan(now) ||
                    (client.ReachedMaxReuestPerTimeSpan(now) && client.ReachedTimeSpanPassedSinceLastCall(now)))
                {
                    client.AddRequest(now, countCalls);
                    //Do API logic and return API response in JSON,...
                    Console.WriteLine("API response for request at " + now);
                }
            }
            else if (AuthToken.Contains("EU"))
            {
                if (LastCallTimeForClients.ContainsKey(client.AuthToken) && !client.ReachedTimeSpanPassedSinceLastCall(now))
                {
                    Console.WriteLine("The API call is restricted for the client with Access Token:" + client.AuthToken);
                    Console.WriteLine("Expected wait time after last request: 60 seconds. Time Now: " + now);
                    Console.WriteLine("Last request time: " + LastCallTimeForClients[client.AuthToken]);
                }
                else if ((LastCallTimeForClients.ContainsKey(client.AuthToken) && client.ReachedTimeSpanPassedSinceLastCall(now)) ||
                    !LastCallTimeForClients.ContainsKey(client.AuthToken))
                {
                    client.AddRequest(now);
                    //Do API logic and return API response in JSON,...
                    Console.WriteLine("API response for request at " + now);
                }
            }
        }

        // The goal is to design a class(-es) that manage rate limits for every provided API resource by a set of provided *configurable and extendable* rules.
        // For example, for one resource you could configure the limiter to use Rule A,
        // for another one - Rule B, for a third one - both A + B, etc. Any combinations of rules are possible, keep this fact in mind when designing the classes.
        public class RateLimiter
        {
            //Some examples of request-limiting rules(you could imagine any others)
            //* X requests per timespan; 
            //* a certain timespan passed since the last call;
            //* for US-based tokens, we use X requests per timespan, for EU-based - certain timespan passed since the last call.
            private string _authToken;
            public string AuthToken
            {
                get
                {
                    return _authToken;
                }
                set
                {
                    _authToken = value;
                }
            }

            private int _allowedrequestCountsPerTimespan;
            private int _clientRequestsCountInTimespan;
            private TimeSpan _timeSpanPastSinceLastCall;
            private TimeSpan _allowedTimeSpan;
            private DateTime _lastCallTime;
            private DateTime _firstCallTime;

            // Initializing the parameters in the constructor
            public RateLimiter(string authToken, TimeSpan timeSpanPastSinceLastCall, TimeSpan allowedTimeSpan, int AllowedrequestCountsPerTimespan)
            {
                this._authToken = authToken;

                // parameters for X request per timespan
                this._allowedrequestCountsPerTimespan = AllowedrequestCountsPerTimespan;

                if (CountCallsForClientsInTimspan.ContainsKey(AuthToken))
                {
                    this._clientRequestsCountInTimespan = CountCallsForClientsInTimspan[AuthToken];
                }

                if (FirstCallTimeForClientsInTimeSpan.ContainsKey(AuthToken))
                {
                    this._firstCallTime = FirstCallTimeForClientsInTimeSpan[AuthToken];
                }

                this._allowedTimeSpan = allowedTimeSpan;


                // parameters for a certain timespan passed since the last call;
                this._timeSpanPastSinceLastCall = timeSpanPastSinceLastCall;

                if (LastCallTimeForClients.ContainsKey(AuthToken))
                {
                    this._lastCallTime = LastCallTimeForClients[AuthToken];
                }
            }

            // ** Rule 1 **//
            public bool ReachedMaxReuestPerTimeSpan(DateTime now)
            {
                bool IsRestricted = false;

                if (this._firstCallTime == new DateTime(0001, 1, 1))
                {
                    this._firstCallTime = now;
                }

                if (this._clientRequestsCountInTimespan == this._allowedrequestCountsPerTimespan &&
                    now - this._firstCallTime < this._allowedTimeSpan)
                {
                    IsRestricted = true;
                }
                else if (this._clientRequestsCountInTimespan == this._allowedrequestCountsPerTimespan && now - this._firstCallTime > this._allowedTimeSpan)
                {
                    IsRestricted = true;
                }
                else if (now - this._firstCallTime > this._allowedTimeSpan)
                {
                    CountCallsForClientsInTimspan[AuthToken] = 0;

                    if (FirstCallTimeForClientsInTimeSpan.ContainsKey(AuthToken))
                    {
                        FirstCallTimeForClientsInTimeSpan[AuthToken] = now;
                    }
                    else
                    {
                        FirstCallTimeForClientsInTimeSpan.Add(AuthToken, now);
                    }
                }

                return IsRestricted;
            }

            // ** Rule 2 **//
            public bool ReachedTimeSpanPassedSinceLastCall(DateTime now)
            {
                bool ReachedTimeSpan = false;

                if (this._lastCallTime == new DateTime(0001, 1, 1))
                {
                    this._lastCallTime = now;
                }

                if (now - this._lastCallTime > this._timeSpanPastSinceLastCall)
                {
                    ReachedTimeSpan = true;
                }

                return ReachedTimeSpan;
            }

            public void AddRequest(DateTime now, int count)
            {
                if (CountCallsForClientsInTimspan.ContainsKey(AuthToken))
                {
                    CountCallsForClientsInTimspan[AuthToken] = count + 1;
                    LastCallTimeForClients[AuthToken] = now;
                }
                else
                {
                    CountCallsForClientsInTimspan.Add(AuthToken, 1);
                    FirstCallTimeForClientsInTimeSpan.Add(AuthToken, now);
                    LastCallTimeForClients.Add(AuthToken, now);
                }
            }

            public void AddRequest(DateTime now)
            {
                if (LastCallTimeForClients.ContainsKey(AuthToken))
                {
                    LastCallTimeForClients[AuthToken] = now;
                }
                else
                {
                    LastCallTimeForClients.Add(AuthToken, now);
                }
            }
        }
    }
}
