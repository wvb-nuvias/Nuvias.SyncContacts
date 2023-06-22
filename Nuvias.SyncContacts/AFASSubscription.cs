using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuvias.SyncContacts
{
    internal class AFASSubscription
    {
        //LibreNMS_DCB_SubscriptionDetail

        private int _SubNr;
        private string _NameEndUser;
        private int _NameEndUserId;
        private string _ResellerName;
        private int _ResellerNameId;
        private DateTime _StartDate;
        private DateTime _EndDate;
        private string _Sla;
        private string _BillingCycle;
        private string _DeviceName;
        private string _SerialNumber;
        private string _TrustedIP;
        private string _ExternalIP1;
        private string _ExternalIP2;
        private string _ExternalIP3;
        private string _ExternalIP4;
        private string _ResellerContact1ID;
        private string _ResellerContact1;
        private string _ResellerContact2;
        private string _EndUserContact1;
        private string _EndUserContact2;
        private string _EndUserContact3;
        private string _EndUserContact4;
        private string _EndUserContact5;
        private string _Code;
        private string _Description;
        private string _Detail;
        private int _Quantity;
        private DateTime _StartDateRule;
        private DateTime _EndDateRule;
        private string _SubTypeCode;
        private string _Model;
        private string _DeviceAddress;
        private string _SN;
        private string _WebConfig;
        private string _GUI_User_Name;
        private string _GUI_Passphrase;
        private string _Managment_station;
        private string _Log_Host_1;
        private string _Log_Host_2;
        private string _READ_Passphrase;
        private string _WRITE_Passphrase;
        private string _BeginCycle;
        private string _Unit;
        private string _License_end_date;
        private bool _Niet_factureren;

        public int SubNr { get => _SubNr; set => _SubNr = value; }
        public string NameEndUser { get => _NameEndUser; set => _NameEndUser = value; }
        public int NameEndUserId { get => _NameEndUserId; set => _NameEndUserId = value; }
        public string ResellerName { get => _ResellerName; set => _ResellerName = value; }
        public int ResellerNameId { get => _ResellerNameId; set => _ResellerNameId = value; }
        public DateTime StartDate { get => _StartDate; set => _StartDate = value; }
        public DateTime EndDate { get => _EndDate; set => _EndDate = value; }
        public string Sla { get => _Sla; set => _Sla = value; }
        public string BillingCycle { get => _BillingCycle; set => _BillingCycle = value; }
        public string DeviceName { get => _DeviceName; set => _DeviceName = value; }
        public string SerialNumber { get => _SerialNumber; set => _SerialNumber = value; }
        public string TrustedIP { get => _TrustedIP; set => _TrustedIP = value; }
        public string ExternalIP1 { get => _ExternalIP1; set => _ExternalIP1 = value; }
        public string ExternalIP2 { get => _ExternalIP2; set => _ExternalIP2 = value; }
        public string ExternalIP3 { get => _ExternalIP3; set => _ExternalIP3 = value; }
        public string ExternalIP4 { get => _ExternalIP4; set => _ExternalIP4 = value; }
        public string ResellerContact1ID { get => _ResellerContact1ID; set => _ResellerContact1ID = value; }
        public string ResellerContact1 { get => _ResellerContact1; set => _ResellerContact1 = value; }
        public string ResellerContact2 { get => _ResellerContact2; set => _ResellerContact2 = value; }
        public string EndUserContact1 { get => _EndUserContact1; set => _EndUserContact1 = value; }
        public string EndUserContact2 { get => _EndUserContact2; set => _EndUserContact2 = value; }
        public string EndUserContact3 { get => _EndUserContact3; set => _EndUserContact3 = value; }
        public string EndUserContact4 { get => _EndUserContact4; set => _EndUserContact4 = value; }
        public string EndUserContact5 { get => _EndUserContact5; set => _EndUserContact5 = value; }
        public string Code { get => _Code; set => _Code = value; }
        public string Description { get => _Description; set => _Description = value; }
        public string Detail { get => _Detail; set => _Detail = value; }
        public int Quantity { get => _Quantity; set => _Quantity = value; }
        public DateTime StartDateRule { get => _StartDateRule; set => _StartDateRule = value; }
        public DateTime EndDateRule { get => _EndDateRule; set => _EndDateRule = value; }
        public string SubTypeCode { get => _SubTypeCode; set => _SubTypeCode = value; }
        public string Model { get => _Model; set => _Model = value; }
        public string DeviceAddress { get => _DeviceAddress; set => _DeviceAddress = value; }
        public string SN { get => _SN; set => _SN = value; }
        public string WebConfig { get => _WebConfig; set => _WebConfig = value; }
        public string GUI_User_Name { get => _GUI_User_Name; set => _GUI_User_Name = value; }
        public string GUI_Passphrase { get => _GUI_Passphrase; set => _GUI_Passphrase = value; }
        public string Managment_station { get => _Managment_station; set => _Managment_station = value; }
        public string Log_Host_1 { get => _Log_Host_1; set => _Log_Host_1 = value; }
        public string Log_Host_2 { get => _Log_Host_2; set => _Log_Host_2 = value; }
        public string READ_Passphrase { get => _READ_Passphrase; set => _READ_Passphrase = value; }
        public string WRITE_Passphrase { get => _WRITE_Passphrase; set => _WRITE_Passphrase = value; }
        public string BeginCycle { get => _BeginCycle; set => _BeginCycle = value; }
        public string Unit { get => _Unit; set => _Unit = value; }
        public string License_end_date { get => _License_end_date; set => _License_end_date = value; }
        public bool Niet_factureren { get => _Niet_factureren; set => _Niet_factureren = value; }
    }
}
