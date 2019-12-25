// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Builder.LanguageGeneration;
using System;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.BotBuilderSamples
{
    public class MultiLingualTemplateEngine
    {
        public Dictionary<string, TemplateEngine> TemplateEnginesPerLocale { get; set; } = new Dictionary<string, TemplateEngine>();
        private LanguagePolicy LangFallBackPolicy;

        public MultiLingualTemplateEngine(Dictionary<string, string> lgFilesPerLocale)
        {
            if (lgFilesPerLocale == null)
            {
                throw new ArgumentNullException(nameof(lgFilesPerLocale));
            }

            LangFallBackPolicy = new LanguagePolicy();

            foreach (var filesPerLocale in lgFilesPerLocale)
            {
                var localeIn = GetLocaleFromFileName(Path.GetFileName(filesPerLocale.Value));
                TemplateEnginesPerLocale[filesPerLocale.Key] = new TemplateEngine().AddFile(filesPerLocale.Value, MultiLingualFileResolver(localeIn));
            }
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
            return InternalGenerateActivity(templateName, data, stepContext.Context.Activity.Locale);

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
            return InternalGenerateActivity(templateName, data, turnContext.Activity.Locale);
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
            return InternalGenerateActivity(templateName, null, stepContext.Context.Activity.Locale);
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
            return InternalGenerateActivity(templateName, null, turnContext.Activity.Locale);
        }

        private Activity InternalGenerateActivity(string templateName, object data, string locale)
        {
            var iLocale = locale ?? "";

            if (TemplateEnginesPerLocale.ContainsKey(iLocale))
            {
                return ActivityFactory.CreateActivity(TemplateEnginesPerLocale[locale].EvaluateTemplate(templateName, data).ToString());
            }
            var locales = GetOptionalLocals(iLocale);

            foreach (var fallBackLocale in locales)
            {
                if (TemplateEnginesPerLocale.ContainsKey(fallBackLocale))
                {
                    return ActivityFactory.CreateActivity(TemplateEnginesPerLocale[fallBackLocale].EvaluateTemplate(templateName, data).ToString());
                }
            }
            return new Activity();
        }


        private string[] GetOptionalLocals(string iLocale)
        {
            if (!LangFallBackPolicy.TryGetValue(iLocale, out var locales))
            {
                if (!LangFallBackPolicy.TryGetValue(string.Empty, out locales))
                {
                    throw new Exception($"No supported language found for {iLocale}");
                }
            }

            return locales;
        }

        private ImportResolverDelegate MultiLingualFileResolver(string locale)
        {
            return (string sourceId, string resourceId) =>
            {
                // import paths are in resource files which can be executed on multiple OS environments
                // normalize to map / & \ in importPath -> OSPath
                var importPath = NormalizePath(resourceId);

                if (!Path.IsPathRooted(importPath))
                {
                    // get full path for importPath relative to path which is doing the import.
                    importPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceId), resourceId));
                }

                var locales = GetOptionalLocals(locale);

                foreach (var fallBackLocale in locales)
                {
                    var newFilePath = string.IsNullOrEmpty(fallBackLocale) ? importPath : importPath.Replace(".lg", $".{fallBackLocale}.lg");
                    if (File.Exists(newFilePath))
                    {
                        return (File.ReadAllText(importPath), newFilePath);
                    }
                }

                throw new Exception($"Something is wrong when import {resourceId} from {sourceId}");
            };
        }

        private string NormalizePath(string ambigiousPath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // map linux/mac sep -> windows
                return ambigiousPath.Replace("/", "\\");
            }
            else
            {
                // map windows sep -> linux/mac
                return ambigiousPath.Replace("\\", "/");
            }
        }

        private string GetLocaleFromFileName(string lgFileName)
        {
            if (string.IsNullOrEmpty(lgFileName) || !lgFileName.EndsWith(".lg"))
            {
                return string.Empty;
            }

            var fileName = lgFileName.Substring(0, lgFileName.Length - ".lg".Length);

            var lastDot = fileName.LastIndexOf(".");
            if (lastDot > 0)
            {
                return fileName.Substring(lastDot + 1);
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
