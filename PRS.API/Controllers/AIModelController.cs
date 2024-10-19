using Microsoft.AspNetCore.Mvc;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using PRS.Core;

namespace PRS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AIModelController : ControllerBase
    {
        private readonly MLContext _mlContext;
        private readonly string _trainDataPath;
        private readonly string _testDataPath;
        private readonly string _trainedModelPath;

        public AIModelController()
        {
            _mlContext = new MLContext(seed: 0);
            _trainDataPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "patient-prescription-train.csv");
            _testDataPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "patient-prescription-test.csv");
            _trainedModelPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "PrescriptionPredictTrainedModel.zip");
        }
        [HttpGet]
        [Route("dataset")]
        public IActionResult Dataset()
        {         
            return Ok($"Successfully collected dataset from Database to perform trained model at {DateTime.Now}!");
        }

        [HttpGet]
        [Route("train")]
        public IActionResult Trained()
        {
            //diseaseId,treatmentId,chiefComplaints,prescriptionId
            IDataView dataView = _mlContext.Data.LoadFromTextFile<Prescription>(_trainDataPath, hasHeader: true, separatorChar: ',');

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
            var model = Train(_mlContext);

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
            Evaluate(_mlContext, model);

            //void TestSinglePrediction(MLContext mlContext, ITransformer model)
            //{
            //    var predictionFunction = mlContext.Model.CreatePredictionEngine<Prescription, PrescriptionPrediction>(model);
            //    var taxiTripSample = new Prescription()
            //    {
            //        diseaseId = "1",
            //        treatmentId = "2",
            //        chiefComplaints = "Chief complaints position 1",
            //        prescriptionId = 0 // To predict. 1,2,Chief complaints position 1,19
            //    };
            //    var prediction = predictionFunction.Predict(taxiTripSample);
            //    Console.WriteLine($"**********************************************************************");
            //    Console.WriteLine($"Predicted fare: {prediction.prescriptionId:0.####}, actual fare: 19");
            //    Console.WriteLine($"Predicted Round: {Math.Round(prediction.prescriptionId)}, actual fare: 19");
            //    Console.WriteLine($"**********************************************************************");
            //}
            //TestSinglePrediction(_mlContext, model);

            // Save the model to a file
            SaveModel(_mlContext, dataView.Schema, model);

            void SaveModel(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
            {
                Console.WriteLine("=============== Saving the model to a file ===============");
                mlContext.Model.Save(model, trainingDataViewSchema, _trainedModelPath);
            }

            return Ok($"Successfully trained an AI based Prescription Predict Model at {DateTime.Now}!");
        }

        // POST api/aimodel/predict
        [HttpPost]
        [Route("predict")]
        public IActionResult Predict([FromBody] Patient model)
        {
            // Load the model from the zipped file
            ITransformer modelLoad = _mlContext.Model.Load(_trainedModelPath, out var modelSchema);

            // Create the prediction engine
            PredictionEngine<Prescription, PrescriptionPrediction> _predictionEngine = _mlContext.Model.CreatePredictionEngine<Prescription, PrescriptionPrediction>(modelLoad);

            var prescription = new Prescription()
            {
                diseaseId =model.DiseaseId,
                treatmentId = model.TreatmentId,
                chiefComplaints = model.ChiefComplaints,
                prescriptionId =0 // To predict. 1,2,Chief complaints position 1,18
            };
            var prediction = _predictionEngine.Predict(prescription);

            Console.WriteLine($"**********************************************************************");
            Console.WriteLine($"Predicted fare: {prediction.prescriptionId:0.####}, actual fare: 15.5");
            Console.WriteLine($"Predicted Round: {Math.Round(prediction.prescriptionId)}, actual fare: 18");
            Console.WriteLine($"**********************************************************************");

            return Ok(prediction);
        }
    }
}
