using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRS.Core;
using PRS.Repository;
using System.Data;
using System.Text;

namespace PRS.Service
{
    public class CommonService
    {
        private readonly PRS_DbContext _dbContext;

        public CommonService()
        {
            _dbContext = new PRS_DbContext();
        }

        public async Task<bool> CollectDataSet(string trainDataPath, string testDataPath)
        {
            try
            {
                var trainingDataSet = await _dbContext.ModelTrainDataSets.ToListAsync();
                if (trainingDataSet.Count > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (ModelTrainDataSet trainingSet in trainingDataSet)
                    {
                        builder.AppendLine(trainingSet.TrainDataSet);
                    }
                    System.IO.File.WriteAllText(trainDataPath, builder.ToString(), new UTF8Encoding());

                    var testDataCount = Convert.ToInt32(Math.Round(trainingDataSet.Count*0.2));
                    var testDataSet = trainingDataSet.Take(testDataCount).ToList();
                    if (testDataSet.Count > 0)
                    {
                        builder = new StringBuilder();
                        foreach (ModelTrainDataSet trainingSet in testDataSet)
                        {
                            builder.AppendLine(trainingSet.TrainDataSet);
                        }
                        System.IO.File.WriteAllText(testDataPath, builder.ToString(), new UTF8Encoding());
                    }
                }
                return true;
            }
            catch (Exception ex)
            {

            }
            return false;
        }
        public async Task<Patient> GetPredictRequestAsync(string id)
        {
            try
            {
                var request = new Patient();
                var sqlParameters = new List<SqlParameter>()
                {
                    new SqlParameter("@Id", id)
                };
                var predictRequestInfo = await GetDataTableFromSP("GetPredictRequestInfo", sqlParameters);

                if (predictRequestInfo!=null && predictRequestInfo.Rows.Count>0)
                {
                    request.DiseaseId=predictRequestInfo.Rows[0][0].ToString();
                    request.TreatmentId=predictRequestInfo.Rows[0][1].ToString();
                    request.ChiefComplaints=predictRequestInfo.Rows[0][2].ToString();
                }
                return request;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string?> GetPrescriptionAsync(int id)
        {
            var sqlParameters = new List<SqlParameter>()
            {
                new SqlParameter("@Id", id)
            };
            var medicineList = await GetDataTableFromSP("GetPrescription", sqlParameters);

            if (medicineList!=null && medicineList.Rows.Count>0)
            {
                return medicineList.Rows[0][0]?.ToString();
            }
            return "";
        }

        public async Task<DataTable> GetDataTableFromSP(string storedProcedure, List<SqlParameter> sqlParameters)
        {
            using (var command = _dbContext.Database.GetDbConnection().CreateCommand())
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProcedure;
                command.Parameters.AddRange(sqlParameters.ToArray());
                await _dbContext.Database.OpenConnectionAsync();
                using (var result = command.ExecuteReader())
                {
                    var dataTable = new DataTable();
                    dataTable.Load(result);
                    await _dbContext.Database.CloseConnectionAsync();
                    return dataTable;
                }
            }
        }
    }
}
