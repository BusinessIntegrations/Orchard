#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Orchard.Environment.Features;
using Orchard.Logging;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;
#endregion

namespace Orchard.Modules.Recipes.Executors {
    /// <summary>
    ///     Imports Enabled and Disabled features
    /// </summary>
    public class ImportFeatureStep : RecipeExecutionStep {
        private readonly IFeatureManager _featureManager;

        public ImportFeatureStep(IFeatureManager featureManager, RecipeExecutionLogger logger)
            : base(logger) {
            _featureManager = featureManager;
        }

        #region Properties
        public override string Name { get { return Constants.Feature; } }
        #endregion

        #region Methods
        // <Feature> <enable><ID>f1</ID><ID>f2</ID></enable> <disable><ID>f2</ID><ID>f4</ID></disable> </Feature>
        // Enable/Disable features.
        /* Andy Mason 2018-04-03
         Converted this method to use xml elements for each individual feature, rather than the original concatenated 
         string list of features stored in a single attribute.
         Enabled processing of imports to check first for attributes in cases where an old export is being imported.
         If attributes are found then element processing is not performed.
         */
        public override void Execute(RecipeExecutionContext recipeContext) {
            List<string> featuresToEnable = null;
            List<string> featuresToDisable = null;
            var foundEnabledFeatures = false;
            var foundDisabledFeatures = false;
            // Add checks to process original concatenated-string versions in xml attributes
            foreach (var attribute in recipeContext.RecipeStep.Step.Attributes()) {
                if (string.Equals(attribute.Name.LocalName, "disable", StringComparison.OrdinalIgnoreCase)) {
                    featuresToDisable = ParseFeatures(attribute.Value);
                    foundDisabledFeatures = true;
                }
                else if (string.Equals(attribute.Name.LocalName, "enable", StringComparison.OrdinalIgnoreCase)) {
                    featuresToEnable = ParseFeatures(attribute.Value);
                    foundEnabledFeatures = true;
                }
                else {
                    Logger.Warning("Unrecognized attribute '{0}' encountered; skipping", attribute.Name.LocalName);
                }
            }

            // Process new-format xml elements if corresponding attribute hasn't already been found.
            foreach (var element in recipeContext.RecipeStep.Step.Elements()) {
                if (string.Equals(element.Name.LocalName, Constants.Disable, StringComparison.OrdinalIgnoreCase)) {
                    if (!foundDisabledFeatures) {
                        featuresToDisable = ParseFeatures(element);
                    }
                }
                else if (string.Equals(element.Name.LocalName, Constants.Enable, StringComparison.OrdinalIgnoreCase)) {
                    if (!foundEnabledFeatures) {
                        featuresToEnable = ParseFeatures(element);
                    }
                }
                else {
                    Logger.Warning("Unrecognized attribute '{0}' encountered; skipping", element.Name.LocalName);
                }
            }

            var availableFeatures = _featureManager.GetAvailableFeatures()
                .Select(x => x.Id)
                .ToArray();
            // Check that all features are valid before attempting any operation
            if (featuresToDisable != null) {
                featuresToDisable.ForEach(featureName => {
                    if (!availableFeatures.Contains(featureName, StringComparer.Ordinal)) {
                        throw new InvalidOperationException(string.Format("Could not disable feature {0} because it was not found.", featureName));
                    }
                });
            }

            if (featuresToEnable != null) {
                foreach (var featureName in featuresToEnable) {
                    if (!availableFeatures.Contains(featureName, StringComparer.Ordinal)) {
                        throw new InvalidOperationException(string.Format("Could not enable feature {0} because it was not found.", featureName));
                    }
                }
            }

            // Perform disabling and enabling of features
            if (featuresToDisable != null &&
                featuresToDisable.Any()) {
                Logger.Information("Disabling features: {0}", featuresToDisable.Count);
                featuresToDisable.ForEach(feature => Logger.Information("    Disabling: {0}", feature));
                _featureManager.DisableFeatures(featuresToDisable, true);
            }

            if (featuresToEnable != null &&
                featuresToEnable.Any()) {
                Logger.Information("Enabling features: {0}", featuresToEnable.Count);
                featuresToEnable.ForEach(feature => Logger.Information("    Enabling: {0}", feature));
                _featureManager.EnableFeatures(featuresToEnable, true);
            }
        }

        /// <summary>
        ///     Element processing of features
        /// </summary>
        /// <param name="parentElement"></param>
        /// <returns></returns>
        private static List<string> ParseFeatures(XContainer parentElement) {
            return parentElement.Elements()
                .Where(element => element.Name.LocalName.Equals(Constants.Id, StringComparison.Ordinal))
                .Select(element => element.Value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList();
        }

        /// <summary>
        ///     Original concatenated-string processing of features
        /// </summary>
        /// <param name="csv"></param>
        /// <returns></returns>
        private static List<string> ParseFeatures(string csv) {
            return csv.Split(',')
                .Select(value => value.Trim())
                .Where(sanitizedValue => !string.IsNullOrEmpty(sanitizedValue))
                .ToList();
        }
        #endregion
    }
}
