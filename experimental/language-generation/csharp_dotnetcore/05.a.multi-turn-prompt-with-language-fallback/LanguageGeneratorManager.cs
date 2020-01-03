using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.LanguageGeneration;

namespace Microsoft.BotBuilderSamples
{
    public class LanguageGeneratorManager
    {
        private readonly Dictionary<string, IList<string>> multilanguageResources;

        /// <summary>
        /// Initializes a new instance of the <see cref="LanguageGeneratorManager"/> class.
        /// </summary>
        /// <param name="resourceExplorer">resourceExplorer to manage LG files from.</param>
        public LanguageGeneratorManager(IList<string> filePaths)
        {
            multilanguageResources = LGResourceLoader.GroupByLocale(filePaths);

            foreach (var lgFile in filePaths)
            {
                engines[Path.GetFileName(lgFile)] = TemplateEngineLanguageGenerator(lgFile);
            }
        }

        public ConcurrentDictionary<string, TemplateEngine> engines { get; set; } = new ConcurrentDictionary<string, TemplateEngine>(StringComparer.OrdinalIgnoreCase);

        private TemplateEngine TemplateEngineLanguageGenerator(string filePath)
        {
            filePath = PathUtils.NormalizePath(filePath);

            var (_, locale) = LGResourceLoader.ParseLGFileName(Path.GetFileName(filePath));
            var importResolver = ResourceExplorerResolver(locale);
            return new TemplateEngine().AddFile(filePath, importResolver);
        }

        private ImportResolverDelegate ResourceExplorerResolver(string locale)
        {
            return (string source, string id) =>
            {
                var fallbackLocale = FallbackLocale(locale, multilanguageResources.Keys.ToList());
                var resources = multilanguageResources[fallbackLocale];

                var resourceName = Path.GetFileName(PathUtils.NormalizePath(id));

                var resource = resources.FirstOrDefault(u => LGResourceLoader.ParseLGFileName(u).prefix.ToLower() == LGResourceLoader.ParseLGFileName(resourceName).prefix.ToLower());
                if (resource == null)
                {
                    throw new Exception($"There is no matching LG resource for {resourceName}");
                }
                else
                {
                    return (File.ReadAllText(resource), resource);
                }
            };
        }

        /// <summary>
        /// Get the fall back locale from the optional locales. for example
        /// en-us, is a locale from English. But the option locales has [en, ''],
        /// So,en would be picked.
        /// </summary>
        /// <param name="locale">current locale.</param>
        /// <param name="optionalLocales">option locales.</param>
        /// <returns>the final locale.</returns>
        private string FallbackLocale(string locale, IList<string> optionalLocales)
        {
            if (optionalLocales == null)
            {
                throw new ArgumentNullException();
            }

            if (optionalLocales.Contains(locale))
            {
                return locale;
            }

            var languagePolicy = new LanguagePolicy();

            if (languagePolicy.ContainsKey(locale))
            {
                var fallbackLocals = languagePolicy[locale];
                foreach (var fallbackLocal in fallbackLocals)
                {
                    if (optionalLocales.Contains(fallbackLocal))
                    {
                        return fallbackLocal;
                    }
                }
            }
            else if (optionalLocales.Contains(string.Empty))
            {
                return string.Empty;
            }

            throw new Exception($"there is no locale fallback for {locale}");
        }
    }
}
