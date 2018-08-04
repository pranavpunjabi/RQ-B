using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
namespace BotApplication5.Dialogs
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Builder.FormFlow;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;

    public static class RQVMetrics
    {
        public static Dictionary<string, List<string>> VerticalsAndSubVerticals = new Dictionary<string, List<string>>()
        {
            { "Accessibility", new List<string> () {"3rd Party Assistive Tech","Assistive Technologies","Core OS Experiences","Updatable App Experiences" } },
            { "App Platform", new List<string> () {"Activation","App Deployment","App Experience","Centennial","Composition","Dev Experience","Ink And Intelligence","Input","Input Platform","Messaging","Project Rome","Watson MTTF" } },
            { "Compat", new List<string> () {"3rd Party Anti-Malware","Modern Apps","OEM Systems & Peripherals","Win32 Apps" } },
            { "Compliance", new List<string> () {"Accessibility","AT API Scan","AT N SKU","AT Protocols","Crypto","Exception","GlobalReadiness","IncompleteAssessments","Manageability","Privacy","Proposed Deliverables","Security" } },
            { "Data Basics", new List<string> () {"Device Telemetry Health","DnA Health","MC Health" } },
            { "Deployment", new List<string> () {"Feature Update Flights","Flights from Active(RS5)","Flights from Current(RS2 or RS3)","Imaging","Media Upgrade","Recovery","Sedimentation","Serviceability" } },
            { "Edge", new List<string> () {"App Compliance","App Experiences","App Performance","App Reliability","App Startup","Compliance","Developer Experience","Performance","Reliability","Startup","Web Compliance","Web Experiences","Web Performance","Web Reliability" } },
            { "Edge Browsing Experience", new List<string> () {"Activation","App Experiences","Crashes and Hangs","Input Platform","Instant Startup","Responsiveness and Performance","Security and Compliance","Tab Experiences","Web Compat and Interop","Web Experiences" } },
            { "Enterprise Experiences", new List<string> () {"Defender ATP","Identity","Information Protection","Licensing","Management","Provisioning" } },
            { "Hardware And Devices", new List<string> () {"Audio","Bluetooth","Camera","Cellular","External Devices","Gaming","Graphics","Kernel And Platform","Miracast","Networking","Print","Remote Desktop","Storage","Video" } },
            { "Inbox Apps Experiences", new List<string> () {"Camera","Feedback Hub","Groove","Mail And Calendar","Maps","Mixed Reality Viewer","Movies And TV","Paint 3D","People","Photos","View 3D" } },
            { "Local User Experience", new List<string> () {"Language Experience","Language Quality","Tier 1 Languages","Tier 2 Languages","Tier 3 Languages","Tier 4 Languages" } },
            { "Partner Ecosystem", new List<string> () {"Manufacturing","MDA Upgrade","OEM" } },
            { "Performance", new List<string> () {"Animations","App Lifecycle","Apps","Browsing","Cortana","Disk Footprint","Input","Instant On","Memory","Networking","On Off","Shell" } },
            { "Power", new List<string> () {"HOBL","SB","Screen off Drilldown","Screen on Drilldown","SP4","Surface Laptop","Surface Pro" } },
            { "Reliability", new List<string> () {"AbnormalShutdown","KernelModeCrash","UserModeCrash","UserModeHang" } },
            { "Security Experiences", new List<string> () {"Authentication","BitLocker","Hardened Platform","Hello Authentication","Safety","Security Services","System Guard","Trusted Platform Module","WD Application Guard" } },
            { "Store", new List<string> () {"Acquisition","Discovery & Navigation","Licensing","Performance","Purchase","Reliability" } },
            { "Web Platform", new List<string> () {"Activation","Composition","Developer Experience","Input Platform" } },
            { "Windows Inbox Experiences", new List<string> () {"Cortana","Login and Lock","Offers and Promotions","Primary UI Experiences","Secondary UI Experiences","Settings","Software Input","Tabbed Sets" } },
        };
    }

#pragma warning disable 1998

    [Serializable]
    public class RootDialog : IDialog<object>
    {
        private string verticalSelected;
        //private int age;
        private string subVerticalSelected;
        private Dictionary<string, Dictionary<string, string>> verticalsAndSubVerticals;

        public async Task StartAsync(IDialogContext context)
        {
            /* Wait until the first message is received from the conversation and call MessageReceviedAsync 
             *  to process that message. */
            context.Wait(this.MessageReceivedAsync);
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            bool kustoQuerySucceeded = false;
            var client = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider("https://wdgeventstore.kusto.windows.net/Webster;Fed=True");
            IDataReader reader;
            using (reader = client.ExecuteQuery("snapMeasures | where parentGroupUrlName startswith \"rqvrs5_desktop\" and parentGroupDisplayName != \"\" | distinct verticalDisplayName, parentGroupDisplayName, parentGroupUrlName | project verticalDisplayName, parentGroupDisplayName, parentGroupUrlName"))
            {
                verticalsAndSubVerticals = new Dictionary<string, Dictionary<string, string>>();

                try
                {
                    while (reader.Read())
                    {
                        if (reader.FieldCount != 3)
                        {
                            throw new Exception("Expected number of columns 3, but see" + reader.FieldCount);
                        }

                        string vertical = reader[0].ToString();
                        string subVertical = reader[1].ToString();
                        string url = reader[2].ToString();
                        if (!verticalsAndSubVerticals.ContainsKey(vertical))
                        {
                            verticalsAndSubVerticals.Add(vertical, new Dictionary<string, string>() { { subVertical, url } });
                        }
                        else
                        {
                            if (!verticalsAndSubVerticals[vertical].ContainsKey(subVertical))
                            {
                                verticalsAndSubVerticals[vertical].Add(subVertical, url);
                            }
                            else
                            {
                                throw new Exception("Unexpected: " + vertical + ":" + subVertical + ":" + url + "Existing:" + verticalsAndSubVerticals[vertical][subVertical]);
                            }
                        }
                    }
                    kustoQuerySucceeded = true;
                }
                catch (Exception e)
                {
                    var message = await result;
                    await context.PostAsync(String.Format("Exception message: {0}", e.Message));
                }
            }

            // Call Close when done reading.
            //reader.Close();

            /* When MessageReceivedAsync is called, it's passed an IAwaitable<IMessageActivity>. To get the message,
             *  await the result. */
            if (kustoQuerySucceeded)
            {
                var message = await result;

                PromptDialog.Choice(
                    context,
                    VerticalsDialogResumeAfter,
                    verticalsAndSubVerticals.Keys,
                    "Select a Vertical:",
                    "Please try again",
                    3,
                    PromptStyle.Auto,
                    null
                );
            }
        }

        //private async Task SendWelcomeMessageAsync(IDialogContext context)
        //{
        //    await context.PostAsync("Hi, I'm the Basic Multi Dialog bot. Let's get started.");
        //    context.Call(new NameDialog(), this.NameDialogResumeAfter);
        //}

        private async Task VerticalsDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            //try
            {
                this.verticalSelected = await result;

                PromptDialog.Choice(
                    context,
                    SubVerticalsDialogResumeAfter,
                    verticalsAndSubVerticals[this.verticalSelected].Keys,
                    "Select a Sub Vertical:",
                    "Please try again",
                    3,
                    PromptStyle.Auto,
                    null
                );
            }
        }

        private async Task SubVerticalsDialogResumeAfter(IDialogContext context, IAwaitable<string> result)
        {
            bool kustoQuerySucceeded = false;
            this.subVerticalSelected = await result;

            var client = Kusto.Data.Net.Client.KustoClientFactory.CreateCslQueryProvider("https://wdgeventstore.kusto.windows.net/Webster;Fed=True");
            string queryForMeasuresGroup =
                String.Format(@"
                //this query will generate MEASURE GROUPS results
                let targetViewName = ""{0}""; //viewName for the dashboard
                let targetDataSlice_Name = ""0""; 
                let startDate = datetime({yyyy-MM-dd});
                let endDate = datetime({yyyy-MM-dd});
                let latestSnaps = materialize(snapMeasureGroups
                | where viewName == targetViewName
                | where dataSlice_Name == targetDataSlice_Name
                | where targetDate >= startDate and targetDate <= endDate
                | project targetDate, snapDateTimePst
                | summarize arg_max(snapDateTimePst, *) by targetDate);
                snapMeasureGroups
                | where viewName == targetViewName and dataSlice_Name == targetDataSlice_Name  and measureGroupUrlName == ""{2}""
                | join kind = inner(latestSnaps) on snapDateTimePst, targetDate
                | project areaOwner, name, dataSlice_Branch, dataSlice_Build, dttScore, snapDateTimePst", "rrdesktop-rs5", DateTime.Now, verticalsAndSubVerticals[verticalSelected][subVerticalSelected]);

            string owner = String.Empty;
            string dttScore = String.Empty;
            string branch = String.Empty;
            string build = String.Empty;
            IDataReader reader;
            using (reader = client.ExecuteQuery(queryForMeasuresGroup))
            {
                try
                {
                    while (reader.Read())
                    {
                        if (reader.FieldCount != 6)
                        {
                            throw new Exception("Expected number of columns 6, but see" + reader.FieldCount);
                        }

                        owner = reader[0].ToString();
                        dttScore = reader[4].ToString();
                        branch = reader[2].ToString();
                        build = reader[3].ToString();
                    }

                    kustoQuerySucceeded = true;
                }
                catch (Exception e)
                {
                    await context.PostAsync("Exception: " + e.Message);
                }
            }

            // Call Close when done reading.
            //reader.Close();

            if (kustoQuerySucceeded)
            {
                await context.PostAsync("You selected the Vertical " + this.verticalSelected.ToString() + " and the sub vertical " + this.subVerticalSelected.ToString() +
                    String.Format("\nowner={0},dtt={1},branch={2},build={3}", owner, dttScore, branch, build));
            }
        }

        //private async Task AgeDialogResumeAfter(IDialogContext context, IAwaitable<int> result)
        //{
        //    try
        //    {
        //        this.age = await result;
        //        await context.PostAsync($"Your name is { verticalSelected } and your age is { age }.");

        //    }
        //    catch (TooManyAttemptsException)
        //    {
        //        await context.PostAsync("I'm sorry, I'm having issues understanding you. Let's try again.");
        //    }
        //    finally
        //    {
        //        await this.SendWelcomeMessageAsync(context);
        //    }
        //}

        //public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        //{
        //    var confirm = await argument;
        //    if (confirm)
        //    {
        //        //this.count = 1;
        //        await context.PostAsync("Reset count.");
        //    }
        //    else
        //    {
        //        await context.PostAsync("Did not reset count.");
        //    }
        //    context.Wait(MessageReceivedAsync);
        //}
    }
}
