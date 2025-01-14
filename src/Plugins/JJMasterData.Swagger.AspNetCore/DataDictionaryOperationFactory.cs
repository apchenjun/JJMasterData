﻿using System.Globalization;
using System.Text;
using JJMasterData.Commons.Dao.Entity;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.DictionaryDAL;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using static JJMasterData.Swagger.AspNetCore.DataDictionarySchema;

namespace JJMasterData.Swagger.AspNetCore;

internal class DataDictionaryOperationFactory
{

    internal FormElement FormElement { get; set; }
    internal DicApiSettings Settings { get; set; }
    internal string ModelName => FormElement.Name.ToLower().Replace("tb_", string.Empty).Replace("vw_", string.Empty);

    internal DataDictionaryOperationFactory(FormElement formElement, DicApiSettings settings)
    {
        FormElement = formElement;
        Settings = settings;
    }

    internal OpenApiOperation Get()
    {

        var pkFields = FormElement.Fields.ToList().FindAll(x => x.IsPk);

        string nameFields = string.Empty;
        foreach (var field in pkFields)
        {
            if (nameFields.Length > 0)
                nameFields += ", ";

            nameFields += field.Name.ToLower();
        }

        var operation = new OpenApiOperation
        {
            Summary = "Get a specific record",
            Description = FormElement.Title + "<br><b>Accept-Encoding</b>: gzip, deflate ou utf8 (opcional)",
            OperationId = ModelName + "_Get",
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = FormElement.Name
                }
            },
            Responses = new OpenApiResponses
            {
                {
                    "200",
                    new OpenApiResponse
                    {
                        Description = "Success",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json", new OpenApiMediaType
                                {
                                    Encoding = new Dictionary<string, OpenApiEncoding>
                                    {
                                        {"utf-8", new OpenApiEncoding{ContentType = "application/json"} }
                                    },
                                    Schema = GetResponseSchema(ModelName)
                                }
                            }
                        }
                    }
                },
            },
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = Settings.GetFieldNameParsed("id"),
                    Description = "Primary Key Value.<br>" + nameFields,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                },
                GetAcceptLanguageParameter()
            }
        };

        operation.Responses.AddDefaultValues();

        return operation;
    }
    internal OpenApiOperation GetAll()
    {

        var operation = new OpenApiOperation
        {
            Summary = "Get all records",
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = FormElement.Name
                }
            },
            Responses = new OpenApiResponses
            {
                {
                    "200",
                    new OpenApiResponse
                    {
                        Description = "Success",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json", new OpenApiMediaType
                                {
                                    Schema = GetResponseSchema(ModelName)
                                }
                            }
                        }
                    }
                },
            },
            Description = FormElement.Title + "<br><b>Accept-Encoding</b>: gzip, deflate ou utf8 (opcional)",
            OperationId = ModelName + "_GetAll",
            Parameters = new List<OpenApiParameter>
        {
            new OpenApiParameter
            {
                Name = Settings.GetFieldNameParsed("pag"),
                Description = "Current page",
                In = ParameterLocation.Query,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Default =  new OpenApiInteger(1)
                }
            },
            new OpenApiParameter
            {
                Name = Settings.GetFieldNameParsed("regporpag"),
                Description = "Number of records per page",
                In = ParameterLocation.Query,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32",
                    Default =  new OpenApiInteger(5)
                }
            },
            new OpenApiParameter
            {
                Name = Settings.GetFieldNameParsed("orderby"),
                Description = "Order of records (default is pk ASC). Attention, this field is case sensitive.",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string"
                }
            },
            new OpenApiParameter
            {
                Name = Settings.GetFieldNameParsed("tot"),
                Description = "If you pass the total, the count of records will not be executed saving processing. (optional)",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "integer",
                    Format = "int32"
                }
            },
        }
        };


        var fields = FormElement.Fields.ToList().FindAll(x => !x.IsPk & x.Filter.Type != FilterMode.None);

        foreach (FormElementField field in fields)
        {
            string fieldName = Settings.GetFieldNameParsed(field.Name);
            string description = "Filter available. (" + field.Filter.Type.ToString().ToLower() + ")";
            if (!string.IsNullOrEmpty(field.Label))
                description += "<br>" + field.Label;

            if (!string.IsNullOrEmpty(field.HelpDescription))
                description += "<br>" + field.HelpDescription;

            var schema = GetFieldSchema(field);

            if (field.Filter.Type == FilterMode.Range)
            {

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = Settings.GetFieldNameParsed("_from"),
                    Description = description,
                    In = ParameterLocation.Query,
                    Required = field.IsRequired,
                    Schema = schema
                });

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = Settings.GetFieldNameParsed("_to"),
                    Description = description,
                    In = ParameterLocation.Query,
                    Required = field.IsRequired,
                    Schema = schema
                });
            }
            else
            {

                Dictionary<string, OpenApiMediaType>? content = null;

                var parameter = new OpenApiParameter
                {
                    Name = Settings.GetFieldNameParsed(fieldName),
                    Description = description ?? string.Empty,
                    In = ParameterLocation.Query,
                    Required = false
                };

                if (field.Component == FormComponent.ComboBox
                    && field.DataItem != null
                    && field.DataItem.Itens != null
                    && field.DataItem.Itens.Count > 0)
                {

                    var enums = (from DataItemValue dataItem in field.DataItem.Itens
                                 select new OpenApiString(dataItem.Id)).ToList<IOpenApiAny>();


                    var example = new StringBuilder();

                    content = new Dictionary<string, OpenApiMediaType>();

                    foreach (DataItemValue dataItem in field.DataItem.Itens)
                    {
                        example.Append("<br>" + dataItem.Id + " = " + dataItem.Description);
                    }

                    content.Add(field.FieldId.ToString(), new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = schema.Type,
                            Default = null,
                            Enum = enums,
                        }
                    });

                    description += example.ToString();

                    parameter.Content = content;
                }

                else
                {
                    parameter.Schema = schema;
                }

                operation.Parameters.Add(parameter);
            }
        }
        operation.Parameters.Add(GetAcceptLanguageParameter());

        operation.Responses.AddDefaultValues();

        return operation;
    }

    internal OpenApiOperation Post()
    {
        StringBuilder description = new();

        description.Append(FormElement.Title);
        description.Append("<br><br>Insert a list of records.");
        description.Append("<br>We do not use transactions in this scope, ");
        description.Append("if sending a list of records the return can be 201(all right) or 207(error in some record) ");
        description.Append("in this case you can check the status of each record in the response return.");

        var id = ModelName + "List";

        var items = GetDictionarySchema(FormElement, Settings, id, true);

        OpenApiSchema listSchema = new()
        {
            Title = ModelName + "List",
            Type = "array",
            Items = items,
            Description = "List of records"
        };

        var responseSchema = new OpenApiSchema
        {
            Title = ModelName + "Status",
            Type = "array",
            Items = GetValidationLetterSchema(true),
            Description = "List with status and validations"
        };


        var operation = new OpenApiOperation
        {
            Summary = "Add new records",
            Tags = new List<OpenApiTag>
            {
                    new OpenApiTag
                    {
                        Name = FormElement.Name
                    }
                },
            Responses = new OpenApiResponses
                {
                    {
                        "201",
                        new OpenApiResponse
                        {
                            Description = "Added",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                {
                                    "application/json", new OpenApiMediaType
                                    {
                                        Schema = responseSchema
                                    }
                                }
                            }
                        }
                    }
                },
            Description = description + "<br><b>Accept-Encoding</b>: gzip, deflate ou utf8 (opcional)",
            OperationId = ModelName + "_Post",
            RequestBody = new OpenApiRequestBody
            {

                Content =
                    {
                        {"application/json",
                            new OpenApiMediaType
                            {
                                Schema = listSchema
                            }
                        }
                    },
                Description = "Array with the objects.",
            },
            Parameters = new List<OpenApiParameter>
                {
                    new OpenApiParameter
                    {
                        Name = Settings.GetFieldNameParsed("replace"),
                        Description = "If record exists updates it, otherwise insert. (default false)",
                        In = ParameterLocation.Query,
                        Required = false,
                        Schema =  new OpenApiSchema
                        {
                            Type = "boolean"
                        }
                    }

                }
        };

        operation.Responses.AddDefaultValues();

        return operation;
    }
    internal OpenApiOperation Put()
    {
        StringBuilder description = new();
        description.Append(FormElement.Title);
        description.Append("<br><br>Update a list of records in batch.");
        description.Append("<br>We do not use transactions in this scope, ");
        description.Append("if sending a list of records the return can be 200(all right) or 207(error in some record) ");
        description.Append("in this case you can check the status of each record in the response return.");
        description.Append("<br><b>Accept-Encoding</b>: gzip, deflate ou utf8 (opcional)");

        var id = ModelName + "List";
        var items = GetDictionarySchema(FormElement, Settings, id, true);

        OpenApiSchema listSchema = new()
        {
            Title = id,
            Type = "array",
            Items = items,
            Description = "List of records"
        };

        OpenApiSchema responseSchema = new()
        {
            Title = ModelName + "Status",
            Type = "array",
            Items = GetValidationLetterSchema(true),
            Description = "List with status and validations"
        };

        var operation = new OpenApiOperation
        {
            Summary = "Update records",
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = FormElement.Name
                }
            },
            Responses = new OpenApiResponses
            {
                {
                    "200",
                    new OpenApiResponse
                    {
                        Description = "Success",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json", new OpenApiMediaType
                                {
                                    Schema = responseSchema
                                }
                            }
                        }
                    }
                }
            },
            Description = description.ToString(),
            OperationId = ModelName + "_Put",
            RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    {
                        "application/json",
                        new OpenApiMediaType
                        {
                            Schema = listSchema
                        }
                    }
                },
                Description = "Array with the objects<br>Primary key required",
            }
        };

        operation.Responses.AddDefaultValues();

        return operation;
    }
    internal OpenApiOperation Patch()
    {
        StringBuilder description = new();

        description.Append(FormElement.Title);
        description.Append("<br><br>Change some fields in a record.");
        description.Append("<br>We do not use transactions in this scope, ");
        description.Append("if sending a list of records the return can be 201(all right) or 207(error in some record) ");
        description.Append("in this case you can check the status of each record in the response return.");

        string id = ModelName + "List";

        OpenApiSchema items = GetDictionarySchema(FormElement, Settings, id, true);

        OpenApiSchema listSchema = new()
        {
            Title = id,
            Type = "array",
            Items = items,
            Description = "List of records"
        };

        OpenApiSchema responseSchema = new()
        {
            Title = ModelName + "Status",
            Type = "array",
            Items = GetValidationLetterSchema(true),
            Description = "List with status and validations"
        };


        var operation = new OpenApiOperation
        {
            Summary = "Update some especific fields",
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = FormElement.Name
                }
            },
            Responses = new OpenApiResponses
            {
                {
                    "200",
                    new OpenApiResponse
                    {
                        Description="Success",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "responseSchema", new OpenApiMediaType
                                {
                                    Schema = responseSchema
                                }
                            }
                        }
                    }
                }
            },
            Description = description + "<br><b>Accept-Encoding</b>: gzip, deflate ou utf8 (opcional)",
            OperationId = ModelName + "_Patch",
            RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    {
                        "application/json",
                        new OpenApiMediaType
                        {
                            Schema = listSchema
                        }
                    }
                },
                Description = "Array with the objects<br>Primary key required",
            }
        };

        operation.Responses.AddDefaultValues();

        return operation;
    }
    internal OpenApiOperation Delete()
    {
        var pkFields = FormElement.Fields.ToList().FindAll(x => x.IsPk);

        var description = new StringBuilder();
        description.Append(FormElement.Title);
        description.Append("<br>Remove a record.");
        if (pkFields.Count > 1)
            description.Append("Please enter the values ​​of the PKs separated by commas in the order of the object");
        else
            description.Append("Please enter the value of the primary key as a parameter.");


        string nameFields = string.Empty;
        foreach (var field in pkFields)
        {
            if (nameFields.Length > 0)
                nameFields += ", ";

            nameFields += field.Name;
        }

        var operation = new OpenApiOperation
        {
            Summary = "Delete a specific record",
            Description = description.ToString(),
            OperationId = ModelName + "_Del",
            Tags = new List<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = FormElement.Name
                }
            },
            Responses = new OpenApiResponses
            {
                {
                    "200",
                    new OpenApiResponse
                    {
                        Description = "Success",
                        Content = new Dictionary<string, OpenApiMediaType>
                        {
                            {
                                "application/json", new OpenApiMediaType
                                {
                                    Schema = GetResponseSchema(ModelName)
                                }
                            }
                        }
                    }
                },
            },
            Parameters = new List<OpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = Settings.GetFieldNameParsed("id"),
                    Description = "Primary Key Value.<br>" + nameFields,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                },
                GetAcceptLanguageParameter()
            }
        };

        operation.Responses.AddDefaultValues();

        return operation;
    }
    private static OpenApiParameter GetAcceptLanguageParameter()
    {
        return new OpenApiParameter
        {
            Name = "Accept-Language",
            Description = "Language Code",
            In = ParameterLocation.Query,
            Required = true,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString(CultureInfo.CurrentCulture.ToString())
            }
        };
    }
}
