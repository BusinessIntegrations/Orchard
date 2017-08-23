using System.Linq;
using Orchard.Data;
using Orchard.Projections.Models;
using Orchard.Projections.Recipes.Builders;
using Orchard.Recipes.Models;
using Orchard.Recipes.Services;

namespace Orchard.Projections.Recipes.Executors {
    public class BindingExecutionStep : RecipeExecutionStep {
        private readonly IRepository<MemberBindingRecord> _repository;

        public BindingExecutionStep(RecipeExecutionLogger logger, IRepository<MemberBindingRecord> repository)
            : base(logger) {
            _repository = repository;
        }

        #region Properties
        public override string Name { get { return BindingBuilderStep.BindingStepName; } }
        #endregion
        
        #region Methods
        public override void Execute(RecipeExecutionContext context) {
            var stepElement = context.RecipeStep.Step;
            foreach (var memberBindingElement in stepElement.Elements(BindingBuilderStep.BindingElementName)) {
                
                var memberBindingRecord = new MemberBindingRecord {
                    Member = memberBindingElement.Attribute(BindingBuilderStep.MemberName)
                        .Value,
                    Type = memberBindingElement.Attribute(BindingBuilderStep.TypeName)
                        .Value,
                    DisplayName = memberBindingElement.Attribute(BindingBuilderStep.DisplayNameName)
                        .Value,
                    Description = memberBindingElement.Attribute(BindingBuilderStep.DescriptionName)
                        .Value
                };
                // Treat record as if it had a unique key on 1st 3 columns
                var q = _repository.Fetch(record => record.Member==memberBindingRecord.Member &&
                record.Type==memberBindingRecord.Type && record.DisplayName == memberBindingRecord.DisplayName).ToList();
                if (!q.Any()) {
                    _repository.Create(memberBindingRecord);
                }
                else {
                    // In theory this should only return one row - if only there was a unique key
                    foreach (var record in q) {
                        memberBindingRecord.Id = record.Id;
                        _repository.Update(memberBindingRecord);
                    }
                    
                }
            }
        }
        #endregion
    }
}
