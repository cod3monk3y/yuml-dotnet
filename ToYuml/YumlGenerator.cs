using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ToYuml
{
	// Generates YUML from a list of types. Does NOT create the URI. 
	// The URI is now composed as an HTTP Post via YumlRequest.
    public class YumlGenerator
    {
		List<Type> Types;
        List<Relationship> Relationships = new List<Relationship>();

        public YumlGenerator(IList<Type> Types)
        {
			this.Types = new List<Type>(Types); // allows for empty lists
        }

		public YumlGenerator()
		{
			this.Types = new List<Type>();
		}

		public YumlGenerator AddType(Type t)
		{
			Types.Add(t);
			return this;
		}

		public YumlGenerator AddTypes(IEnumerable<Type> types)
		{
			Types.AddRange(types);
			return this;
		}

		// add all types in this assembly that pass the filter
		// filter can be null
		public YumlGenerator AddTypesForAssembly(Assembly assembly, Func<Type,bool> filter=null)
		{
			foreach (Type t in assembly.GetTypes()) {
				if (filter == null || filter(t))
					Types.Add(t);
			}
			return this;
		}

		// add all types for the specified type
		public YumlGenerator AddTypesForAssembly(Type type, Func<Type, bool> filter = null)
		{
			return AddTypesForAssembly(type.Assembly, filter);
		}

		// Generate the YUML string
        public string Yuml()
        {
			bool FirstPass = true;
			var sb = new StringBuilder();
            foreach (var type in Types)
            {
                if (!Types.Contains(type)) continue;
                if (type.IsClass)
                {
                    if (!FirstPass) sb.Append(",");
                    sb.AppendFormat("[{0}{1}]", Interfaces(type), type.Name);
                    sb.Append(DerivedClasses(type));
                    sb.Append(AssosiatedClasses(type));
                }
				FirstPass = false;
            }
            return sb.ToString();
        }

        private string Interfaces(Type type)
        {
            var sb = new StringBuilder();
            foreach (var interfaceType in type.GetInterfaces())
            {
                if (!Types.Contains(interfaceType)) continue;
                sb.AppendFormat("<<{0}>>;", interfaceType.Name);
            }
            return sb.ToString();
        }

        private string DerivedClasses(Type type)
        {
            var prevType = type;
            var sb = new StringBuilder();

            while (type.BaseType != null)
            {
                type = type.BaseType;
                if (Types.Contains(type))
                {
                    var relationship = new Relationship(type, prevType, RelationshipType.Inherits);

                    if (!Relationships.Exists(r => (r.Type1 == relationship.Type1 && r.Type2 == relationship.Type2 && r.RelationshipType == relationship.RelationshipType)))
                    {
                        sb.AppendFormat(",[{0}{1}]^-[{2}{3}]", Interfaces(type), type.Name, Interfaces(prevType), prevType.Name);
                        Relationships.Add(relationship);
                    }
                }
                prevType = type;
            }
            return sb.ToString();
        }

        private string AssosiatedClasses(Type type)
        {
            var sb = new StringBuilder();
            foreach (var property in type.GetProperties())
            {

                if (Types.Contains(property.PropertyType))
                {
                    sb.AppendFormat(",[{0}{1}]->[{2}{3}]", Interfaces(type), type.Name,
                                    Interfaces(property.PropertyType), property.PropertyType.Name);


                }
                else if (property.PropertyType.IsGenericType)
                {
                    var IsEnumerable = property.PropertyType.GetInterface(typeof(IEnumerable).FullName) != null;
                    var typeParameters = property.PropertyType.GetGenericArguments();

                    if (Types.Contains(typeParameters[0]) && IsEnumerable){
                        sb.AppendFormat(",[{0}{1}]1-0..*[{2}{3}]", Interfaces(type), type.Name,
                        Interfaces(typeParameters[0]), typeParameters[0].Name);
                    }
                }

                //if (type != property.PropertyType)
                //    AssosiatedClasses(property.PropertyType);

            }
            return sb.ToString();
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

    }

    public class Relationship
    {
        public Type Type1 { get; set; }
        public Type Type2 { get; set; }
        public RelationshipType RelationshipType { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object" /> class.
        /// </summary>
        public Relationship(Type type1, Type type2, RelationshipType relationshipType)
        {
            Type1 = type1;
            Type2 = type2;
            RelationshipType = relationshipType;
        }
    }

    public enum RelationshipType
    {
        Inherits = 1,
        HasOne = 2
    }
}
