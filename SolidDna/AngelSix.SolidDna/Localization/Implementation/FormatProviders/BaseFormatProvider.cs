﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AngelSix.SolidDna
{
    /// <summary>
    /// A base class adding caching ability and helper functions to IResourceFormatProvider implementations
    /// </summary>
    public class BaseFormatProvider
    {
        #region Protected Members

        /// <summary>
        /// A list of all cached resources
        /// </summary>
        protected Dictionary<string, object> Cache;

        /// <summary>
        /// If true, resource files are cached in memory
        /// </summary>
        protected bool CacheResourceFiles;

        #endregion

        #region Protected Helpers

        /// <summary>
        /// Gets the file data for the specified resource, storing a cached version if specified
        /// IMPORTANT:
        /// NOTE: Make sure any and all await calls inside this function and its children
        ///       use ConfigureAwait(false). This is because the parent has to support 
        ///       a synchronous version of this call, so the method cannot sync back with
        ///       it's calling context without risk of deadlock
        /// </summary>
        /// <param name="pathFormat">The path to the desired resource, containing {0} in place of the culture</param>
        /// <param name="constructData">The function that takes the file data and converts it into the format required by the provider</param>
        /// <param name="culture">The culture to use. If not specified, the default culture is used</param>
        /// <returns></returns>
        protected async Task<T> GetResourceDocument<T>(ResourceDefinition pathFormat, Func<Stream, T> constructData, string culture = null)
        {
            // Get culture path 
            var resourcePath = ResourceFormatProviderHelpers.GetCulturePath(pathFormat.Location, culture);

            // Make sure list has been initialized
            if (Cache == null)
                Cache = new Dictionary<string, object>();

            // If we have a document already, return that
            if (Cache.ContainsKey(resourcePath))
                return (T)Cache[resourcePath];

            // Otherwise try and get it
            T resourceDocument = default(T);

            try
            {
                // Try to get the stream for this resource
                using (var stream = await ResourceFormatProviderHelpers.GetStream(pathFormat.Type, resourcePath).ConfigureAwait(false))
                {
                    // If successfully try and convert that data into a usable resource object
                    if (stream != null)
                        resourceDocument = constructData(stream);
                }
            }
            finally
            {
                // Add resource document if found or not (except for Urls, never add failed Urls in case of bad internet connection
                if (CacheResourceFiles && pathFormat.Type != ResourceDefinitionType.Url)
                    Cache.Add(resourcePath, resourceDocument);
            }

            // Return what we found, if anything
            return resourceDocument;
        }

        #endregion
    }
}
