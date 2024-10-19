using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRS.Core
{
    public class Patient
    {
        public string DiseaseId { get; set; }

        public string TreatmentId { get; set; }

        public string ChiefComplaints { get; set; }
    }
}
