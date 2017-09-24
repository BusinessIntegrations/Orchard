#region Using
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Orchard.ContentManagement;
using Orchard.Environment.Extensions.Models;
using Orchard.Environment.Features;
using Orchard.Localization;
using Orchard.Modules.ViewModels;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;
#endregion

namespace Orchard.Modules.Recipes.Builders {
    /// <summary>
    /// Exports enabled and disabled features
    /// </summary>
    public class ExportFeatureStep : RecipeBuilderStep {
        private readonly IFeatureManager _featureManager;

        public ExportFeatureStep(IFeatureManager featureManager) {
            _featureManager = featureManager;
            ExportEnabledFeatures = true;
        }

        #region Properties
        public override LocalizedString Description { get { return T("Exports enabled and disabled features."); } }
        public override LocalizedString DisplayName { get { return T("Features"); } }
        public override string Name { get { return Constants.Feature; } }
        public override int Position { get { return 70; } }
        public override int Priority { get { return 500; } }
        internal bool ExportDisabledFeatures { get; set; }
        internal bool ExportEnabledFeatures { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Generates nested elements of enabled and disabled features
        /// </summary>
        /// <param name="context"></param>
        public override void Build(BuildContext context) {
            if (!ExportEnabledFeatures &&
                !ExportDisabledFeatures) {
                return;
            }
            var orchardElement = context.RecipeDocument.Element(Constants.Orchard);
            if (orchardElement != null) {
                var root = new XElement(Constants.Feature);
                if (ExportEnabledFeatures) {
                    AddFeatures(Constants.Enable, root, _featureManager.GetEnabledFeatures());
                }
                if (ExportDisabledFeatures) {
                    AddFeatures(Constants.Disable, root, _featureManager.GetDisabledFeatures());
                }
                orchardElement.Add(root);
            }
        }

        public override dynamic BuildEditor(dynamic shapeFactory) {
            return UpdateEditor(shapeFactory, null);
        }

        public override void Configure(RecipeBuilderStepConfigurationContext context) {
            ExportEnabledFeatures = context.ConfigurationElement.Attr<bool>("ExportEnabledFeatures");
            ExportDisabledFeatures = context.ConfigurationElement.Attr<bool>("ExportDisabledFeatures");
        }

        public override void ConfigureDefault() {
            ExportEnabledFeatures = true;
            ExportDisabledFeatures = false;
        }

        public override dynamic UpdateEditor(dynamic shapeFactory, IUpdateModel updater) {
            var viewModel = new FeatureStepViewModel {
                ExportEnabledFeatures = ExportEnabledFeatures,
                ExportDisabledFeatures = ExportDisabledFeatures
            };
            if (updater != null &&
                updater.TryUpdateModel(viewModel, Prefix, null, null)) {
                ExportEnabledFeatures = viewModel.ExportEnabledFeatures;
                ExportDisabledFeatures = viewModel.ExportDisabledFeatures;
            }
            return shapeFactory.EditorTemplate(TemplateName: "BuilderSteps/Feature", Model: viewModel, Prefix: Prefix);
        }

        private static void AddFeatures(string xName, XContainer root, IEnumerable<FeatureDescriptor> enabledFeatures) {
            var enabledChild = new XElement(xName);
            foreach (var s in enabledFeatures.Select(x => x.Id)
                .OrderBy(x => x)) {
                enabledChild.Add(new XElement(Constants.Id, s));
            }
            root.Add(enabledChild);
        }
        #endregion
    }
}
