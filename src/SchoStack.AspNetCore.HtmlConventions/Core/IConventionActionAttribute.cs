using System;
using HtmlTags;

namespace SchoStack.AspNetCore.HtmlConventions.Core
{
    public interface IConventionActionAttribute<TAttribute>
    {
        void BuildBy(Func<RequestData, TAttribute, HtmlTag> builder);
        void BuildBy(Func<RequestData, TAttribute, IConventionPipeline, HtmlTag> builder);
        void Modify(Action<HtmlTag, RequestData, TAttribute> modifier);
    }
}