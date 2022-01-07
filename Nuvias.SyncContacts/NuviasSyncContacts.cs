using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.EventLog;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.IO;
using TeamsHook.NET;

namespace Nuvias.SyncContacts
{
    public partial class NuviasSyncContacts : ServiceBase {
        private static ILogger<NuviasSyncContacts> _logger;
        private static Timer _timer;

        public NuviasSyncContacts()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder => {
                builder.AddConsole()
                       .AddEventLog();
            });
            _logger = loggerFactory.CreateLogger<NuviasSyncContacts>();
            _logger.LogInformation(Conf("AppName") + " Started.");

            //start the run timer (runs every x minutes, based on config file)
            _timer = new Timer(new TimerCallback(TickTimer), null, int.Parse(Conf("TimerDelay")), int.Parse(Conf("TimerCheck")));
            _logger.LogInformation("Timer Started.");
        }

        protected override void OnStop()
        {
            //stop timer
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Timer Stopped.");

            //event log stop signal
            _logger.LogInformation("Service Stopped.");
        }

        public static string Conf(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        public static void SetConf(string key, string val)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings[key].Value = val;
            configuration.Save(ConfigurationSaveMode.Full, true);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static bool Ping(string Host)
        {
            Ping myPing = new Ping();
            byte[] buffer = new byte[32];
            int timeout = 1000;
            PingOptions pingOptions = new PingOptions();
            PingReply reply = myPing.Send(Host, timeout, buffer, pingOptions);
            if (reply.Status == IPStatus.Success)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        static void TickTimer(object state)
        {
            bool _online = false;
            bool _executionneeded = false;
            int i = 0;
            string lijn = "";

            //check the internet connection (InternetAccessCheckIP)
            try
            {
                if (Ping(Conf("InternetAccessCheckIP")))
                {
                    _online = true;
                    //_logger.LogInformation("Internet access confirmed.");
                }
                else
                {
                    _logger.LogError("No Internet access!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            if (_online)
            {
                var cultureInfo = new CultureInfo("nl-BE");


                //TODO actions to take when online (sync organisations)
                //sync contacts
                //create users when not in db + userprops & userprefs
                //update users (blocked or updated)
                //sync subscriptions
                //test git

            }
        }
        private static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
        }

        private static string AfasToken()
        {
            return "AfasToken " + Base64Encode("<token><version>1</version><data>" + Conf("AFASToken") + "</data></token>");
        }

        private static string TreatAFASDate(string datum)
        {
            string tmp = "";
            if (datum == null) return tmp;
            if (datum == "") return tmp;
            if (datum == "null") return tmp;
            string[] spl = datum.Split('/');
            tmp = spl[1] + "/" + spl[0] + "/" + spl[2];

            return tmp;
        }

        private static void TeamsMessage(string message)
        {
            string url = Conf("TeamsWebhookUrl");
            var client = new TeamsHookClient();
            var card = new MessageCard()
            {
                Title = "Status Update",
                Text = message
            };

            try
            {
                var task = client.PostAsync(url, card);
                task.Wait();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("TeamsMessage : Error recieved:\r\n\r\n" + ex.Message);
            }
        }

        private static List<AFASOrganisation> AfasOrganisations()
        {
            string url = Conf("AFASBaseUrl") + "/profitrestservices/connectors/LibreNMS_Organisations?skip=0&take=30000&Orderbyfieldids&filterfieldids&operatortypes";
            List<AFASOrganisation> tmp = new List<AFASOrganisation>();
            AFASOrganisation niew = null;

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                };
                request.Headers.Add("Authorization", AfasToken());

                var task = client.SendAsync(request)
                .ContinueWith((taskwithmsg) =>
                {
                    var response = taskwithmsg.Result;
                    var str = response.Content.ReadAsStringAsync();
                    string res = str.Result;

                    dynamic jsonObj = JsonConvert.DeserializeObject(res);
                    int c = jsonObj.rows.Count;

                    _logger.LogInformation("Number of organisation rows recieved = " + c);

                    foreach (dynamic row in jsonObj.rows)
                    {
                        niew = new AFASOrganisation();
                        niew.Addressline1 = row.AddressLine1;
                        niew.Addressline2 = row.AddressLine2;
                        niew.Addressline3 = row.AddressLine3;
                        niew.Addressline4 = row.AddressLine4;
                        niew.Naam = row.Naam;
                        niew.Nummer = row.Nummer;                        
                        tmp.Add(niew);
                    }
                });
                task.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogInformation("AfasOrganisations : Error recieved:\r\n\r\n" + ex.Message);
            }

            return tmp;
        }

        private static List<AFASContact> AfasContacts()
        {
            string url = Conf("AFASBaseUrl") + "/profitrestservices/connectors/LibreNMS_Contacts?skip=0&take=30000&Orderbyfieldids&filterfieldids&operatortypes";
            List<AFASContact> tmp = new List<AFASContact>();
            AFASContact niew = null;

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                };
                request.Headers.Add("Authorization", AfasToken());

                var task = client.SendAsync(request)
                .ContinueWith((taskwithmsg) =>
                {
                    var response = taskwithmsg.Result;
                    var str = response.Content.ReadAsStringAsync();
                    string res = str.Result;

                    dynamic jsonObj = JsonConvert.DeserializeObject(res);
                    int c = jsonObj.rows.Count;

                    _logger.LogInformation("Number of contact rows recieved = " + c);

                    foreach (dynamic row in jsonObj.rows)
                    {
                        niew = new AFASContact();
                        niew.Blocked = row.Blocked;
                        niew.Name = row.Name;
                        niew.Type = row.Type;
                        niew.Voornaam = row.Voornaam;
                        niew.Achternaam = row.Achternaam;
                        niew.Function = row.Function;
                        niew.Functioncard = row.FunctionCard;
                        niew.Addressline1 = row.AddressLine1;
                        niew.Addressline2 = row.AddressLine2;
                        niew.Addressline3 = row.AddressLine3;
                        niew.Department = row.Department;
                        niew.Gender = row.Gender;
                        niew.Incident_statusmails = row.Incident_Statusmails;
                        niew.Mag_incident_insturen = row.Mag_incident_Insturen;
                        niew.Mailwork = row.MailWork;
                        niew.Mobwork = row.MobWork;
                        niew.Telwork = row.TelWork;
                        niew.Orgnumber = row.OrgNumber;
                        niew.Pernumber = row.PerNumber;
                        niew.Supportportal_master_user = row.Supportportal_Master_User;
                        niew.Toegang_supportportal = row.Toegang_Supportportal;
                        tmp.Add(niew);
                    }
                });
                task.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogInformation("AfasContacts : Error recieved:\r\n\r\n" + ex.Message);
            }

            return tmp;
        }

        private static List<AFASSubscription> AfasSubscriptions()
        {
            string url = Conf("AFASBaseUrl") + "/profitrestservices/connectors/LibreNMS_DCB_SubscriptionDetail?skip=0&take=40000&Orderbyfieldids&filterfieldids&operatortypes";
            List<AFASSubscription> tmp = new List<AFASSubscription>();
            AFASSubscription niew = null;

            try
            {
                var client = new HttpClient();
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(url),
                    Method = HttpMethod.Get,
                };
                request.Headers.Add("Authorization", AfasToken());

                var task = client.SendAsync(request)
                .ContinueWith((taskwithmsg) =>
                {
                    var response = taskwithmsg.Result;
                    var str = response.Content.ReadAsStringAsync();
                    string res = str.Result;

                    dynamic jsonObj = JsonConvert.DeserializeObject(res);
                    int c = jsonObj.rows.Count;

                    _logger.LogInformation("Number of subscription rows recieved = " + c);

                    foreach (dynamic row in jsonObj.rows)
                    {
                        niew = new AFASSubscription();
                        niew.SubNr = row.SubNr;
                        niew.NameEndUser = row.NameEndUser;
                        niew.NameEndUserId = row.NameEndUserId;
                        niew.ResellerName = row.ResellerName;
                        niew.ResellerNameId = row.ResellerNameId;
                        niew.StartDate = row.StartDate;
                        niew.EndDate = row.EndDate;
                        niew.Sla = row.SubType;
                        niew.BillingCycle = row.BillingCycle;
                        niew.DeviceName = row.DeviceName;
                        niew.SerialNumber = row.SerialNumber;
                        niew.TrustedIP = row.TrustedIP;
                        niew.ExternalIP1 = row.ExternalIP1;
                        niew.ExternalIP2 = row.ExternalIP2;
                        niew.ExternalIP3 = row.ExternalIP3;
                        niew.ExternalIP4 = row.ExternalIP4;
                        niew.ResellerContact1ID = row.ResellerContact1ID;
                        niew.ResellerContact1 = row.ResellerContact1;
                        niew.ResellerContact2 = row.ResellerContact2;
                        niew.EndUserContact1 = row.EndUserContact1;
                        niew.EndUserContact2 = row.EndUserContact2;
                        niew.EndUserContact3 = row.EndUserContact3;
                        niew.EndUserContact4 = row.EndUserContact4;
                        niew.EndUserContact5 = row.EndUserContact5;
                        niew.Code = row.Code;
                        niew.Description = row.Description;
                        niew.Detail = row.Detail;
                        niew.Quantity = row.Quantity;
                        niew.StartDateRule = row.StartDateRule;
                        niew.EndDateRule = row.EndDateRule;
                        niew.SubTypeCode = row.SubTypeCode;
                        niew.Model = row.Model;
                        niew.DeviceAddress = row.DeviceAddress;
                        niew.SN = row.SN;
                        niew.WebConfig = row.WebConfig;
                        niew.GUI_User_Name = row.GUI_User_Name;
                        niew.GUI_Passphrase = row.GUI_Passphrase;
                        niew.Managment_station = row.Managment_station;
                        niew.Log_Host_1 = row.Log_Host_1;
                        niew.Log_Host_2 = row.Log_Host_2;
                        niew.READ_Passphrase = row.READ_Passphrase;
                        niew.WRITE_Passphrase = row.WRITE_Passphrase;
                        niew.BeginCycle = row.BeginCycle;
                        niew.Unit = row.Unit;
                        niew.License_end_date = row.License_end_date;
                        niew.Niet_factureren = row.Niet_factureren;
                        tmp.Add(niew);
                    }
                });
                task.Wait();

            }
            catch (Exception ex)
            {
                _logger.LogInformation("AfasSubscriptions : Error recieved:\r\n\r\n" + ex.Message);
            }

            return tmp;
        }
    }
}
