using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentScheduler;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ListingsNotifier
{
    public class MyRegistry : Registry
    {
        // test test
        static bool debug = Convert.ToBoolean(Environment.GetEnvironmentVariable("debugmode"));

        public MyRegistry()
        {
            Action scheduleNotifications = new Action(() =>
            {
                Console.WriteLine($"Timed Task - Will run now {DateTime.Now}");
                try
                {
                    var t = Task.Run(() => Program.RunJob());
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            });

            //debug = false;

            if (debug == true)
            {
                //this.Schedule(scheduleNotifications).ToRunNow().AndEvery(15).Seconds();
                this.Schedule(scheduleNotifications).ToRunNow().AndEvery(1).Days().At(08, 00);
            }
            else
            {
                //this.Schedule(scheduleSnapshots).ToRunNow().AndEvery(1).Weeks().On(DayOfWeek.Monday).At(12, 30);
                //this.Schedule(scheduleSnapshots).ToRunNow().AndEvery(1).Hours();
                this.Schedule(scheduleNotifications).ToRunNow().AndEvery(1).Days().At(08, 00);
                //this.Schedule(scheduleNotifications).ToRunNow().AndEvery(15).Seconds();
            }
            #region // Other options for scheduling
            //this.Schedule(someMethod).ToRunEvery(1).Weeks().On(DayOfWeek.Monday).At(12, 30);

            // Schedule an IJob to run at an interval
            //Schedule<MyJob>().ToRunNow().AndEvery(2).Seconds();

            // Schedule an IJob to run once, delayed by a specific time interval
            //Schedule<MyJob>().ToRunOnceIn(5).Seconds();

            // Schedule a simple job to run at a specific time
            //Schedule(() => Console.WriteLine("It's 9:15 PM now.")).ToRunEvery(1).Days().At(21, 15);

            // Schedule a more complex action to run immediately and on an monthly interval
            //Schedule<MyJob>().ToRunNow().AndEvery(1).Months().OnTheFirst(DayOfWeek.Monday).At(3, 0);

            // Schedule multiple jobs to be run in a single schedule
            //Schedule<MyJob>().AndThen<MyJob>().ToRunNow().AndEvery(5).Minutes();
            #endregion
        }

        class Program
        {
            static string MailBody;
            static string SendgridApiKey = Environment.GetEnvironmentVariable("settings.sendgrid.apikey");

            private static void Main(string[] args)
            {
                //RunJob();
                JobManager.Initialize(new MyRegistry());
                //Console.ReadLine();

                var starting = new ManualResetEventSlim();
                starting.Wait();
                Console.WriteLine("Exiting Application");
            }
            public static void RunJob()
            {
                Console.WriteLine("Program Started");

                var MailBodyReturn = CheckUrl();

                Console.WriteLine(MailBodyReturn);

                try
                {
                    SendMail("Daily Apartment Listings", MailBody, "rasmus@callofthevoid.dk");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
            public static string CheckUrl()
            {
                MailBody = "";
                Console.WriteLine("Running CheckUrl Function");
                using (WebClient webClient = new WebClient())
                {
                    webClient.Encoding = Encoding.UTF7;
                    var page = webClient.DownloadString("http://lejdinbolig.nu/index.php?pID=residenceList&sort=output_aarsleje&dir=ASC");


                    HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(page);

                    List<List<string>> table = doc.DocumentNode.SelectSingleNode("//table")
                                .Descendants("tr")
                                .Skip(1)
                                .Where(tr => tr.Elements("td").Count() > 1)
                                .Select(tr => tr.Elements("td").Select(td => td.InnerText.Trim()).ToList())
                                .ToList();

                    MailBody += "--------------------------------------------<br>\n";
                    foreach (var data in table)
                    {
                        var l_address = data[0];
                        var l_area = data[1];
                        var l_rooms = data[2];
                        var l_balcony = data[3];
                        var l_rentmonth = data[4];
                        var l_occupation = data[5];
                        var l_status = data[6];

                        if (l_address.Contains("Østbrovej"))
                        {
                            /*
                            Console.WriteLine("Address:     " + l_address);
                            Console.WriteLine("Area:        " + l_area);
                            Console.WriteLine("Rooms:       " + l_rooms);
                            Console.WriteLine("Balcony:     " + l_balcony);
                            Console.WriteLine("Rent:        " + l_rentmonth);
                            Console.WriteLine("Occupation:  " + l_occupation);
                            Console.WriteLine("Status:      " + l_status);
                            */

                            MailBody += "Address:     " + l_address + "\n<br>";
                            MailBody += "Area:        " + l_area + "\n<br>";
                            MailBody += "Rooms:       " + l_rooms + "\n<br>";
                            MailBody += "Balcony:     " + l_balcony + "\n<br>";
                            MailBody += "Rent:        <b>" + l_rentmonth + "</b>\n<br>";
                            MailBody += "Occupation:  " + l_occupation + "\n<br>";
                            MailBody += "Status:      " + l_status + "\n<br>";
                            MailBody += "--------------------------------------------<br>\n";
                        }
                    }
                }
                return MailBody;
            }
            public static void SendMail(string mailsubject, string mailbody, string mailto)
            {
                Console.WriteLine("Running SendMail Function");
                try
                {
                    //var apiKey = "";
                    var client = new SendGridClient(SendgridApiKey);
                    //Console.WriteLine("SENDGRIDKEY: " + SendgridApiKey);
                    var from = new EmailAddress("listings@callofthevoid.dk", "Apartment Listings");
                    var subject = mailsubject;
                    var to = new EmailAddress(mailto);
                    var Content = mailbody;
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, Content, Content);
                    var response = client.SendEmailAsync(msg);

                    var statuscode = response.Result.StatusCode.ToString();
                    if (statuscode == "Accepted") { Console.ForegroundColor = ConsoleColor.Green; }
                    else { Console.ForegroundColor = ConsoleColor.Red; }

                    Console.WriteLine("Sendgrid Response: " + statuscode);

                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
        }
    }
}
