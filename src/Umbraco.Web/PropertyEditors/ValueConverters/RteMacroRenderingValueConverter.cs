﻿using System.Text;
using Umbraco.Core;
using Umbraco.Core.Macros;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Umbraco.Web.Templates;
using System.Linq;
using HtmlAgilityPack;
using Umbraco.Core.Cache;
using Umbraco.Core.Services;
using Umbraco.Web.Composing;
using Umbraco.Web.Macros;

namespace Umbraco.Web.PropertyEditors.ValueConverters
{
    /// <summary>
    /// A value converter for TinyMCE that will ensure any macro content is rendered properly even when
    /// used dynamically.
    /// </summary>
    [DefaultPropertyValueConverter]
    public class RteMacroRenderingValueConverter : TinyMceValueConverter
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IMacroRenderer _macroRenderer;

        public override PropertyCacheLevel GetPropertyCacheLevel(PublishedPropertyType propertyType)
        {
            // because that version of RTE converter parses {locallink} and executes macros, its value has
            // to be cached at the published snapshot level, because we have no idea what the macros may depend on actually.
            return PropertyCacheLevel.Snapshot;
        }

        public RteMacroRenderingValueConverter(IUmbracoContextAccessor umbracoContextAccessor, IMacroRenderer macroRenderer)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _macroRenderer = macroRenderer;
        }

        // NOT thread-safe over a request because it modifies the
        // global UmbracoContext.Current.InPreviewMode status. So it
        // should never execute in // over the same UmbracoContext with
        // different preview modes.
        private string RenderRteMacros(string source, bool preview)
        {
            var umbracoContext = _umbracoContextAccessor.UmbracoContext;
            using (umbracoContext.ForcedPreview(preview)) // force for macro rendering
            {
                var sb = new StringBuilder();

                MacroTagParser.ParseMacros(
                    source,
                    //callback for when text block is found
                    textBlock => sb.Append(textBlock),
                    //callback for when macro syntax is found
                    (macroAlias, macroAttributes) => sb.Append(_macroRenderer.Render(
                        macroAlias,
                        umbracoContext.PublishedRequest?.PublishedContent,
                        //needs to be explicitly casted to Dictionary<string, object>
                        macroAttributes.ConvertTo(x => (string)x, x => x)).GetAsText()));

                return sb.ToString();
            }
        }

        public override object ConvertSourceToIntermediate(IPublishedElement owner, PublishedPropertyType propertyType, object source, bool preview)
        {
            if (source == null)
            {
                return null;
            }

            var sourceString = source.ToString();

            // ensures string is parsed for {localLink} and urls are resolved correctly
            sourceString = TemplateUtilities.ParseInternalLinks(sourceString, preview, Current.UmbracoContext);
            sourceString = TemplateUtilities.ResolveUrlsFromTextString(sourceString);

            // ensure string is parsed for macros and macros are executed correctly
            sourceString = RenderRteMacros(sourceString, preview);

            // find and remove the rel attributes used in the Umbraco UI from img tags
            var doc = new HtmlDocument();
            doc.LoadHtml(sourceString);

            if (doc.ParseErrors.Any() == false && doc.DocumentNode != null)
            {
                // Find all images with rel attribute
                var imgNodes = doc.DocumentNode.SelectNodes("//img[@rel]");

                if (imgNodes != null)
                {
                    var modified = false;

                    foreach (var img in imgNodes)
                    {
                        var firstOrDefault = img.Attributes.FirstOrDefault(x => x.Name == "rel");
                        if (firstOrDefault != null)
                        {
                            var rel = firstOrDefault.Value;

                            // Check that the rel attribute is a integer before removing
                            int nodeId;
                            if (int.TryParse(rel, out nodeId))
                            {
                                img.Attributes.Remove("rel");
                                modified = true;
                            }
                        }
                    }

                    if (modified)
                    {
                        return doc.DocumentNode.OuterHtml;
                    }
                }
            }

            return sourceString;
        }
    }
}
