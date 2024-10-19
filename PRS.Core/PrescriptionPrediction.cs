using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRS.Core
{
    public class PrescriptionPrediction
    {
        [ColumnName("Score")]
        public float prescriptionId;
    }
}
