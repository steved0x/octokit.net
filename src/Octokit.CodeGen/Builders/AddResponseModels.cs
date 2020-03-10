using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Octokit.CodeGen
{
    using TypeBuilderFunc = System.Func<PathMetadata, ApiClientFileMetadata, ApiClientFileMetadata>;

    public partial class Builders
    {

        public static readonly TypeBuilderFunc AddResponseModels = (metadata, data) =>
        {



            // first parameter of return type is the current model (needed for assigning to property)
            // second parameter is any additional models that were deserialized
            (ApiModelMetadata, List<ApiModelMetadata>) parseInnerModel(ObjectResponseProperty objectProperty, string classPrefix)
            {
                var additionalModels = new List<ApiModelMetadata>();
                var additionalName = GetPropertyName(objectProperty.Name, true);

                // TODO: what if we just skip the prefix here? how far can we
                // get without needing to worry about clashes and denormalizing
                // models?

                var classNamePrefix = $"{classPrefix}{additionalName}";
                var properties = new List<ApiModelProperty>();

                foreach (var property in objectProperty.Properties)
                {
                    property.Switch(primitiveProperty =>
                    {
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(primitiveProperty.Name),
                            Type = primitiveProperty.Type,
                        });
                    }, objectResponse =>
                    {
                        var (current, others) = parseInnerModel(objectResponse, classNamePrefix);
                        additionalModels.Add(current);
                        additionalModels.AddRange(others);
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(objectResponse.Name),
                            Type = current.Name,
                        });
                    }, primitiveList =>
                   {
                       properties.Add(new ApiModelProperty
                       {
                           Name = GetPropertyName(primitiveList.Name),
                           Type = "IReadOnlyList<string>",
                       });
                   }, objectList =>
                   {
                       throw new NotImplementedException($"AddResponseModels.parseInnerModel needs to process object lists for the property {objectList.Name} and type {objectList.Type}");
                   });
                }

                var top = new ApiModelMetadata
                {
                    Kind = "response",
                    Name = classNamePrefix,
                    Properties = properties,
                };

                return (top, additionalModels);
            }

            List<ApiModelMetadata> parseArrayResponseToModels(ArrayResponseContent arrayContent, HttpMethod method, string statusCode)
            {
                var models = new List<ApiModelMetadata>();

                var classNamePrefix = GetClassName(metadata);
                var properties = new List<ApiModelProperty>();

                foreach (var property in arrayContent.ItemProperties)
                {
                    property.Switch(primitiveProperty =>
                    {
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(primitiveProperty.Name),
                            Type = primitiveProperty.Type,
                        });
                    }, objectProperty =>
                    {
                        var (current, others) = parseInnerModel(objectProperty, "");
                        models.Add(current);
                        models.AddRange(others);
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(objectProperty.Name),
                            Type = current.Name,
                        });
                    }, primitiveList =>
                    {
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(primitiveList.Name),
                            Type = "IReadOnlyList<string>",
                        });
                    }, objectList =>
                    {
                        throw new NotImplementedException($"AddResponseModels.parseArrayResponseToModels needs to process object lists for the property {objectList.Name} and type {objectList.Type}");
                    });
                }

                var top = new ApiModelMetadata
                {
                    Kind = "response",
                    Name = classNamePrefix,
                    Properties = properties,
                    Method = method,
                    StatusCode = statusCode,
                };

                models.Add(top);

                return models;
            }

            List<ApiModelMetadata> parseObjectResponseToModels(ObjectResponseContent objectContent, HttpMethod method, string statusCode)
            {
                var models = new List<ApiModelMetadata>();

                var classNamePrefix = GetClassName(metadata);
                var properties = new List<ApiModelProperty>();

                foreach (var property in objectContent.Properties)
                {
                    property.Switch(primitiveProperty =>
                    {
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(primitiveProperty.Name),
                            Type = primitiveProperty.Type,
                        });
                    }, objectProperty =>
                    {
                        var (current, others) = parseInnerModel(objectProperty, "");
                        models.Add(current);
                        models.AddRange(others);
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(objectProperty.Name),
                            Type = current.Name,
                        });
                    }, primitiveList =>
                    {
                        properties.Add(new ApiModelProperty
                        {
                            Name = GetPropertyName(primitiveList.Name),
                            Type = "IReadOnlyList<string>",
                        });
                    }, objectList =>
                    {
                        throw new NotImplementedException($"AddResponseModels.parseObjectResponseToModels needs to process object lists for the property {objectList.Name} and type {objectList.Type}");
                    });
                }

                var top = new ApiModelMetadata
                {
                    Kind = "response",
                    Name = classNamePrefix,
                    Properties = properties,
                    Method = method,
                    StatusCode = statusCode,
                };

                models.Add(top);

                return models;
            }

            foreach (var verb in metadata.Verbs)
            {
                foreach (var response in verb.Responses)
                {
                    if (response.ContentType == "application/json")
                    {
                        response.Content.Switch(objectResponse =>
                        {
                            data.Models.AddRange(parseObjectResponseToModels(objectResponse, verb.Method, response.StatusCode));
                        },
                        arrayResponse =>
                        {
                            data.Models.AddRange(parseArrayResponseToModels(arrayResponse, verb.Method, response.StatusCode));
                        });
                    }
                }
            }

            data.Models = data.Models.Distinct(ApiModelCompararer.Default).ToList();

            return data;
        };

    }
}
