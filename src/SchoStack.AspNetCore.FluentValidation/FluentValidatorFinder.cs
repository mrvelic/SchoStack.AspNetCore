﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentValidation;
using FluentValidation.Validators;
using SchoStack.AspNetCore.HtmlConventions;
using SchoStack.AspNetCore.HtmlConventions.Core;

namespace SchoStack.AspNetCore.FluentValidation
{
#pragma warning disable CS0612 // Type or member is obsolete
    public interface IValidatorFinder
    {
        IEnumerable<PropertyValidatorResult> FindValidators(RequestData r);
    }

    public class FluentValidatorFinder : IValidatorFinder
    {
        private readonly Func<Type, IValidator> _resolver;

        public FluentValidatorFinder(Func<Type, IValidator> resolver)
        {
            _resolver = resolver;
        }

        public IEnumerable<PropertyValidatorResult> FindValidators(RequestData requestData)
        {
            if (requestData.InputType == null)
                return new List<PropertyValidatorResult>();

            var baseValidator = ResolveValidator(requestData.InputType);
            if (baseValidator == null)
                return new List<PropertyValidatorResult>();

            var properties = InputPropertyMatcher.FindPropertyData(requestData);
            var validators = GetPropertyValidators(baseValidator, properties);
            return validators;
        }

        private IEnumerable<PropertyValidatorResult> GetPropertyValidators(IValidator baseValidator, List<PropertyInfo> properties)
        {
            var desc = baseValidator.CreateDescriptor();
            var validators = GetNestedPropertyValidators(desc, properties, 0).ToList();
            return validators;
        }

        private IEnumerable<PropertyValidatorResult> GetNestedPropertyValidators(IValidatorDescriptor desc, List<PropertyInfo> propertyInfo, int i)
        {
            if (i == propertyInfo.Count)
                return new List<PropertyValidatorResult>();

            var vals = desc.GetValidatorsForMember(propertyInfo[i].Name);
            var name = desc.GetName(propertyInfo[i].Name);

            var propertyValidators = new List<PropertyValidatorResult>();

            foreach (var inlineval in vals)
            {
                if (i == propertyInfo.Count - 1)
                {
                    var propertyVal = inlineval is DelegatingValidator delegatingValidator
                        ? delegatingValidator.InnerValidator
                        : inlineval;

                    propertyValidators.Add(new PropertyValidatorResult(propertyVal, name));
                }

                var val = GetValidator(inlineval, null);

                if (val == null)
                    continue;

                var morevals = GetNestedPropertyValidators(val.CreateDescriptor(), propertyInfo, i + 1);
                propertyValidators.AddRange(morevals.Select(x => new PropertyValidatorResult(x.PropertyValidator, x.DisplayName)));
            }

            return propertyValidators;
        }

        private IValidator GetValidator(IPropertyValidator inlineval, IValidator val)
        {
            var valtype = inlineval.GetType();
            if (valtype == typeof(ChildValidatorAdaptor))
                val = GetChildValidator((ChildValidatorAdaptor)inlineval);
            else if (valtype == typeof(DelegatingValidator))
                val = GetValidator(((DelegatingValidator)inlineval).InnerValidator, val);

            return val;
        }

        private IValidator GetChildValidator(ChildValidatorAdaptor adaptor)
        {
            var validatorContext = new ValidationContext(null);
            var propertyValidatorContext = new PropertyValidatorContext(validatorContext, null, null);
            return adaptor.GetValidator(propertyValidatorContext);
        }

        private IValidator ResolveValidator(Type modelType)
        {
            var gentype = typeof(IValidator<>).MakeGenericType(modelType);
            var validator = (IValidator)_resolver(gentype);
            return validator;
        }
    }
#pragma warning enable CS0612 // Type or member is obsolete
}
