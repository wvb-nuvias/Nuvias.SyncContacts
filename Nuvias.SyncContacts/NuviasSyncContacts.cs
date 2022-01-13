using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Net.NetworkInformation;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using TeamsHook.NET;
using MySql.Data.MySqlClient;

namespace Nuvias.SyncContacts
{
    public partial class NuviasSyncContacts : ServiceBase {
        private static ILogger<NuviasSyncContacts> _logger;
        private static Timer _timer;
        private static bool MySqlConnected = false;
        private static MySqlConnection mysqlconnection = default;

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
            _timer = new Timer(new TimerCallback(TickTimer), null, int.Parse(Conf("TimerDelay")), int.Parse(Conf("TimerCheck")));
            _logger.LogInformation("Timer Started.");
        }

        protected override void OnStop()
        {            
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Timer Stopped.");
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
            string[] spl = default;
                        
            try
            {
                if (Ping(Conf("InternetAccessCheckIP")))
                {
                    _online = true;            
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

                DateTime last = DateTime.Parse(Conf("LastExecution"), cultureInfo);
                
                //TODO if conf executionDys = * (every day!!)
                if (Conf("ExecutionDays") != "*")
                {
                    DateTime now = DateTime.Parse(DateTime.Now.Day.ToString() + "/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString() + " " + Conf("ExecutionTime"), cultureInfo);
                    //construct executionDays, based on config value
                    foreach (string day in Conf("ExecutionDays").Split(','))
                    {
                        DateTime dat = DateTime.Parse(day + "/" + DateTime.Now.Month.ToString() + "/" + DateTime.Now.Year.ToString() + " " + Conf("ExecutionTime"), cultureInfo);

                        string _message = "";
                        _message += "Now: " + now.ToString("dd/MM/yyyy HH:mm:ss", cultureInfo) + "\r\n";
                        _message += "Last: " + last.ToString("dd/MM/yyyy HH:mm:ss", cultureInfo) + "\r\n";
                        _message += "Check: " + dat.ToString("dd/MM/yyyy HH:mm:ss", cultureInfo) + "\r\n";

                        if (now.CompareTo(dat) > 0)
                        {
                            _message += "Date is in the past";
                        }
                        else if (now.CompareTo(dat) < 0)
                        {
                            _message += "Date is in the future";
                        }
                        else
                        {
                            _message += "Date is today";
                            //do not run if already run today
                            if (last.ToString("dd/MM/yyyy") != now.ToString("dd/MM/yyyy"))
                            {
                                _executionneeded = true;
                            }
                        }
                    }

                    if (Conf("ExecuteOnStart") == "true")
                    {
                        if (last.ToString("dd/MM/yyyy") != now.ToString("dd/MM/yyyy"))
                        {
                            _executionneeded = true;
                        }
                    }
                } else
                {
                    spl = Conf("ExecutionTime").Split(',');
                                                            
                    foreach (string s in spl)
                    {
                        if (int.Parse(s) == DateTime.Now.Hour)
                        {
                            if (last.Hour != DateTime.Now.Hour)
                            {
                                _executionneeded = true;
                                break;
                            }
                        }
                    }

                }

                if (_executionneeded)
                {                    
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);

                    var startexecution = DateTime.Now;

                    MySqlConnect();
                    if (MySqlConnected)
                    {                        
                        SaveOrganisations(AfasOrganisations());
                        SaveContacts(AfasContacts());
                        SaveSubscriptions(AfasSubscriptions());
                    }
                    MySqlDisconnect();

                    var stopexecution = DateTime.Now;
                    TimeSpan diff = stopexecution - startexecution;
                    var tijd = diff.Hours.ToString("00") + ":" + diff.Minutes.ToString("00") + ":" + diff.Seconds.ToString("00");

                    SetConf("LastExecution", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", cultureInfo));                    
                    TeamsMessage("Finished organisations, contacts and subscriptions update, it took " + tijd + ".");
                                        
                    _timer.Change(int.Parse(Conf("TimerDelay")), int.Parse(Conf("TimerCheck")));
                }                
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

        private static void MySqlConnect()
        {
            mysqlconnection = new MySqlConnection(Conf("IncidentsDataSource"));
            try
            {
                mysqlconnection.Open();
                MySqlConnected = true;
                _logger.LogInformation("MySqlConnect : Connection established.\r\nData Source: " + Conf("IncidentsDataSource") + "\r\n");
            }
            catch (MySqlException e)
            {
                _logger.LogInformation("MySqlConnect : Error Generated. Details: " + e.ToString() + "\r\nData Source: " + Conf("IncidentsDataSource") + "\r\n");
            }            
        }

        private static void MySqlDisconnect()
        {
            if (MySqlConnected)
            {
                mysqlconnection.Close();
                mysqlconnection = null;
                MySqlConnected=false;
                _logger.LogInformation("MySqlDisconnect : Connection Closed.\r\n");
            }
        }

        private static void SaveOrganisations(List<AFASOrganisation> lst)
        {            
            foreach (AFASOrganisation organisation in lst)
            {
                SaveOrganisation(organisation);
            }            
        }

        private static void SaveContacts(List<AFASContact> lst) {
            foreach (AFASContact contact in lst)
            {
                SaveContact(contact);
            }        
        }

        private static void SaveSubscriptions(List<AFASSubscription> lst)
        {
            foreach (AFASSubscription subscription in lst)
            {
                SaveSubscription(subscription);
            }            
        }
        
        private static void SaveOrganisation(AFASOrganisation organisation)
        {
            bool exists = false;            
            string existstr = "";
            string query = "";
            string tmp = "Save Organisation\r\n-----------------\r\nOrganisation Id: " + organisation.Nummer + "\r\nOrganisation Name:" + organisation.Naam + "\r\n\r\n";            
            MySqlCommand cmd = default;
                        
            query = "SELECT organisationid FROM organisations WHERE organisationid=@organisationid;";
            cmd = new MySqlCommand(query, mysqlconnection);
            cmd.Parameters.AddWithValue("@organisationid", organisation.Nummer);

            try
            {
                MySqlDataReader reader = cmd.ExecuteReader();
                exists = reader.HasRows;
                reader.Close();
                reader = null;
                if (exists)
                {
                    existstr = "exists.";
                }
                else
                {
                    existstr = "does not exist.";
                }
                tmp += "Record check for Organisation Id " + organisation.Nummer + " : Record " + existstr + "\r\n";
            }
            catch (MySqlException e)
            {
                tmp += "Record check : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
            }
                        
            if (exists)
            {
                query = "UPDATE organisations SET name=@name, address1=@address1, address2=@address2, address3=@address3, updated_at=@updated_at WHERE organisationid=@organisationid;";
                cmd = new MySqlCommand(query, mysqlconnection);
                            
                cmd.Parameters.AddWithValue("@organisationid", organisation.Nummer);
                cmd.Parameters.AddWithValue("@name", organisation.Naam);
                cmd.Parameters.AddWithValue("@address1", organisation.Addressline1);
                cmd.Parameters.AddWithValue("@address2", organisation.Addressline2);
                cmd.Parameters.AddWithValue("@address3", organisation.Addressline3);
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Update record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Update record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }                    
            }
            else
            {
                query = "INSERT INTO organisations (organisationid, name, address1, address2, address3, created_at, updated_at) VALUES(@organisationid, @name, @address1, @address2, @address3, @created_at, @updated_at);";
                cmd = new MySqlCommand(query, mysqlconnection);

                cmd.Parameters.AddWithValue("@organisationid", organisation.Nummer);
                cmd.Parameters.AddWithValue("@name", organisation.Naam);
                cmd.Parameters.AddWithValue("@address1", organisation.Addressline1);
                cmd.Parameters.AddWithValue("@address2", organisation.Addressline2);
                cmd.Parameters.AddWithValue("@address3", organisation.Addressline3);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("G"));
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Insert record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Insert record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }                    
            }            

            _logger.LogInformation(tmp);
        }

        private static void SaveContact(AFASContact contact)
        {
            int userid = 0;
            int isnuvias = 0;
            int isreseller = 0;
            bool exists = false;
            string existstr = "";
            string query = "";
            string tmp = "Save Contact\r\n-----------------\r\nContact Id: " + contact.Pernumber + "\r\n\r\n";
            MySqlCommand cmd = default;

            query = "SELECT contactid FROM contacts WHERE contactid=@contactid;";
            cmd = new MySqlCommand(query, mysqlconnection);
            cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);

            try
            {
                MySqlDataReader reader = cmd.ExecuteReader();
                exists = reader.HasRows;
                reader.Close();
                reader = null;
                if (exists)
                {
                    existstr = "exists.";
                }
                else
                {
                    existstr = "does not exist.";
                }
                tmp += "Record check for Contact Id " + contact.Pernumber + " : Record " + existstr + "\r\n";
            }
            catch (MySqlException e)
            {
                tmp += "Record check : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
            }

            if (exists)
            {
                query = "UPDATE contacts SET organisation=@organisation, firstname=@firstname, lastname=@lastname, telephone=@telephone, mobile=@mobile, email=@email, `function`=@function, departement=@departement, supportportal_masteruser=@supportportal_masteruser, incident_maycreate=@incident_maycreate, supportportal_access=@supportportal_access, incident_statusmails=@incident_statusmails, updated_at='@updated_at', blocked=@blocked WHERE contactid=@contactid;";
                cmd = new MySqlCommand(query, mysqlconnection);

                cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);
                cmd.Parameters.AddWithValue("@organisation", contact.Orgnumber);
                cmd.Parameters.AddWithValue("@firstname", contact.Voornaam);
                cmd.Parameters.AddWithValue("@lastname", contact.Achternaam);
                cmd.Parameters.AddWithValue("@telephone", contact.Telwork);
                cmd.Parameters.AddWithValue("@mobile", contact.Mobwork);
                cmd.Parameters.AddWithValue("@email", contact.Mailwork);
                cmd.Parameters.AddWithValue("@function", contact.Function);
                cmd.Parameters.AddWithValue("@departement", contact.Department);
                cmd.Parameters.AddWithValue("@supportportal_masteruser", contact.Supportportal_master_user);
                cmd.Parameters.AddWithValue("@incident_maycreate", contact.Mag_incident_insturen);
                cmd.Parameters.AddWithValue("@supportportal_access", contact.Toegang_supportportal);
                cmd.Parameters.AddWithValue("@incident_statusmails", contact.Incident_statusmails);
                cmd.Parameters.AddWithValue("@blocked", contact.Blocked);
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));
                    
                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Update record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Update record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }
            }
            else
            {
                query = "INSERT INTO contacts (contactid, organisation, firstname, lastname, telephone, mobile, email, `function`, departement, supportportal_masteruser, incident_maycreate, supportportal_access, incident_statusmails, created_at, updated_at, blocked) VALUES (@contactid, @organisation, @firstname, @lastname, @telephone, @mobile, @email, @function, @departement, @supportportal_masteruser, @incident_maycreate, @supportportal_access, @incident_statusmails, @created_at, @updated_at, @blocked);";
                cmd = new MySqlCommand(query, mysqlconnection);

                cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);
                cmd.Parameters.AddWithValue("@organisation", contact.Orgnumber);
                cmd.Parameters.AddWithValue("@firstname", contact.Voornaam);
                cmd.Parameters.AddWithValue("@lastname", contact.Achternaam);
                cmd.Parameters.AddWithValue("@telephone", contact.Telwork);
                cmd.Parameters.AddWithValue("@mobile", contact.Mobwork);
                cmd.Parameters.AddWithValue("@email", contact.Mailwork);
                cmd.Parameters.AddWithValue("@function", contact.Function);
                cmd.Parameters.AddWithValue("@departement", contact.Department);
                cmd.Parameters.AddWithValue("@supportportal_masteruser", contact.Supportportal_master_user);
                cmd.Parameters.AddWithValue("@incident_maycreate", contact.Mag_incident_insturen);
                cmd.Parameters.AddWithValue("@supportportal_access", contact.Toegang_supportportal);
                cmd.Parameters.AddWithValue("@incident_statusmails", contact.Incident_statusmails);
                cmd.Parameters.AddWithValue("@blocked", contact.Blocked);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("G"));
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));
                    
                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Insert record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Insert record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }
            }
                        
            if (contact.Toegang_supportportal)
            {                
                query = "SELECT userid FROM users WHERE organisations=" + contact.Orgnumber + " contact=" + contact.Pernumber;
                
                cmd = new MySqlCommand(query, mysqlconnection);
                cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);

                try
                {
                    MySqlDataReader reader = cmd.ExecuteReader();
                    exists = reader.HasRows;
                    if (exists)
                    {
                        reader.Read();
                        userid = reader.GetInt32(0);
                    }                    

                    reader.Close();
                    reader = null;
                    if (exists)
                    {
                        existstr = "exists.";
                    }
                    else
                    {
                        existstr = "does not exist.";
                    }
                    tmp += "User Record check for Contact Id " + contact.Pernumber + " : Record " + existstr + "\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "User Record check : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }

                if (exists)
                {                    
                    query = "UPDATE users SET name=@name, email=@email, updated_at=@updated_at, organisations=@organisations, contactid=@contactid, supportportal_masteruser=@supportportal_masteruser, incident_maycreate=@incident_maycreate, supportportal_access=@supportportal_access, incident_statusmails=@incident_statusmails WHERE userid=" + userid;
                    cmd = new MySqlCommand(query, mysqlconnection);

                    cmd.Parameters.AddWithValue("@name", contact.Voornaam + " " + contact.Achternaam);
                    cmd.Parameters.AddWithValue("@organisations", contact.Orgnumber);
                    cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);
                    cmd.Parameters.AddWithValue("@email", contact.Mailwork);
                    cmd.Parameters.AddWithValue("@supportportal_masteruser", contact.Supportportal_master_user);
                    cmd.Parameters.AddWithValue("@incident_maycreate", contact.Mag_incident_insturen);
                    cmd.Parameters.AddWithValue("@supportportal_access", contact.Toegang_supportportal);
                    cmd.Parameters.AddWithValue("@incident_statusmails", contact.Incident_statusmails);
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                    try
                    {
                        cmd.ExecuteNonQuery();
                        tmp += "Update user record successful.\r\n";
                    }
                    catch (MySqlException e)
                    {
                        tmp += "Update user record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                    }
                } else 
                {
                    int newid;

                    query = "INSERT INTO users (name, email, password, created_at, updated_at, organisations, contactid, supportportal_masteruser, incident_maycreate, supportportal_access, incident_statusmails) VALUES (@name, @email, @password, @created_at, @updated_at, @organisations, @contactid, @supportportal_masteruser, @incident_maycreate, @supportportal_access, @incident_statusmails);";
                    cmd = new MySqlCommand(query, mysqlconnection);

                    cmd.Parameters.AddWithValue("@name", contact.Voornaam + " " + contact.Achternaam);
                    cmd.Parameters.AddWithValue("@organisations", contact.Orgnumber);
                    cmd.Parameters.AddWithValue("@password", "$2y$10$B0.Sqa1Whm6qKRU5J0/9Xe9AosHJfjalRLBYrPoah/rakGmw55Pgi"); //default password 12345678
                    cmd.Parameters.AddWithValue("@contactid", contact.Pernumber);
                    cmd.Parameters.AddWithValue("@email", contact.Mailwork);
                    cmd.Parameters.AddWithValue("@supportportal_masteruser", contact.Supportportal_master_user);
                    cmd.Parameters.AddWithValue("@incident_maycreate", contact.Mag_incident_insturen);
                    cmd.Parameters.AddWithValue("@supportportal_access", contact.Toegang_supportportal);
                    cmd.Parameters.AddWithValue("@incident_statusmails", contact.Incident_statusmails);                    
                    cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("G"));
                    cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                    try
                    {
                        cmd.ExecuteNonQuery();
                        newid = (int)cmd.LastInsertedId;
                        tmp += "Insert user record successful.\r\n";
                    }
                    catch (MySqlException e)
                    {
                        tmp += "Insert user record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                    }
                                        
                    query = "INSERT INTO userprefs (user,name,value,created_at,updated_at) VALUES (" + userid + ",'locale','en',now(),now());";
                    query += "INSERT INTO userprefs (user,name,value,created_at,updated_at) VALUES (" + userid + ",'theme','dark',now(),now());";
                    query += "INSERT INTO userprefs (user, name, value, created_at, updated_at) VALUES(" + userid + ", 'incidentstatusarray', '[1,2,3,7]', now(), now());";
                    
                    cmd = new MySqlCommand(query, mysqlconnection);

                    try
                    {
                        cmd.ExecuteNonQuery();                        
                        tmp += "Insert userprefs records successful.\r\n";
                    }
                    catch (MySqlException e)
                    {
                        tmp += "Insert userprefs records : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                    }
                                        
                    query = "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'IsSiteAdmin','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'IsDeveloper','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanAPI','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanSeeLog','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanReOpen','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanChangeType','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanImpersonate','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanAssign','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanEscalate','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanDeleteIncidents','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanReadIncidents','1',now(),now(),'',0);";

                    if (contact.Orgnumber == 1001051 || contact.Orgnumber == 1010318)
                    {
                        isnuvias = 1;
                        query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanCloseIncidents','1',now(),now(),'',0);";
                    }
                    else
                    {
	                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES ("+ userid +",'CanCloseIncidents','0',now(),now(),'',0);";
                    }

                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'IsNuviasDCB','" + isnuvias + "',now(),now(),'',0);";
                                        
                    isreseller = AfasSubscriptions().Where(x => x.ResellerNameId == contact.Orgnumber).Count()>1?1:0;
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'IsReseller','" + isreseller + "',now(),now(),'',0);";

                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'CanUpdateIncidents','" + contact.Mag_incident_insturen + "',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'CanCreateIncidents','" + contact.Mag_incident_insturen + "',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'IsAdmin','" + contact.Supportportal_master_user + "',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'CanReadPolicyChanges','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'CanCreatePolicyChanges','0',now(),now(),'',0);";
                    query += "INSERT INTO userproperties (user,name,value,created_at,updated_at,description,hidden) VALUES (" + userid + ",'CanReadSubscriptions','1',now(),now(),'',0);";

                    cmd = new MySqlCommand(query, mysqlconnection);

                    try
                    {
                        cmd.ExecuteNonQuery();
                        tmp += "Insert userprops records successful.\r\n";
                    }
                    catch (MySqlException e)
                    {
                        tmp += "Insert userprops records : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                    }
                }
            }
            
            _logger.LogInformation(tmp);
        }

        private static void SaveSubscription(AFASSubscription subscription)
        {
            bool exists = false;
            string existstr = "";
            string query = "";
            string tmp = "Save Subscription\r\n-----------------\r\nSubscription Id: " + subscription.SubNr + "\r\n\r\n";
            MySqlCommand cmd = default;

            query = "SELECT contractnumber FROM subscriptions WHERE contractnumber=@contractnumber;";
            cmd = new MySqlCommand(query, mysqlconnection);
            cmd.Parameters.AddWithValue("@contractnumber", subscription.SubNr);

            try
            {
                MySqlDataReader reader = cmd.ExecuteReader();
                exists = reader.HasRows;
                reader.Close();
                reader = null;
                if (exists)
                {
                    existstr = "exists.";
                }
                else
                {
                    existstr = "does not exist.";
                }
                tmp += "Record check for Subscription Id " + subscription.SubNr + " : Record " + existstr + "\r\n";
            }
            catch (MySqlException e)
            {
                tmp += "Record check : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
            }
                        
            if (exists)
            {
                query = "UPDATE subscriptions SET reseller=@reseller, organisation=@organisation, serialnumber=@serialnumber, devicename=@devicename, articlecode=@articlecode, description=@description, trustedip=@trustedip, externalip1=@externalip1, externalip2=@externalip2, externalip3=@externalip3, externalip4=@externalip4, deviceaddress=@deviceaddress, model=@model, startdate=@startdate, enddate=@enddate, updated_at=@updated_at, sla=@sla, sladays=@sladays, startdaterule=@startdaterule, enddaterule=@enddaterule, stopped=@stopped WHERE contractnumber=@contractnumber;";
                cmd = new MySqlCommand(query, mysqlconnection);

                cmd.Parameters.AddWithValue("@contractnumber", subscription.SubNr);
                cmd.Parameters.AddWithValue("@reseller", subscription.ResellerNameId);
                cmd.Parameters.AddWithValue("@organisation", subscription.NameEndUserId);
                cmd.Parameters.AddWithValue("@serialnumber", subscription.SerialNumber);
                cmd.Parameters.AddWithValue("@devicename", subscription.DeviceName);
                cmd.Parameters.AddWithValue("@articlecode", subscription.Code);
                cmd.Parameters.AddWithValue("@description", subscription.Description);
                cmd.Parameters.AddWithValue("@trustedip", subscription.TrustedIP);
                cmd.Parameters.AddWithValue("@externalip1", subscription.ExternalIP1);
                cmd.Parameters.AddWithValue("@externalip2", subscription.ExternalIP2);
                cmd.Parameters.AddWithValue("@externalip3", subscription.ExternalIP3);
                cmd.Parameters.AddWithValue("@externalip4", subscription.ExternalIP4);
                cmd.Parameters.AddWithValue("@deviceaddress", subscription.DeviceAddress);
                cmd.Parameters.AddWithValue("@model", subscription.Model);
                cmd.Parameters.AddWithValue("@startdate", subscription.StartDate);
                cmd.Parameters.AddWithValue("@enddate", subscription.EndDate);
                cmd.Parameters.AddWithValue("@sla", subscription.Sla);
                cmd.Parameters.AddWithValue("@sladays", "");
                cmd.Parameters.AddWithValue("@startdaterule", subscription.StartDateRule);
                cmd.Parameters.AddWithValue("@enddaterule", subscription.EndDateRule);
                cmd.Parameters.AddWithValue("@stopped", 0);
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Update record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Update record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }
            }
            else
            {
                query = "INSERT INTO subscriptions (reseller, organisation, serialnumber, contractnumber, devicename, articlecode, description, trustedip, externalip1, externalip2, externalip3, externalip4, deviceaddress, model, startdate, enddate, created_at, updated_at, sla, sladays, startdaterule, enddaterule, stopped) VALUES (@reseller, @organisation, @serialnumber, @devicename, @articlecode, @description, @trustedip, @externalip1, @externalip2, @externalip3, @externalip4, @deviceaddress, @model, @startdate, @enddate, @created_at, @updated_at, @sla, @sladays, @startdaterule, @enddaterule, @stopped);";
                cmd = new MySqlCommand(query, mysqlconnection);

                cmd.Parameters.AddWithValue("@contractnumber", subscription.SubNr);
                cmd.Parameters.AddWithValue("@reseller", subscription.ResellerNameId);
                cmd.Parameters.AddWithValue("@organisation", subscription.NameEndUserId);
                cmd.Parameters.AddWithValue("@serialnumber", subscription.SerialNumber);
                cmd.Parameters.AddWithValue("@devicename", subscription.DeviceName);
                cmd.Parameters.AddWithValue("@articlecode", subscription.Code);
                cmd.Parameters.AddWithValue("@description", subscription.Description);
                cmd.Parameters.AddWithValue("@trustedip", subscription.TrustedIP);
                cmd.Parameters.AddWithValue("@externalip1", subscription.ExternalIP1);
                cmd.Parameters.AddWithValue("@externalip2", subscription.ExternalIP2);
                cmd.Parameters.AddWithValue("@externalip3", subscription.ExternalIP3);
                cmd.Parameters.AddWithValue("@externalip4", subscription.ExternalIP4);
                cmd.Parameters.AddWithValue("@deviceaddress", subscription.DeviceAddress);
                cmd.Parameters.AddWithValue("@model", subscription.Model);
                cmd.Parameters.AddWithValue("@startdate", subscription.StartDate);
                cmd.Parameters.AddWithValue("@enddate", subscription.EndDate);
                cmd.Parameters.AddWithValue("@sla", subscription.Sla);
                cmd.Parameters.AddWithValue("@sladays", "");
                cmd.Parameters.AddWithValue("@startdaterule", subscription.StartDateRule);
                cmd.Parameters.AddWithValue("@enddaterule", subscription.EndDateRule);
                cmd.Parameters.AddWithValue("@stopped", 0);
                cmd.Parameters.AddWithValue("@created_at", DateTime.Now.ToString("G"));
                cmd.Parameters.AddWithValue("@updated_at", DateTime.Now.ToString("G"));

                try
                {
                    cmd.ExecuteNonQuery();
                    tmp += "Insert record successful.\r\n";
                }
                catch (MySqlException e)
                {
                    tmp += "Insert record : Error Generated. Details: " + e.ToString() + "\r\nQuery : " + query + "\r\n";
                }
            }

            _logger.LogInformation(tmp);
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
