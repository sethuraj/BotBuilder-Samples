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
        private readonly Dictionary<string, string> localeToEntryFileMapping;
        private readonly LanguagePolicy languagePolicy;

        public MultiLingualTemplateEngine(Dictionary<string, string> localeToEntryFileMapping)
        {
            this.localeToEntryFileMapping = localeToEntryFileMapping;
            languagePolicy = new LanguagePolicy();
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
            var iLocale = turnContext.Activity.Locale ?? "";

            var locales = GetOptionalLocals(iLocale);

            var filePath = string.Empty;
            foreach (var currentLocale in locales)
            {
                if (localeToEntryFileMapping.TryGetValue(currentLocale, out filePath))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new Exception($"locale {turnContext.Activity.Locale} has no entry lg file.");
            }

            var importResolver = MultiLangResolver(iLocale);
            var engine = new TemplateEngine().AddFile(filePath, importResolver);

            return ActivityFactory.CreateActivity(engine.EvaluateTemplate(templateName, data).ToString());
        }

        private ImportResolverDelegate MultiLangResolver(string locale)
        {
            return (string sourceId, string resourceId) =>
            {
                var importPath = ImportResolver.NormalizePath(resourceId);

                if (!Path.IsPathRooted(importPath))
                {
                    importPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceId), resourceId));
                }

                var locales = GetOptionalLocals(locale);

                foreach (var currentLocale in locales)
                {
                    var newFilePath = string.IsNullOrEmpty(currentLocale) ? importPath : importPath.Replace(".lg", $".{currentLocale}.lg");
                   if (File.Exists(newFilePath))
                    {
                        return (File.ReadAllText(newFilePath), newFilePath);
                    }
                }

                throw new Exception($"can not find file {importPath} with locale {locale}.");
            };
        }

        private string[] GetOptionalLocals(string locale)
        {
            var locales = new string[] { string.Empty };
            if (!languagePolicy.TryGetValue(locale, out locales))
            {
                if (!languagePolicy.TryGetValue(string.Empty, out locales))
                {
                    throw new Exception($"No supported language found for {locale}");
                }
            }

            return locales;
        }
    }
}
