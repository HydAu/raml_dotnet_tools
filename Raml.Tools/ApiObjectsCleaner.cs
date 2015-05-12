﻿using System;
using System.Collections.Generic;
using System.Linq;
using Raml.Tools.ClientGenerator;
using Raml.Tools.WebApiGenerator;

namespace Raml.Tools
{
	public class ApiObjectsCleaner
	{
		private readonly IDictionary<string, ApiObject> schemaRequestObjects;
		private readonly IDictionary<string, ApiObject> schemaResponseObjects;

		public ApiObjectsCleaner(IDictionary<string, ApiObject> schemaRequestObjects, IDictionary<string, ApiObject> schemaResponseObjects)
		{
			this.schemaRequestObjects = schemaRequestObjects;
			this.schemaResponseObjects = schemaResponseObjects;
		}

		public void CleanObjects(IEnumerable<ControllerObject> controllers, IDictionary<string, ApiObject> objects, Func<IEnumerable<ControllerObject>, ApiObject, bool> checkAction)
		{
			var keys = objects.Keys.ToArray();
			foreach (var key in keys)
			{
				var apiObject = objects[key];
				if (checkAction(controllers, apiObject))
					continue;

				if (IsUsedAsReferenceInAnyObject(apiObject))
					continue;

				objects.Remove(key);
			}
		}

		public void CleanObjects(IEnumerable<ClassObject> classes, IDictionary<string, ApiObject> objects, Func<IEnumerable<ClassObject>, ApiObject, bool> checkAction)
		{
			var keys = objects.Keys.ToArray();
			foreach (var key in keys)
			{
				var apiObject = objects[key];
				if (checkAction(classes, apiObject))
					continue;

                if (IsUsedAsReferenceInAnyObject(apiObject))
					continue;

				objects.Remove(key);
			}
		}

		public bool IsUsedAsResponseInAnyMethod(IEnumerable<ControllerObject> controllers, ApiObject requestObj)
		{
			return controllers.Any(c => c.Methods.Any(m => m.ReturnType == requestObj.Name ||   m.ReturnType == CollectionTypeHelper.GetCollectionType(requestObj.Name)));
		}

		public bool IsUsedAsParameterInAnyMethod(IEnumerable<ControllerObject> controllers, ApiObject requestObj)
		{
			return controllers.Any(c => c.Methods
				.Any(m => m.Parameter != null
                          && (m.Parameter.Type == requestObj.Name || m.Parameter.Type == CollectionTypeHelper.GetCollectionType(requestObj.Name))));
		}

        public bool IsUsedAsParameterOrResponseInAnyMethod(IEnumerable<ControllerObject> controllers, ApiObject obj)
        {
            return IsUsedAsParameterInAnyMethod(controllers, obj) || IsUsedAsResponseInAnyMethod(controllers, obj);
        }

        public bool IsUsedAsParameterOrResponseInAnyMethod(IEnumerable<ClassObject> classes, ApiObject obj)
        {
            return IsUsedAsParameterInAnyMethod(classes, obj) || IsUsedAsResponseInAnyMethod(classes, obj);
        }

		public bool IsUsedAsResponseInAnyMethod(IEnumerable<ClassObject> classes, ApiObject requestObj)
		{
            return classes.Any(c => c.Methods.Any(m => m.ReturnType == requestObj.Name || m.ReturnType == CollectionTypeHelper.GetCollectionType(requestObj.Name)));
		}

		public bool IsUsedAsParameterInAnyMethod(IEnumerable<ClassObject> classes, ApiObject requestObj)
		{
			return classes.Any(c => c.Methods
				.Any(m => m.Parameter != null
                          && (m.Parameter.Type == requestObj.Name || m.Parameter.Type == CollectionTypeHelper.GetCollectionType(requestObj.Name))));
		}
        
		private bool IsUsedAsReferenceInAnyObject(ApiObject requestObj)
		{
           	return schemaRequestObjects.SelectMany(o => o.Value.Properties).Any(x => x.Type == requestObj.Name || 
                        x.Type == CollectionTypeHelper.GetCollectionType(requestObj.Name) || 
                        x.Type == requestObj.BaseClass || 
                        x.Type == CollectionTypeHelper.GetCollectionType(requestObj.BaseClass))
				   || schemaResponseObjects.SelectMany(o => o.Value.Properties).Any(x => x.Type == requestObj.Name || 
                        x.Type == CollectionTypeHelper.GetCollectionType(requestObj.Name) ||
                        x.Type == requestObj.BaseClass ||
                        x.Type == CollectionTypeHelper.GetCollectionType(requestObj.BaseClass));
		}

	}
}