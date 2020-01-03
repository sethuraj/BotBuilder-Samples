using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs.Adaptive;

namespace Microsoft.BotBuilderSamples
{
    public class LGResourceLoader
    {
        public static Dictionary<string, IList<string>> GroupByLocale(IList<string> lgFiles)
        {
            var resourceMapping = new Dictionary<string, IList<string>>();
            var languagePolicy = new LanguagePolicy();
            foreach (var item in languagePolicy)
            {
                var locale = item.Key;
                var suffixs = item.Value;
                var existNames = new HashSet<string>();
                foreach (var suffix in suffixs)
                {
                    if (string.IsNullOrEmpty(locale) || !string.IsNullOrEmpty(suffix))
                    {
                        var resourcesWithSuchSuffix = lgFiles.Where(u => ParseLGFileName(u).language == suffix);
                        foreach (var filePath in resourcesWithSuchSuffix)
                        {
                            var fileName = Path.GetFileName(filePath);
                            var length = string.IsNullOrEmpty(suffix) ? 3 : 4;
                            var prefixName = fileName.Substring(0, fileName.Length - suffix.Length - length);
                            if (!existNames.Contains(prefixName))
                            {
                                existNames.Add(prefixName);
                                if (!resourceMapping.ContainsKey(locale))
                                {
                                    resourceMapping[locale] = new List<string> { filePath };
                                }
                                else
                                {
                                    resourceMapping[locale].Add(filePath);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (resourceMapping.ContainsKey(locale))
                        {
                            var resourcesWithEmptySuffix = lgFiles.Where(u => ParseLGFileName(u).language == string.Empty);
                            foreach (var filePath in resourcesWithEmptySuffix)
                            {
                                var fileName = Path.GetFileName(filePath);
                                var prefixName = fileName.Substring(0, fileName.Length - 3);
                                if (!existNames.Contains(prefixName))
                                {
                                    existNames.Add(prefixName);
                                    resourceMapping[locale].Add(filePath);
                                }
                            }
                        }
                    }
                }
            }

            return FallbackMultiLangResource(resourceMapping);
        }

        /// <summary>
        /// parse lg file name into prefix and language.
        /// </summary>
        /// <param name="lgFileName">lg input name.</param>
        /// <returns>get the name and language.</returns>
        public static (string prefix, string language) ParseLGFileName(string lgFileName)
        {
            if (string.IsNullOrEmpty(lgFileName) || !lgFileName.EndsWith(".lg"))
            {
                return (lgFileName, string.Empty);
            }

            var fileName = Path.GetFileName(lgFileName);
            fileName = fileName.Substring(0, fileName.Length - ".lg".Length);

            var lastDot = fileName.LastIndexOf(".");
            if (lastDot > 0)
            {
                return (fileName.Substring(0, lastDot), fileName.Substring(lastDot + 1));
            }
            else
            {
                return (fileName, string.Empty);
            }
        }

        /// <summary>
        /// fallback resource.
        /// for example, en-us -> [1.en.lg, 2.lg].   en -> [1.en.lg, 2.lg]
        /// result will be :en -> [1.en.lg, 2.lg]. and use fallback to find the resources.
        /// </summary>
        /// <param name="resourceMapping">input resource mapping.</param>
        /// <returns>merged resource mapping.</returns>
        private static Dictionary<string, IList<string>> FallbackMultiLangResource(Dictionary<string, IList<string>> resourceMapping)
        {
            var resourcePoolDict = new Dictionary<string, IList<string>>();
            foreach (var languageItem in resourceMapping)
            {
                var currentLocale = languageItem.Key;
                var currentResourcePool = languageItem.Value;
                var sameResourcePool = resourcePoolDict.FirstOrDefault(u => HasSameResourcePool(u.Value, languageItem.Value));
                var existLocale = sameResourcePool.Key;

                if (existLocale == null)
                {
                    resourcePoolDict.Add(currentLocale, currentResourcePool);
                }
                else
                {
                    var newLocale = FindCommonAncestorLocale(existLocale, currentLocale);
                    if (!string.IsNullOrWhiteSpace(newLocale) && newLocale != existLocale)
                    {
                        resourcePoolDict.Remove(existLocale);
                        resourcePoolDict.Add(newLocale, currentResourcePool);
                    }
                }
            }

            return resourcePoolDict;
        }

        /// <summary>
        /// find the common parent locale, for example
        /// en-us, en-gb, has the same parent locale: en.
        /// and en-us, fr, has the no same parent locale.
        /// </summary>
        /// <param name="locale1">first locale.</param>
        /// <param name="locale2">second locale.</param>
        /// <returns>the most closest common ancestor local.</returns>
        private static string FindCommonAncestorLocale(string locale1, string locale2)
        {
            var policy = new LanguagePolicy();
            if (!policy.ContainsKey(locale1) || !policy.ContainsKey(locale2))
            {
                return string.Empty;
            }

            var key1Policy = policy[locale1];
            var key2Policy = policy[locale2];
            foreach (var key1Language in key1Policy)
            {
                foreach (var key2Language in key2Policy)
                {
                    if (key1Language == key2Language)
                    {
                        return key1Language;
                    }
                }
            }

            return string.Empty;
        }

        private static bool HasSameResourcePool(IList<string> resourceMapping1, IList<string> resourceMapping2)
        {
            if (resourceMapping1 == null && resourceMapping2 == null)
            {
                return true;
            }

            if ((resourceMapping1 == null && resourceMapping2 != null)
                || (resourceMapping1 != null && resourceMapping2 == null)
                || resourceMapping1.Count != resourceMapping2.Count)
            {
                return false;
            }

            resourceMapping1 = resourceMapping1.OrderBy(u => u).ToList();
            resourceMapping2 = resourceMapping2.OrderBy(u => u).ToList();

            for (var i = 0; i < resourceMapping1.Count; i++)
            {
                if (resourceMapping1[i] != resourceMapping2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
