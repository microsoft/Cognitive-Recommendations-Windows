# Recommendations API Sample 

## Introduction
This sample shows how to use the [Recommendations API](https://www.microsoft.com/cognitive-services/en-us/recommendations-api). 
You can find more details on the Recommendations API and other Cognitive Services at [www.microsoft.com/cognitive-services/](http://www.microsoft.com/cognitive-services/). 
 

The Recommendations API identifies consumption patterns from your transaction information in order to provide recommendations. These recommendations can help your customers more easily discover items that they may be interested in.  By showing your customers products that they are more likely to be interested in, you will, in turn, increase your sales.

## Before you get started...
If you have not done so already, you will need to sign up for the Recommendations API service.

1. Go to the **Azure Portal** at [http://portal.azure.com/](http://portal.azure.com/) and sign in with your Azure account.
2. Click on **+ New**.
3. Select the **Intelligence** option.
4. Select the **Cognitive Services APIs** product. This product will allow you to start a subscription for any of the cognitive services APIs (Face, Text Analytics, Computer Vision, etc.). Today we will focus on the Recommendations API.
5. Enter a **Resource name** for your Recommendations subscription. (For instace: "MyRecommendations"). This name should not have any spaces in it.
6. On **API type**, select **Recommendations**.
7. On **Pricing tier**, you can select a plan. You may select the Free tier for 10,000 transactions/month. This is a free plan, so it is a good way to start trying the system. Once you go to production, we recommend you consider your request volume and change the plan type accordingly.
8. Select a **Resource Group**, or create a new one if you don't have one already.
9. You may change other elements in the Create dialog. We should point out that the resource provider today is only supported from United States data centers. Once you are done with any selections, click **Create**.
10. Wait a few minutes for the resource to be deployed. Once it is deployed, you can go to the **Keys** section in the **Settings** blade where you will be provided a primary and secondary key to use the API. Copy the primary key, as you'll need it when creating your first model.

## Buiding the Sample
First of all, provide your API key to the application by passing it as command-line parameters.
Right click on the project and click on **Properties**. 
On **Properties**, go to **Debug**, and then on **Command line arguments** enter the API key. You may choose to modify the code so this information is part of the code instead of reading from the command line. 

## Description
The application will:
1. Create a model container.
2. Add catalog and usage data needed to train the model.
3. Trigger a recommendation model build.
4. Monitor the training process, and notify you when the build has completed.
5. Use the newly created build to get recommendations.
6. The sample also shows how to do batch scoring (you will need to uncomment the line that calls the batch scoring)

## Source Code Files
- SampleApp.cs - The main file that included the application code. 
- RecommendationsApiWrapper.cs - A wrapper that makes it easy to consume the recommendations API from C# 
- Helpers.cs - Helper file with classes to simplify serialization/deserialization of the RESTful API requests/responses. 
- BlobHelper.cs - Helper File to deal with Blob Storage. (Used only for batch scoring)

You may find more realistic data files at http://aka.ms/RecoSampleData 

## More Information
To learn more you can visit the complete Recommendations API Reference If you have questions, don't hesitate to contact us at mlapi@microsoft.com
