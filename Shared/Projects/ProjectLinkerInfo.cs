using NLang.DevelopmentKit.Shared.Modules;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace NLang.DevelopmentKit.Shared.Projects;

// TODO: We should make the linker extensible like the other things.
//       We could let somebody make a custom linker to add code from other
//       places not natively supported by NLang. See issue #48.
public class ProjectLinkerInfo
{
    public static ProjectLinkerInfo Default { get; } = new();

    public List<LinkInfoBase> Links { get; } = [];

    public static ProjectLinkerInfo FromProjectFile(XElement linkerInfoRoot)
    {
        ProjectLinkerInfo info = new();
        List<Exception> exceptions = [];
        foreach (XElement reference in linkerInfoRoot.Elements("Reference"))
        {
            XAttribute? type = reference.Attribute("type");
            if (type is null || reference.Value is null)
            {
                exceptions.Add(new("A linker reference must have a type attribute."));
                continue;
            }

            LinkerInfoBase? linker = LinkerInfoBase.Get(type.Value);
            if (linker is null)
            {
                exceptions.Add(new($"The reference type {type.Value} is not known to the linker."));
                continue;
            }

            LinkInfoBase result;
            try
            {
                result = linker.ParseReference(reference);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                continue;
            }

            info.Links.Add(result);
        }

        if (exceptions.Count > 0) throw new AggregateException("One or more errors occurred while parsing the linker info.", exceptions);
        else return info;
    }
}
