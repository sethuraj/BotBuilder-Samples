// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;

namespace Microsoft.BotBuilderSamples
{
    public class MultiLingualTemplateEngine
    {
        private readonly string lgFileName;
        private readonly LanguagePolicy languagePolicy;

        public MultiLingualTemplateEngine(string lgFileName)
        {
            languagePolicy = new LanguagePolicy();
            this.lgFileName = Path.GetFileName(lgFileName);
        }

        public Activity GenerateActivity(string templateName, object data, WaterfallStepContext stepContext)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (stepContext == null || stepContext.Context == null || stepContext.Context.Activity == null)
            {
                throw new ArgumentNullException(nameof(stepContext));
            }
            return InternalGenerateActivity(templateName, data, stepContext.Context);

        }

        public Activity GenerateActivity(string templateName, object data, ITurnContext turnContext)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (turnContext == null || turnContext.Activity == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }
            return InternalGenerateActivity(templateName, data, turnContext);
        }

        public Activity GenerateActivity(string templateName, WaterfallStepContext stepContext)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            if (stepContext == null || stepContext.Context == null || stepContext.Context.Activity == null)
            {
                throw new ArgumentNullException(nameof(stepContext));
            }
            return InternalGenerateActivity(templateName, null, stepContext.Context);
        }

        public Activity GenerateActivity(string templateName, TurnContext turnContext)
        {
            if (templateName == null)
            {
                throw new ArgumentNullException(nameof(templateName));
            }

            if (turnContext == null || turnContext.Activity == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }
            return InternalGenerateActivity(templateName, null, turnContext);
        }

        private Activity InternalGenerateActivity(string templateName, object data, ITurnContext turnContext)
        {
            var lgm = turnContext.TurnState.Get<LanguageGeneratorManager>();
            var iLocale = turnContext.Activity.Locale ?? "";

            var locales = new string[] { string.Empty };
            if (!languagePolicy.TryGetValue(iLocale, out locales))
            {
                if (!languagePolicy.TryGetValue(string.Empty, out locales))
                {
                    throw new Exception($"No supported language found for {iLocale}");
                }
            }

            var templateEngines = new List<TemplateEngine>();
            foreach (var currentLocale in locales)
            {
                var resourceId = string.IsNullOrEmpty(currentLocale) ? lgFileName : lgFileName.Replace(".lg", $".{currentLocale}.lg");
                if (lgm.engines.TryGetValue(resourceId, out var engine))
                {
                    templateEngines.Add(engine);
                }
            }

            if (templateEngines.Count == 0)
            {
                throw new Exception($"No template engine found for language {iLocale}");
            }

            var errors = new List<string>();
            foreach (var engine in templateEngines)
            {
                try
                {
                    return ActivityFactory.CreateActivity(engine.EvaluateTemplate(templateName, data).ToString());
                }
                catch (Exception err)
                {
                    errors.Add(err.Message);
                }
            }

            return new Activity();
        }
    }
}
