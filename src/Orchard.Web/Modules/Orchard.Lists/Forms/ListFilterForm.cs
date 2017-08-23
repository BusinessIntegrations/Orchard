
using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Orchard.ContentManagement;
using Orchard.Core.Containers.Models;
using Orchard.Core.Containers.Services;
using Orchard.DisplayManagement;
using Orchard.Lists.Services;

namespace Orchard.Lists.Forms {
    public class ListFilterForm : Component, IFormProvider {
        private readonly IContainerService _containerService;
        private readonly IContentManager _contentManager;

        public ListFilterForm(IShapeFactory shapeFactory, IContainerService containerService, IContentManager contentManager) {
            _containerService = containerService;
            _contentManager = contentManager;
            New = shapeFactory;
        }
        protected dynamic New { get; set; }

        public void Describe(dynamic context) {
            Func<IShapeFactory, object> form =
                shape => {
                    var f = New.Form(
                        Id: "List",
                        _Lists: New.SelectList(
                            Id: "listId", Name: "ListId",
                            Title: T("List"),
                            Description: T("Select a list."),
                            Multiple: false));

                    foreach (var list in _containerService.GetContainers(VersionOptions.Latest).OrderBy(GetListName)) {
                        f._Lists.Add(new SelectListItem {Value = list.Id.ToString(CultureInfo.InvariantCulture), Text = GetListName(list)});
                    }

                    return f;
                };

            Action<dynamic, ImportContentContext> importing = Importing;
            Action<dynamic, ExportContentContext> exporting = Exporting;
            context.Form("ListFilter", form, importing, exporting);
        }
        
        private void Importing(dynamic dynamic, ImportContentContext importContentContext) {
            var jObject = dynamic as JObject;
            if (jObject != null) {
                var jToken = jObject["ListId"];
                if (jToken != null) {
                    try {
                        // This could throw an exception if the ListID is not a string (...)
                        var listID = jToken.Value<string>();
                        // if it doesn't, see if it is a valid IdentityPart and Identifier, if so, replace it with the contentid
                        var contentItem = importContentContext.GetItemFromSession(listID);
                        if (contentItem != null) {
                            jObject["ListId"] = contentItem.Id;
                        }
                    }
                    catch (Exception ex) {
                        Logger.Log(LogLevel.Debug, ex, "Could not parse ListId [{0}] when importing query content", jToken.ToString());
                    }
                }
            }
        }
        
        private void Exporting(dynamic dynamic, ExportContentContext exportContentContext) {
            var jObject = dynamic as JObject;
            if (jObject != null) {
                var jToken = jObject["ListId"];
                if (jToken != null) {
                    try {
                        // This could throw an exception if the ListID is not a number
                        var listID = jToken.Value<int>();

                        // if it doesn't, use its Identifier value instead of contentid for the export
                        var contentItem = _contentManager.Get(listID);
                        if (contentItem != null) {
                            var identifier = _contentManager.GetItemMetadata(contentItem)
                                .Identity.ToString();
                            if (!string.IsNullOrEmpty(identifier)) {
                                jObject["ListId"] = identifier;
                            }
                        }
                    }
                    catch (Exception ex) {
                        Logger.Log(LogLevel.Debug, ex, "Could not parse ListId [{0}] when exporting query content", jToken.ToString());
                    }
                }
            }
        }
        
        private string GetListName(ContainerPart containerPart) {
            return _contentManager.GetItemMetadata(containerPart).DisplayText;
        }
    }
}
