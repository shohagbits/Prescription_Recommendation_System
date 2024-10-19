using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using PRS.Core;

namespace PRS.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AiModealControlle : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }

        [HttpGet]
        public IActionResult Train()
        {
            string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-train.csv");
            string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-test.csv");
            string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "Medicine_Model.zip");

            MLContext mlContext = new MLContext(seed: 0);

            //diseaseId,treatmentId,chiefComplaints,prescriptionId
            IDataView dataView = mlContext.Data.LoadFromTextFile<Prescription>(_trainDataPath, hasHeader: true, separatorChar: ',');

            ITransformer Train(MLContext mlContext)
            {
                //Define trainer options.
                var options = new LbfgsPoissonRegressionTrainer.Options
                {
                    // Reduce optimization tolerance to speed up training at the cost of
                    // accuracy.
                    OptimizationTolerance = 1e-4f,
                    // Decrease history size to speed up training at the cost of
                    // accuracy.
                    HistorySize = 30,
                    // Specify scale for initial weights.
                    InitialWeightsDiameter = 0.2f
                };

                var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "prescriptionId")
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "diseaseIdEncoded", inputColumnName: "diseaseId"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "treatmentIdEncoded", inputColumnName: "treatmentId"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "chiefComplaintsEncoded", inputColumnName: "chiefComplaints"))
                    .Append(mlContext.Transforms.Concatenate("Features", "diseaseIdEncoded", "treatmentIdEncoded", "chiefComplaintsEncoded"))
                     //.Append(mlContext.Transforms.Concatenate("Features", "diseaseId", "treatmentId", "chiefComplaints"))
                     .Append(mlContext.Regression.Trainers.LbfgsPoissonRegression());
                //.Append(mlContext.Regression.Trainers.FastTree());
                var model = pipeline.Fit(dataView);
                return model;
            }
            var model = Train(mlContext);

            void Evaluate(MLContext mlContext, ITransformer model)
            {
                IDataView dataView = mlContext.Data.LoadFromTextFile<Prescription>(_testDataPath, hasHeader: true, separatorChar: ',');
                var predictions = model.Transform(dataView);
                var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");
                Console.WriteLine();
                Console.WriteLine($"*************************************************");
                Console.WriteLine($"*       Model quality metrics evaluation         ");
                Console.WriteLine($"*------------------------------------------------");
                Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
                Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:0.##}");
            }
            Evaluate(mlContext, model);

            void TestSinglePrediction(MLContext mlContext, ITransformer model)
            {
                var predictionFunction = mlContext.Model.CreatePredictionEngine<Prescription, PrescriptionPrediction>(model);
                var taxiTripSample = new Prescription()
                {
                    diseaseId = "1",
                    treatmentId = "2",
                    chiefComplaints = "Chief complaints position 1",
                    prescriptionId = 0 // To predict. 1,2,Chief complaints position 1,19
                };
                var prediction = predictionFunction.Predict(taxiTripSample);
                Console.WriteLine($"**********************************************************************");
                Console.WriteLine($"Predicted fare: {prediction.prescriptionId:0.####}, actual fare: 19");
                Console.WriteLine($"Predicted Round: {Math.Round(prediction.prescriptionId)}, actual fare: 19");
                Console.WriteLine($"**********************************************************************");
            }
            TestSinglePrediction(mlContext, model);

            // Save the model to a file
            SaveModel(mlContext, dataView.Schema, model);

            void SaveModel(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
            {
                var modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "MedicineRecommenderModel.zip");

                Console.WriteLine("=============== Saving the model to a file ===============");
                mlContext.Model.Save(model, trainingDataViewSchema, modelPath);
            }

            return Ok("Done!");
        }
        [HttpGet]
        public IActionResult Test()
        {
            MLContext _mlContext = new MLContext(seed: 0);
            // Load the model from the zipped file
            var modelPath = Path.Combine(AppContext.BaseDirectory, "Data", "MedicineRecommenderModel.zip");
            ITransformer model = _mlContext.Model.Load(modelPath, out var modelSchema);

            // Create the prediction engine
            PredictionEngine<Prescription, PrescriptionPrediction> _predictionEngine = _mlContext.Model.CreatePredictionEngine<Prescription, PrescriptionPrediction>(model);

            var taxiTripSample = new Prescription()
            {
                diseaseId = "1",
                treatmentId = "2",
                chiefComplaints = "Chief complaints position 2",
                prescriptionId = 0 // To predict. 1,2,Chief complaints position 1,18
            };
            var prediction = _predictionEngine.Predict(taxiTripSample);
            Console.WriteLine($"**********************************************************************");
            Console.WriteLine($"Predicted fare: {prediction.prescriptionId:0.####}, actual fare: 15.5");
            Console.WriteLine($"Predicted Round: {Math.Round(prediction.prescriptionId)}, actual fare: 18");
            Console.WriteLine($"**********************************************************************");

            return Ok(prediction);
        }
    }
}
