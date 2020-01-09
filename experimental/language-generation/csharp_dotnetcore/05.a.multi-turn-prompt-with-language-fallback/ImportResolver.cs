using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class ImportResolver
    {
        /// <summary>
        /// Default resolver
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        public static ImportResolverDelegate DefaultFileResolver(string locale)
        {
            return (string sourceId, string resourceId) =>
            {
                var importPath = Bot.Builder.LanguageGeneration.ImportResolver.NormalizePath(resourceId);

                if (!Path.IsPathRooted(importPath))
                {
                    // get full path for importPath relative to path which is doing the import.
                    importPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourceId), resourceId));
                }

                return (File.ReadAllText(importPath), importPath);
            };
        }

        /// <summary>
        /// multi language resolver provided by lg team.
        /// </summary>
        /// <param name="locale"></param>
        /// <returns></returns>
        public static ImportResolverDelegate MultiLangResolver(string locale)
        {
            return (string sourceId, string resourceId) =>
            {
                var importPath = Bot.Builder.LanguageGeneration.ImportResolver.NormalizePath(resourceId);

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

        private static string[] GetOptionalLocals(string locale)
        {
            var languagePolicy = new LanguagePolicy();
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
