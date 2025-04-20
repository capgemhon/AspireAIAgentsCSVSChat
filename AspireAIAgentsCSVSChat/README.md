# AI Chat with Custom Data

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. Please note that this template is currently in an early preview stage. If you have feedback, please take a [brief survey](https://aka.ms/dotnet-chat-templatePreview2-survey).

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

# Configure the AI Model Provider

## Using Azure OpenAI

To use Azure OpenAI, you will need an Azure account and an Azure OpenAI Service resource. For detailed instructions, see the [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource).

### 1. Create an Azure OpenAI Service Resource
[Create an Azure OpenAI Service resource](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal).

### 2. Deploy the Models
Deploy the `gpt-4o-mini` and `text-embedding-3-small` models to your Azure OpenAI Service resource. When creating those deployments, give them the same names as the models (`gpt-4o-mini` and `text-embedding-3-small`). See the Azure OpenAI documentation to learn how to [Deploy a model](https://learn.microsoft.com/azure/ai-services/openai/how-to/create-resource?pivots=web-portal#deploy-a-model).

### 3. Configure API Key and Endpoint
Configure your Azure OpenAI API key and endpoint for this project, using .NET User Secrets:
   1. In the Azure Portal, navigate to your Azure OpenAI resource.
   2. Copy the "Endpoint" URL and "Key 1" from the "Keys and Endpoint" section.
   3. In Visual Studio, right-click on the AspireAIAgentsCSVSChat.AppHost project in the Solution Explorer and select "Manage User Secrets".
   4. This will open a secrets.json file where you can store your API key and endpoint without it being tracked in source control. Add the following keys & values to the file:

      ```json
      {
        "ConnectionStrings:openai": "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
      }
      ```

Make sure to replace `YOUR-API-KEY` and `YOUR-DEPLOYMENT-NAME` with your actual Azure OpenAI key and endpoint. Make sure your endpoint URL is formatted like https://YOUR-DEPLOYMENT-NAME.openai.azure.com/ (do not include any path after .openai.azure.com/).

# Running the application

## Using Visual Studio

1. Open the `.sln` file in Visual Studio.
2. Press `Ctrl+F5` or click the "Start" button in the toolbar to run the project.

## Using Visual Studio Code

1. Open the project folder in Visual Studio Code.
2. Install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) for Visual Studio Code.
3. Once installed, Open the `Program.cs` file in the AspireAIAgentsCSVSChat.AppHost project.
4. Run the project by clicking the "Run" button in the Debug view.

## Trust the localhost certificate

Several .NET Aspire templates include ASP.NET Core projects that are configured to use HTTPS by default. If this is the first time you're running the project, an exception might occur when loading the Aspire dashboard. This error can be resolved by trusting the self-signed development certificate with the .NET CLI.

See [Troubleshoot untrusted localhost certificate in .NET Aspire](https://learn.microsoft.com/dotnet/aspire/troubleshooting/untrusted-localhost-certificate) for more information.

# Learn More
To learn more about development with .NET and AI, check out the following links:

* [AI for .NET Developers](https://learn.microsoft.com/dotnet/ai/)
