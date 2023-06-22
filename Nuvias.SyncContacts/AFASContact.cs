using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuvias.SyncContacts
{
    internal class AFASContact
    {
        //LibreNMS_Contacts

        private string _type;        
        private string _name;
        private int _orgnumber;
        private int _pernumber;
        private string _voornaam;
        private string _achternaam;
        private string _department;
        private string _addressline1;
        private string _addressline2;
        private string _addressline3;
        private string _telwork;
        private string _mobwork;
        private string _mailwork;
        private string _function;
        private string _functioncard;
        private string _gender;
        private bool _Supportportal_Master_User;
        private bool _Mag_incident_insturen;
        private bool _Toegang_Supportportal;
        private bool _Incident_Statusmails;
        private bool _Blocked;
        
        public string Type { get { return _type; } set { _type = value; } }
        public string Name { get { return _name;} set { _name = value; } }
        public int Orgnumber { get { return _orgnumber; } set { _orgnumber = value; } }
        public int Pernumber { get { return _pernumber; } set { _pernumber = value; } }
        public string Voornaam { get { return _voornaam; } set { _voornaam = value; } }   
        public string Achternaam { get { return _achternaam; } set { _achternaam = value; } }
        public string Department { get { return _department; } set { _department = value; } }
        public string Addressline1 { get { return _addressline1; } set { _addressline1 = value; } }
        public string Addressline2 { get { return _addressline2; } set { _addressline2 = value; } }
        public string Addressline3 { get { return _addressline3; } set { _addressline3 = value; } }
        public string Telwork { get { return _telwork; } set { _telwork = value; } }
        public string Mobwork { get { return _mobwork; } set { _mobwork = value; } }
        public string Mailwork { get { return _mailwork; } set { _mailwork = value; } }
        public string Function { get { return _function; } set { _function = value; } }
        public string Functioncard { get { return _functioncard; } set { _functioncard = value; } }
        public string Gender { get { return _gender; } set { _gender = value; } }
        public bool Supportportal_master_user { get { return _Supportportal_Master_User; } set { _Supportportal_Master_User = value; } }
        public bool Mag_incident_insturen { get { return _Mag_incident_insturen;} set { _Mag_incident_insturen = value; } }
        public bool Toegang_supportportal { get { return _Toegang_Supportportal; } set { _Toegang_Supportportal = value; } }
        public bool Incident_statusmails { get { return _Incident_Statusmails; } set { _Incident_Statusmails = value; } }
        public bool Blocked { get { return _Blocked; } set { _Blocked = value; } }

    }
}
