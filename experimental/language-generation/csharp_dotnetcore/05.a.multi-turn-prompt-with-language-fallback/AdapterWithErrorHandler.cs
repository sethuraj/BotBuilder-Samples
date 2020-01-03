// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private MultiLingualTemplateEngine _lgGenerator;
        public AdapterWithErrorHandler(ICredentialProvider credentialProvider,
            ILogger<BotFrameworkHttpAdapter> logger,
            ConversationState conversationState = null)
            : base(credentialProvider)
        {
            // all lg files that this project would use
            var lgFiles = new List<string> {
                Path.Combine(".", "Resources", "SummaryReadout.fr.lg"),
                Path.Combine(".", "Resources", "AdapterWithErrorHandler.fr-fr.lg"),
                Path.Combine(".", "Resources", "AdapterWithErrorHandler.lg"),
                Path.Combine(".", "Resources", "SummaryReadout.lg"),
                Path.Combine(".", "Resources", "UserProfileDialog.fr-fr.lg"),
                Path.Combine(".", "Resources", "UserProfileDialog.lg"),
            };

            Use(new RegisterClassMiddleware<LanguageGeneratorManager>(new LanguageGeneratorManager(lgFiles)));

            _lgGenerator = new MultiLingualTemplateEngine("AdapterWithErrorHandler.lg");

            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                logger.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all apology to the user.
                await turnContext.SendActivityAsync(_lgGenerator.GenerateActivity("SomethingWentWrong", exception, turnContext));

                if (conversationState != null)
                {
                    try
                    {
                        // Delete the conversationState for the current conversation to prevent the
                        // bot from getting stuck in a error-loop caused by being in a bad state.
                        // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
                        await conversationState.DeleteAsync(turnContext);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
                    }
                }
            };
        }
    }
}
