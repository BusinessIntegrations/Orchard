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
    /// Imports Enabled and Disabled features
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
        public override void Execute(RecipeExecutionContext recipeContext) {
            var featuresToEnable = new List<string>();
            var featuresToDisable = new List<string>();
            foreach (var element in recipeContext.RecipeStep.Step.Elements()) {
                if (string.Equals(element.Name.LocalName, Constants.Disable, StringComparison.OrdinalIgnoreCase)) {
                    featuresToDisable = ParseFeatures(element);
                }
                else if (string.Equals(element.Name.LocalName, Constants.Enable, StringComparison.OrdinalIgnoreCase)) {
                    featuresToEnable = ParseFeatures(element);
                }
                else {
                    Logger.Warning("Unrecognized attribute '{0}' encountered; skipping", element.Name.LocalName);
                }
            }
            var availableFeatures = _featureManager.GetAvailableFeatures()
                .Select(x => x.Id)
                .ToArray();
            foreach (var featureName in featuresToDisable) {
                if (!availableFeatures.Contains(featureName, StringComparer.Ordinal)) {
                    throw new InvalidOperationException(string.Format("Could not disable feature {0} because it was not found.", featureName));
                }
            }
            foreach (var featureName in featuresToEnable) {
                if (!availableFeatures.Contains(featureName, StringComparer.Ordinal)) {
                    throw new InvalidOperationException(string.Format("Could not enable feature {0} because it was not found.", featureName));
                }
            }
            if (featuresToDisable.Any()) {
                Logger.Information("Disabling features: {0}", featuresToDisable.Count);
                featuresToDisable.ForEach(feature => Logger.Information("    Disabling: {0}", feature));
                _featureManager.DisableFeatures(featuresToDisable, true);
            }
            if (featuresToEnable.Any()) {
                Logger.Information("Enabling features: {0}", featuresToEnable.Count);
                featuresToEnable.ForEach(feature => Logger.Information("    Enabling: {0}", feature));
                _featureManager.EnableFeatures(featuresToEnable, true);
            }
        }

        private static List<string> ParseFeatures(XContainer parentElement) {
            return parentElement.Elements()
                .Where(element => element.Name.LocalName.Equals(Constants.Id, StringComparison.Ordinal))
                .Select(element => element.Value.Trim())
                .Where(value => !string.IsNullOrEmpty(value))
                .ToList();
        }
        #endregion
    }
}
