using System;
using ProjectReferences.Interfaces;
using ProjectReferences.Output.Html;
using ProjectReferences.Output.Yuml;
using ProjectReferences.Shared;

namespace ProjectReferences.Output
{
    public sealed class OutputFactory
    {
        public static IOutputProvider CreateProvider(OutputType outputType)
        {
            Logger.Log(string.Format("Creating IOutputProvider for type: '{0}'", outputType));
            switch (outputType)
            {
                case OutputType.YumlReferenceList:
                    return new YumlReferenceListOutputProvider();
                case OutputType.YumlUrl:
                    return new YumlUrlOutputProvider();
                case OutputType.YumlImage:
                    return new YumlImageOutputProvider();
                case OutputType.HtmlDocument:
                    return new SinglePageHtmlDocumentOutputProvider();
                default:
                    throw new ArgumentOutOfRangeException("outputType");
            }
        }
    }
}
