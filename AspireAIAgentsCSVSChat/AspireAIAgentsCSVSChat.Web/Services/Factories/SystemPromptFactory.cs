using AspireAIAgentsCSVSChat.Web.Services.Models;
using static AspireAIAgentsCSVSChat.Web.Services.StructuredFormats.ChatResponseFormatBuilder;

namespace AspireAIAgentsCSVSChat.Web.Services.Factories
{
    internal static class SystemPromptFactory
    {
        public static string GetAgentName(AgentType agentType)
        {

            string name = string.Empty;
            switch (agentType)
            {
                case AgentType.ValidationPlanning:
                    name = "ValidationPlanning";
                    break;
                case AgentType.RiskAssessment:
                    name = "RiskAssessment";
                    break;
                case AgentType.StakeholderAlignment:
                    name = "DocumentationTraining";
                    break;
                case AgentType.RequirementsSpecification:
                    name = "RequirementsSpecification";
                    break;
                case AgentType.OngoingReview:
                    name = "OngoingReview";
                    break;
                case AgentType.Coordinator:
                    name = "Coordinator";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(agentType), agentType, null);
            }



            return name;//.ToUpper();
        }


        public static string GetAgentPrompts(AgentType agentType)
        {

            string promptFile = string.Empty;
            switch (agentType)
            {
                case AgentType.ValidationPlanning:
                    promptFile = "ValidationPlanning.prompty";
                    break;
                case AgentType.RiskAssessment:
                    promptFile = "RiskAssessment.prompty";
                    break;
                case AgentType.StakeholderAlignment:
                    promptFile = "DocumentationTraining.prompty";
                    break;
                case AgentType.RequirementsSpecification:
                    promptFile = "RequirementsSpecification.prompty";
                    break;
                case AgentType.OngoingReview:
                    promptFile = "OngoingReview.prompty";
                    break;
                case AgentType.SelectionStrategy:
                    promptFile = "SelectionStrategy.prompty";
                    break;
                case AgentType.TerminationStrategy:
                    promptFile = "TerminationStrategy.prompty";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(agentType), agentType, null);
            }

            string prompt = $"{File.ReadAllText("Prompts/" + promptFile)}{File.ReadAllText("Prompts/CommonAgentRules.prompty")}";

            return prompt;
        }

        public static string GetStrategyPrompts(ChatResponseStrategy strategyType)
        {
            string prompt = string.Empty;
            switch (strategyType)
            {
                case ChatResponseStrategy.Continuation:
                    prompt = File.ReadAllText("Prompts/SelectionStrategy.prompty");
                    break;
                case ChatResponseStrategy.Termination:
                    prompt = File.ReadAllText("Prompts/TerminationStrategy.prompty");
                    break;

            }
            return prompt;
        }
        public static string CreateSystemPrompt(string systemPrompt)
        {
            return systemPrompt;
        }
        public static string CreateSystemPrompt(string systemPrompt, string additionalInfo)
        {
            return $"{systemPrompt} {additionalInfo}";
        }
        public static string CreateSystemPrompt(string systemPrompt, string additionalInfo, string context)
        {
            return $"{systemPrompt} {additionalInfo} {context}";
        }
    }
}
