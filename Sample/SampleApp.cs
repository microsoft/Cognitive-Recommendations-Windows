/**************************************************************************************************
 * This sample shows how to use the Recommendations API. You can find more details on the 
 * Recommendations API and other Cognitive Services at http://go.microsoft.com/fwlink/?LinkID=759709.
 * 
 * The Recommendations API identifies consumption patterns from your transaction information 
 * in order to provide recommendations. These recommendations can help your customers more 
 * easily discover items that they may be interested in.  By showing your customers products that 
 * they are more likely to be interested in, you will, in turn, increase your sales.
 * 
 *  Before you run the application:
 *  1. Sign up for the Recommendations API service and get an API Key.
 *     (http://go.microsoft.com/fwlink/?LinkId=761106 )
 *     
 *  2. Set the AccountKey variable in the RecommendationsSampleApp to the key you got.
 *  
 *  3. Verify the endpoint Uri you got when you subscribed matches the BaseUri as it may 
 *     be different if you selected a different data center.
 *
 *  4. To train a model with your own data, you will need to replace the catalog files
 *     and usage files in the Resources folder, and add them to the project. 
 *     Note that you will need to set the "Copy To Ouput Directory" property to "Copy If Newer" 
 *     if you add new resource files.
 * 
 *************************************************************************************************/

namespace Recommendations
{
    using AzureMLRecoSampleApp;
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;

    public class RecommendationsSampleApp
    {
        private static string AccountKey = ""; // <---  Set to your API key here.
        private const string BaseUri = "https://westus.api.cognitive.microsoft.com/recommendations/v4.0"; 
        private static RecommendationsApiWrapper recommender = null;

        /// <summary>
        /// 1) Builds a recommendations model and upload catalog and usage data
        /// 2) Triggers a model build and monitor the build operation status
        /// 3) Sets the build as the active build for the model.
        /// 4) Requests item recommendations
        /// 5) Requests user recommendations
        /// </summary>
        public static void Main(string[] args)
        {
            string modelName = "MyNewModel";
            string modelId = null;
            long buildId =  -1;

            try
            {
                if (String.IsNullOrEmpty(AccountKey))
                {
                    Console.WriteLine("In order to use this sample, you need to get have an Account Key for the Recommendations Cognitive Service.");
                    Console.WriteLine("You can get one from the Azure Management Portal (http://go.microsoft.com/fwlink/?LinkId=761106)");
                    Console.WriteLine("Please enter your Recommendations API Account key:");
                    AccountKey = Console.ReadLine();
                    return;
                }

                recommender = new RecommendationsApiWrapper(AccountKey, BaseUri);

                // Create a model if not already provided.
                if (String.IsNullOrEmpty(modelId))
                {
                    modelId = CreateModel(modelName);
                }

                // If build is not provided, trigger a build with new data.
                if (buildId == -1)
                {
                    // Upload Catalog and Usage data and then train the model (create a build)
                    buildId = UploadDataAndTrainModel(modelId, BuildType.Recommendation);
                }

                // Get item-to-item recommendations and user-to-item recommendations one at a time
                GetRecommendationsSingleRequest(recommender, modelId, buildId);

                // Uncomment to score a batch of recommendations.
                // GetRecommendationsBatch(recommender, modelId, buildId);

                Console.WriteLine("Press any key to end");
                Console.ReadKey();
            }
            catch(Exception e)
            {
                Console.WriteLine("Error encountered:");
                Console.WriteLine(e.Message);
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }
            finally
            {
                // Uncomment the line below if you wish to delete the model.
                // Note that you can have up to 10 models at any time. 
                // You may have up to 20 builds per model.
                //recommender.DeleteModel(modelId); 
            }
        }

        /// <summary>
        /// Creates a model, upload catalog and usage file and trigger a build.
        /// Returns the Build ID of the trained build.
        /// </summary>
        /// <param name="recommender">Wrapper that maintains API key</param>
        /// <param name="modelId">The model Id</param>
        public static string CreateModel(string modelName)
        {
            string modelId;
            Console.WriteLine("Creating a new model {0}...", modelName);
            ModelInfo modelInfo = recommender.CreateModel(modelName, "MSStore");
            modelId = modelInfo.Id;
            Console.WriteLine("Model '{0}' created with ID: {1}", modelName, modelId);
            return modelId;
        }

        /// <summary>
        /// Creates a model, upload catalog and usage files and trigger a build.
        /// Returns the Build ID of the trained build.
        /// </summary>
        /// <param name="recommender">Wrapper that maintains API key</param>
        /// <param name="buildType">The type of build. (Recommendation or FBT)</param>
        /// <param name="modelId">The model Id</param>
        public static long UploadDataAndTrainModel(string modelId, BuildType buildType = BuildType.Recommendation)
        {
            long buildId = -1;
            
            // Import data to the model.            
            var resourcesDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            Console.WriteLine("Importing catalog files...");

            int catalogFilesCount = 0;
            foreach (string catalog in Directory.GetFiles(resourcesDir, "catalog*.csv"))
            {
                var catalogFile = new FileInfo(catalog);
                recommender.UploadCatalog(modelId, catalogFile.FullName, catalogFile.Name);
                catalogFilesCount++;
            }

            Console.WriteLine("Imported {0} catalog files.", catalogFilesCount);


            Console.WriteLine("Importing usage files...");
            int usageFilesCount = 0;
            foreach (string usage in Directory.GetFiles(resourcesDir, "usage*.csv"))
            {
                var usageFile = new FileInfo(usage);
                recommender.UploadUsage(modelId, usageFile.FullName, usageFile.Name);
                usageFilesCount++;
            }

            Console.WriteLine("Imported {0} usage files.", usageFilesCount);

            #region training
            // Trigger a recommendation build.
            string operationLocationHeader;
            Console.WriteLine("Triggering build for model '{0}'. \nThis will take a few minutes...", modelId);
            if (buildType == BuildType.Recommendation)
            {
                buildId = recommender.CreateRecommendationsBuild(modelId, "Recommendation Build " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                                                                     enableModelInsights: false,
                                                                     operationLocationHeader: out operationLocationHeader);
            }
            else
            {
                buildId = recommender.CreateFbtBuild(modelId, "Frequenty-Bought-Together Build " + DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                                                     enableModelInsights: false,
                                                     operationLocationHeader: out operationLocationHeader);
            }

            // Monitor the build and wait for completion.
            Console.WriteLine("Monitoring build {0}", buildId);
            var buildInfo = recommender.WaitForOperationCompletion<BuildInfo>(RecommendationsApiWrapper.GetOperationId(operationLocationHeader));
            Console.WriteLine("Build {0} ended with status {1}.\n", buildId, buildInfo.Status);

            if (String.Compare(buildInfo.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) != 0)
            {
                Console.WriteLine("Build {0} did not end successfully, the sample app will stop here.", buildId);
                Console.WriteLine("Press any key to end");
                Console.ReadKey();
                return - 1;
            }

            // Waiting  in order to propagate the model updates from the build...
            Console.WriteLine("Waiting for 40 sec for propagation of the built model...");
            Thread.Sleep(TimeSpan.FromSeconds(40));

            // The below api is more meaningful when you want to give a certain build id to be an active build.
            // Currently this app has a single build which is already active.
            Console.WriteLine("Setting build {0} as active build.", buildId);
            recommender.SetActiveBuild(modelId, buildId);
            #endregion

            return buildId;
        }

        /// <summary>
        /// Shows how to get item-to-item recommendations and user-to-item-recommendations
        /// </summary>
        /// <param name="recommender">Wrapper that maintains API key</param>
        /// <param name="modelId">Model ID</param>
        /// <param name="buildId">Build ID</param>
        public static void GetRecommendationsSingleRequest(RecommendationsApiWrapper recommender, string modelId, long buildId)
        {
            // Get item to item recommendations. (I2I)
            Console.WriteLine();
            Console.WriteLine("Getting Item to Item 5C5-00025");
            const string itemIds = "5C5-00025";
            var itemSets = recommender.GetRecommendations(modelId, buildId, itemIds, 6);
            if (itemSets.RecommendedItemSetInfo != null)
            {
                foreach (RecommendedItemSetInfo recoSet in itemSets.RecommendedItemSetInfo)
                {
                    foreach (var item in recoSet.Items)
                    {
                        Console.WriteLine("Item id: {0} \n Item name: {1} \t (Rating  {2})", item.Id, item.Name, recoSet.Rating);
                    }
                }
            }
            else
            {
                Console.WriteLine("No recommendations found.");
            }

            // Now let's get a user recommendation (U2I)
            Console.WriteLine();
            Console.WriteLine("Getting User Recommendations for User: 0003BFFDC7118D12");
            string userId = "0003BFFDC7118D12";
            itemSets = recommender.GetUserRecommendations(modelId, buildId, userId, 6);
            if (itemSets.RecommendedItemSetInfo != null)
            {
                foreach (RecommendedItemSetInfo recoSet in itemSets.RecommendedItemSetInfo)
                {
                    foreach (var item in recoSet.Items)
                    {
                        Console.WriteLine("Item id: {0} \n Item name: {1} \t (Rating  {2})", item.Id, item.Name, recoSet.Rating);
                    }
                }
            }
            else
            {
                Console.WriteLine("No recommendations found.");
            }
        }


        /// <summary>
        /// Shows how to get item-to-item recommendations in batch.
        /// You can learn more about batch scoring at https://azure.microsoft.com/en-us/documentation/articles/cognitive-services-recommendations-batch-scoring/
        /// Before you can use this method, you need to provide your blob account name, blob account key, and the input container name.
        /// </summary>
        /// <param name="recommender">Wrapper that maintains API key</param>
        /// <param name="modelId">Model ID</param>
        /// <param name="buildId">Build ID</param>
        public static void GetRecommendationsBatch(RecommendationsApiWrapper recommender, string modelId, long buildId)
        {
            #region  setup
            // Set storage credentials and copy input file that defines items we want to get recommendations to the Blob Container.
            string blobStorageAccountName = ""; // enter your account name here.
            string blobStorageAccountKey = ""; // enter your account key here
            const string containerName = ""; // enter your container name here

            string outputContainerName = containerName;
            string baseLocation   = "https://" + blobStorageAccountName + ".blob.core.windows.net/";
            string inputFileName  = "batchInput.json"; // the batch input
            string outputFileName = "batchOutput.json"; // the batch output
            string errorFileName  = "batchError.json"; // the batch error

            // Validate user entered credentials.
            if (String.IsNullOrEmpty(blobStorageAccountKey) || String.IsNullOrEmpty(blobStorageAccountKey) || String.IsNullOrEmpty(containerName))
            {
                Console.WriteLine("GetRecommendationsBatch: Provide your blob account name, blob account key, and the input container name.");
                Console.WriteLine("Press any key to continue.");
                Console.ReadKey();
            }

            string connectionString = "DefaultEndpointsProtocol=https;AccountName=" + blobStorageAccountName + ";AccountKey=" + blobStorageAccountKey;

            // Copy input file from resources directory to blob storate
            var sourceStorageAccount = CloudStorageAccount.Parse(connectionString);
            BlobHelper bh = new BlobHelper(sourceStorageAccount, containerName);
            var resourcesDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
            bh.PutBlockBlob(containerName, inputFileName, File.ReadAllText(Path.Combine(resourcesDir, inputFileName)));

            var inputSas = BlobHelper.GenerateBlobSasToken(connectionString, containerName, inputFileName);
            var outputSas = BlobHelper.GenerateBlobSasToken(connectionString, outputContainerName, outputFileName);
            var errorSas = BlobHelper.GenerateBlobSasToken(connectionString, outputContainerName, errorFileName);

            // Now we need to define the batch job to perform.
            BatchJobsRequestInfo batchJobsRequestInfo = new BatchJobsRequestInfo
            {
                Input = new StorageBlobInfo
                {
                    AuthenticationType = "PublicOrSas",
                    BaseLocation = baseLocation,
                    RelativeLocation = containerName + "/" + inputFileName,
                    SasBlobToken = inputSas
                },

                Output = new StorageBlobInfo
                {
                    AuthenticationType = "PublicOrSas",
                    BaseLocation = baseLocation,
                    RelativeLocation = containerName + "/" + outputFileName,
                    SasBlobToken = outputSas
                },

                Error = new StorageBlobInfo
                {
                    AuthenticationType = "PublicOrSas",
                    BaseLocation = baseLocation,
                    RelativeLocation = containerName + "/" + errorFileName,
                    SasBlobToken = errorSas
                },

                // You may modify the information below to meet your request needs.
                // Note that currently only "ItemRecommend" is supported.
                Job = new JobInfo
                {
                    ApiName = "ItemRecommend",
                    ModelId = modelId, //staging model id for books
                    BuildId = buildId,   
                    NumberOfResults = 10,
                    IncludeMetadata = false,
                    MinimalScore = 0
                }
            };

            #endregion


            #region start the job, wait for completion 

            // kick start the batch job.
            string operationLocationHeader = "";
            var jobId = recommender.StartBatchJob(batchJobsRequestInfo, out operationLocationHeader);

            // Monitor the batch job and wait for completion.
            Console.WriteLine("Monitoring batch job {0}", jobId);

            var batchInfo = recommender.WaitForOperationCompletion<BatchJobInfo>(RecommendationsApiWrapper.GetOperationId(operationLocationHeader));
            Console.WriteLine("Batch {0} ended with status {1}.\n", jobId, batchInfo.Status);


            if (String.Compare(batchInfo.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) != 0)
            {
                Console.WriteLine("Batch job {0} did not end successfully, the sample app will stop here.", jobId);
                Console.WriteLine("Press any key to end");
                Console.ReadKey();
                return;
            }
            else
            {
                // Copy the output file from blob starage into the local machine.
                Stream reader = bh.GetBlobReader(outputContainerName, outputFileName);
                string outputFullPath = Path.Combine(resourcesDir, outputFileName);
                using (var fileStream = File.Create(outputFullPath))
                {
                    reader.Seek(0, SeekOrigin.Begin);
                    reader.CopyTo(fileStream);
                }

                Console.WriteLine("The output of the blob operation has been saved to: {0}", outputFullPath);
            }
            #endregion
        }
    }
}