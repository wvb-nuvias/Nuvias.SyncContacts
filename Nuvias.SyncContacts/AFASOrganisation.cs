using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nuvias.SyncContacts
{
    internal class AFASOrganisation
    {
        //LibreNMS_Organisations

        private int _nummer;
        private string _naam;
        private string _addressline1;
        private string _addressline2;
        private string _addressline3;
        private string _addressline4;

        public int Nummer { get => _nummer; set => _nummer = value; }
        public string Naam { get => _naam; set => _naam = value; }
        public string Addressline1 { get => _addressline1; set => _addressline1 = value; }
        public string Addressline2 { get => _addressline2; set => _addressline2 = value; }
        public string Addressline3 { get => _addressline3; set => _addressline3 = value; }
        public string Addressline4 { get => _addressline4; set => _addressline4 = value; }
    }
}
