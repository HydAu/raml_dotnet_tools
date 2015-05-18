﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Raml.Common;
using Raml.Parser.Expressions;
using Raml.Tools.WebApiGenerator;

namespace Raml.Tools
{
	public class WebApiMethodsGenerator : MethodsGeneratorBase
	{
		public WebApiMethodsGenerator(RamlDocument raml) : base(raml)
		{
		}

		public IEnumerable<ControllerMethod> GetMethods(Resource resource, string url, ControllerObject parent, string objectName, IDictionary<string, ApiObject> schemaResponseObjects, IDictionary<string, ApiObject> schemaRequestObjects)
		{
			var methodsNames = new List<string>();
			if (parent != null && parent.Methods != null)
				methodsNames = parent.Methods.Select(m => m.Name).ToList();

			var generatorMethods = new Collection<ControllerMethod>();
			if (resource.Methods == null)
				return generatorMethods;

			foreach (var method in resource.Methods)
			{
				var generatedMethod = BuildControllerMethod(url, method, resource, parent, schemaResponseObjects, schemaRequestObjects);

				if (IsVerbForMethod(method))
				{
					if (methodsNames.Contains(generatedMethod.Name))
						generatedMethod.Name = GetUniqueName(methodsNames, generatedMethod.Name, resource.RelativeUri);

					if (method.QueryParameters != null && method.QueryParameters.Any())
					{
						var queryParameters = QueryParametersParser.ParseParameters(method);
						generatedMethod.QueryParameters = queryParameters;
					}

					generatorMethods.Add(generatedMethod);
					methodsNames.Add(generatedMethod.Name);
				}
			}

			return generatorMethods;
		}

		private ControllerMethod BuildControllerMethod(string url, Method method, Resource resource, ControllerObject parent, IDictionary<string, ApiObject> schemaResponseObjects, IDictionary<string, ApiObject> schemaRequestObjects)
		{
			var relativeUri = UrlGeneratorHelper.GetRelativeUri(url, parent.PrefixUri);

			return new ControllerMethod
			{
				Name = NetNamingMapper.GetMethodName(method.Verb ?? "Get" + resource.RelativeUri),
				Parameter = GetParameter(GeneratorServiceHelper.GetKeyForResource(method, resource), method, resource, schemaRequestObjects, url),
				UriParameters = uriParametersGenerator.GetUriParameters(resource, url),
				ReturnType = GetReturnType(GeneratorServiceHelper.GetKeyForResource(method, resource), method, resource, schemaResponseObjects, url),
				Comment = GetComment(resource, method),
				Url = relativeUri,
				Verb = NetNamingMapper.Capitalize(method.Verb),
				Parent = null,
				UseSecurity =
					raml.SecuredBy != null && raml.SecuredBy.Any() ||
					resource.Methods.Any(m => m.Verb == method.Verb && m.SecuredBy != null && m.SecuredBy.Any()),
				SecurityParameters = GetSecurityParameters(raml, method)
			};
		}

		private IEnumerable<Property> GetSecurityParameters(RamlDocument ramlDocument, Method method)
		{
			var securityParams = new Collection<Property>();
			if (ramlDocument.SecuritySchemes == null || !ramlDocument.SecuritySchemes.Any())
				return securityParams;

			if ((ramlDocument.SecuredBy == null || !ramlDocument.SecuredBy.Any())
				&& (method.SecuredBy == null || !method.SecuredBy.Any()))
				return securityParams;

			var securedBy = method.SecuredBy != null && method.SecuredBy.Any() ? method.SecuredBy : ramlDocument.SecuredBy;

			if (securedBy == null)
				return securityParams;

			var secured = securedBy.First();

			var dic = ramlDocument.SecuritySchemes.FirstOrDefault(s => s.ContainsKey(secured));
			if (dic == null)
				return securityParams;

			var descriptor = ramlDocument.SecuritySchemes.First(s => s.ContainsKey(secured))[secured].DescribedBy;
			if (descriptor == null || descriptor.QueryParameters == null || !descriptor.QueryParameters.Any())
				return securityParams;

			return QueryParametersParser.ConvertParametersToProperties(descriptor.QueryParameters);
		}
	}
}