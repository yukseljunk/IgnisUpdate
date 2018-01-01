using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace IgnisUpdate
{
    class Program
    {
        public static int Interval
        {
            get
            {
                return Int32.Parse(GetConfig("pollingMs"));
            }
        }
        public static string coin
        {
            get
            {
                return GetConfig("coin");
            }
        }

        public static string publicKey
        {
            get
            {
                return GetConfig("publickey");
            }
        }

        public static string secretKey
        {
            get
            {
                return GetConfig("secretkey");
            }
        }

        private static string GetConfig(string key)
        {
            return System.Configuration.ConfigurationManager.AppSettings[key].ToString();
        }

        static void Main(string[] args)
        {
            decimal coinInTradingBalance = 0;
            decimal coinInAccountBalance = 0;
            while (true)
            {
                Console.WriteLine("Checking for {0} Balance...", coin);
                coinInAccountBalance = GetAccountBalance(coin);
                if (coinInAccountBalance != 0)
                {
                    WarnMe();
                    break;
                }

                coinInTradingBalance = GetTradingBalance(coin);
                if (coinInTradingBalance != 0)
                {
                    WarnMe();
                    break;
                }
                Console.WriteLine("-----------------------------");
                Console.WriteLine("Cannot find, retrying in {0} secs", Interval / 1000);
                Thread.Sleep(Interval);
            }
            if (coinInAccountBalance != 0)
            {
                Console.WriteLine("Moving to trading account...");
                for (int i = 0; i < 5; i++)
                {

                    var trId = MoveToTradingAccount(coin, coinInAccountBalance);
                    if (trId == null)
                    {
                        Console.WriteLine("Transfer Fail!");
                    }
                    else
                    {
                        Console.WriteLine("Transfer done : {0}", trId);
                        break;
                    }
                    Thread.Sleep(Interval);
                }
            }
            Console.ReadLine();
        }

        private static void WarnMe()
        {
            Console.Beep();
            for (int i = 100; i < 3000; i += 200)
            {
                Console.Beep(i, 1000);

            }
            Console.Beep();
        }

        private static string MoveToTradingAccount(string currency, decimal amount)
        {
            try
            {
                var res = MakeRequest("api/2/account/transfer", string.Format("currency={0}&amount={1}&type=bankToExchange", currency, amount.ToString(new CultureInfo("en-US"))));
                foreach (dynamic obj in res.First)
                {
                    return obj.First.ToString();
                }
                return null;
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                return null;
            }
        }

        private static decimal GetAccountBalance(string coin2Look4)
        {

            var objects = MakeRequest("api/2/account/balance"); // parse as array  
            foreach (dynamic obj in objects.First)
            {
                var cur = obj.currency;
                var available = obj.available;
                var reserved = obj.reserved;
                var av = Math.Round((decimal)available, 8);
                var rv = Math.Round((decimal)reserved, 8);

                if (av == 0 && rv == 0) { continue; }
                var x = obj;
                Console.WriteLine("{0}:{1}", cur, available);
                if (cur.ToString().Contains(coin2Look4)) return av;

            }
            return (decimal)0;
        }
        private static void GetAccountBalance()
        {

            var objects = MakeRequest("api/2/account/balance"); // parse as array  
            foreach (dynamic obj in objects.First)
            {
                var cur = obj.currency;
                var available = obj.available;
                var reserved = obj.reserved;
                var av = Math.Round((decimal)available, 8);
                var rv = Math.Round((decimal)reserved, 8);

                if (av == 0 && rv == 0) { continue; }
                var x = obj;
                Console.WriteLine("{0}:{1}", cur, available);

            }
        }

        private static decimal GetTradingBalance(string coin2Look4)
        {
            var objects = MakeRequest("api/2/trading/balance"); // parse as array  
            foreach (dynamic obj in objects.First)
            {
                var cur = obj.currency;
                var available = obj.available;
                var reserved = obj.reserved;
                if (available == 0 && reserved == 0) { continue; }
                var x = obj;
                Console.WriteLine("{0}:{1}", cur, available);
                if (cur.ToString().Contains(coin2Look4)) return Math.Round((decimal)available, 8);

            }
            return (decimal)0;
        }

        private static void GetTradingBalance()
        {
            var objects = MakeRequest("api/2/trading/balance"); // parse as array  
            foreach (dynamic obj in objects.First)
            {
                var cur = obj.currency;
                var available = obj.available;
                var reserved = obj.reserved;
                if (available == 0 && reserved == 0) { continue; }
                var x = obj;
                Console.WriteLine("{0}:{1}", cur, available);

            }
        }



        static JArray MakeRequest(string qs, string postdata = "")
        {
            string url = "https://api.hitbtc.com/" + qs;
            WebRequest myReq = WebRequest.Create(url);
            string credentials = string.Format("{0}:{1}", publicKey, secretKey);
            CredentialCache mycache = new CredentialCache();
            myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
            if (postdata != "")
            {
                myReq.Method = "POST";
                byte[] dataStream = Encoding.UTF8.GetBytes(postdata);
                myReq.ContentLength = dataStream.Length;

                using (Stream newStream = myReq.GetRequestStream())
                {
                    // Send the data.
                    newStream.Write(dataStream, 0, dataStream.Length);
                    newStream.Close();
                }
            }

            WebResponse wr = myReq.GetResponse();
            Stream receiveStream = wr.GetResponseStream();
            StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
            string content = reader.ReadToEnd();
            //  Console.WriteLine(content);
            var json = "[" + content + "]"; // change this to array
            return JArray.Parse(json); // parse as array  
        }
    }
}
