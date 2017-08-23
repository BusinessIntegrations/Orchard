using System.Xml.Linq;
using Orchard.Localization;
using Orchard.Projections.Services;
using Orchard.Recipes.Services;

namespace Orchard.Projections.Recipes.Builders {
    public class BindingBuilderStep : RecipeBuilderStep {
        internal const string DescriptionName = "Description";
        internal const string DisplayNameName = "DisplayName";
        internal const string BindingElementName = "Binding";
        internal const string MemberName = "Member";
        internal const string BindingStepName = "QueryMemberBindings";
        internal const string TypeName = "Type";
        private readonly IMemberBindingProvider _memberBindingProvider;

        public BindingBuilderStep(IMemberBindingProvider memberBindingProvider) {
            _memberBindingProvider = memberBindingProvider;
        }

        #region Properties
        public override LocalizedString Description { get { return T("Exports Query member bindings."); } }
        public override LocalizedString DisplayName { get { return T("Query Bindings"); } }
        public override string Name { get { return BindingStepName; } }
        #endregion

        #region Methods
        public override void Build(BuildContext context) {
            var xElement = context.RecipeDocument.Element("Orchard");
            if (xElement != null) {
                var root = new XElement(BindingStepName);
                AddBindings(root);
                if (root.HasElements) {
                    xElement.Add(root);
                }
            }
        }

        private void AddBindings(XContainer root) {
            var bindingBuilder = new BindingBuilder();
            _memberBindingProvider.GetMemberBindings(bindingBuilder);
            foreach (var bindingItem in bindingBuilder.Build()) {
                var declaringType = bindingItem.Property.DeclaringType;
                if (declaringType != null) {
                    var memberBinding = new XElement(BindingElementName, new XAttribute(TypeName, declaringType.FullName), new XAttribute(MemberName, bindingItem.Property.Name), new XAttribute(DisplayNameName, bindingItem.DisplayName), new XAttribute(DescriptionName, bindingItem.Description));
                    root.Add(memberBinding);
                }
            }
        }
        #endregion
    }
}
