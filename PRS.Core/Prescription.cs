using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRS.Core
{
    public class Prescription
    {
        //diseaseId,treatmentId,chiefComplaints,prescriptionId
        [LoadColumn(0)]
        public string? diseaseId;

        [LoadColumn(1)]
        public string? treatmentId;

        [LoadColumn(2)]
        public string? chiefComplaints;

        [LoadColumn(3)]
        public float prescriptionId;
    }
}
