using System.Collections.Generic;
using Autodesk.Forge.DesignAutomation.Model;

namespace Interaction
{
    /// <summary>
    /// Customizable part of Publisher class.
    /// </summary>
    internal partial class Publisher
    {
        /// <summary>
        /// Constants.
        /// </summary>
        private static class Constants
        {
            private const int EngineVersion = 24;
            public static readonly string Engine = $"Autodesk.Inventor+{EngineVersion}";

            public const string Description = "PUT DESCRIPTION HERE";

            internal static class Bundle
            {
                public static readonly string Id = "iLogicFormExtractor";
                public const string Label = "alpha";

                public static readonly AppBundle Definition = new AppBundle
                {
                    Engine = Engine,
                    Id = Id,
                    Description = Description
                };
            }

            internal static class Activity
            {
                public static readonly string Id = Bundle.Id;
                public const string Label = Bundle.Label;
            }

            internal static class Parameters
            {
                public const string InputDoc = nameof(InputDoc);
                public const string OutputJson = nameof(OutputJson);
            }
        }


        /// <summary>
        /// Get command line for activity.
        /// </summary>
        private static List<string> GetActivityCommandLine()
        {
            return new List<string> { $"$(engine.path)\\InventorCoreConsole.exe /al $(appbundles[{Constants.Activity.Id}].path) /i $(args[{Constants.Parameters.InputDoc}].path)" };
        }

        /// <summary>
        /// Get activity parameters.
        /// </summary>
        private static Dictionary<string, Parameter> GetActivityParams()
        {
            return new Dictionary<string, Parameter>
                    {
                        {
                            Constants.Parameters.InputDoc,
                            new Parameter
                            {
                                Verb = Verb.Get,
                                Description = "Inventor file to process"
                            }
                        },
                        {
                            Constants.Parameters.OutputJson,
                            new Parameter
                            {
                                Verb = Verb.Put,
                                LocalName = "result",
                                Zip = true,
                                Description = "Resulting Json and pics",
                                Ondemand = false,
                                Required = false
                            }
                        }
                    };
        }

        /// <summary>
        /// Get arguments for workitem.
        /// </summary>
        private static Dictionary<string, IArgument> GetWorkItemArgs()
        {
            // TODO: update the URLs below with real values
            return new Dictionary<string, IArgument>
                    {
                        {
                            Constants.Parameters.InputDoc,
                            new XrefTreeArgument
                            {
                                Url = "!!! CHANGE ME !!!"
                            }
                        },
                        {
                            Constants.Parameters.OutputJson,
                            new XrefTreeArgument
                            {
                                Verb = Verb.Put,
                                Url = "!!! CHANGE ME !!!"
                            }
                        }
                    };
        }
    }
}
