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
        HashSet<Type> Types;
        List<Relationship> Relationships = new List<Relationship>();
		
		// Settings
		bool InterfaceInheritance = false; // off by default
		bool IncludeNonPublicFields = false; // search only public fields by default

        public YumlGenerator(IList<Type> Types)
        {
            this.Types = new HashSet<Type>(Types); // allows for empty lists
        }

        public YumlGenerator()
        {
            this.Types = new HashSet<Type>();
        }

        public YumlGenerator AddType(Type t)
        {
            Types.Add(t);
            return this;
        }

        public YumlGenerator AddTypes(IEnumerable<Type> types)
        {
            foreach (Type t in types)
                Types.Add(t);
            return this;
        }

        // add all types in this assembly that pass the filter
        // filter can be null
        public YumlGenerator AddTypesForAssembly(Assembly assembly, Func<Type, bool> filter = null)
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

		// when false, will use [<<Interface>>Type]
		// when true, will use [<<Interface>>]^-.-[Type]
		public YumlGenerator UseInterfaceInheritance(bool interfaceInheritance)
		{
			this.InterfaceInheritance = interfaceInheritance;
			return this;
		}

		public YumlGenerator SearchNonPublicMembers(bool includeNonPublic)
		{
			IncludeNonPublicFields = includeNonPublic;
			return this;
		}

        // Generate the YUML string
        public string Yuml()
        {
			int count = 0;
            var sb = new StringBuilder();
            foreach (var type in Types) {

                if (type.IsClass) {
					if (count > 0) sb.Append(",");
                    
                    sb.AppendFormat("[{0}{1}]", Interfaces(type), type.Name);
					sb.Append(ExplicitInterfaces(type));
                    sb.Append(DerivedClasses(type));
                    sb.Append(AssosiatedClasses(type));

					++count;
                }
				// [<<A>>]^-.-[B]
				else if (type.IsInterface && InterfaceInheritance) {
					if (count > 0) sb.Append(",");
					
					sb.AppendFormat("[<<{0}>>]", type.Name);

					++count;
				}
            }
            return sb.ToString();
        }

		// inline representation of an interface
        private string Interfaces(Type type)
        {
			if (InterfaceInheritance) return String.Empty;

            var sb = new StringBuilder();
            foreach (var interfaceType in type.GetInterfaces()) {
                if (!Types.Contains(interfaceType)) continue;
                sb.AppendFormat("<<{0}>>;", interfaceType.Name);
            }
            return sb.ToString();
        }

		// explicit interface inheritance
		private string ExplicitInterfaces(Type type)
		{
			if (!InterfaceInheritance) return String.Empty;
			var sb = new StringBuilder();
			foreach (var interfaceType in type.GetInterfaces()) {
				if (!Types.Contains(interfaceType)) continue;
				sb.AppendFormat(",[<<{0}>>]^-.-[{1}]", interfaceType.Name, type.Name);
			}
			return sb.ToString();
		}

        private string DerivedClasses(Type type)
        {
            var prevType = type;
            var sb = new StringBuilder();

            while (type.BaseType != null) {
                type = type.BaseType;
                if (Types.Contains(type)) {
                    var relationship = new Relationship(type, prevType, RelationshipType.Inherits);

                    if (!Relationships.Exists(r => (r.Type1 == relationship.Type1 && r.Type2 == relationship.Type2 && r.RelationshipType == relationship.RelationshipType))) {
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
			HashSet<Type> single = new HashSet<Type>();
			HashSet<Type> generic = new HashSet<Type>();

			BindingFlags binding = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.SetField
				| BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance;

			if (IncludeNonPublicFields) {
				binding |= BindingFlags.NonPublic;
			}

            var sb = new StringBuilder();

			// search properties
            foreach (var property in type.GetProperties(binding)) {
				
				// only process properties in the declaring type
				if (property.DeclaringType != type) continue;

                if (Types.Contains(property.PropertyType)) {
					single.Add(property.PropertyType);
				}
                else if (property.PropertyType.IsGenericType) {
					generic.Add(property.PropertyType);
				}
			}

			// search fields
			foreach (FieldInfo fi in type.GetFields(binding)) {
				if(fi.DeclaringType != type) continue;

				if(Types.Contains(fi.FieldType)) {
					single.Add(fi.FieldType);
				}
				else if (fi.FieldType.IsGenericType) {
					generic.Add(fi.FieldType);
				}
			}

			// process generics first. if they are enumerable, then output the 1-0..* notation, 
			// else add all type parameters to the "single" set
			foreach(Type t in generic) {
                var IsEnumerable = t.GetInterface(typeof(IEnumerable).FullName) != null;
                var typeParameters = t.GetGenericArguments();

				if(!IsEnumerable) {
					// add all type parameters to the single list
					foreach(Type typeParam in typeParameters) {
						if(Types.Contains(typeParam))
							single.Add(typeParam);
					}
				}
				else {
					// it's enumerable, and should be output as 1-0..*
					Type p0 = typeParameters[0];
					if (Types.Contains(p0)) { // enumerable on <T>
						sb.AppendFormat(",[{0}{1}]1-0..*[{2}{3}]", 
							Interfaces(type), type.Name,
							Interfaces(p0), p0.Name );
					}
				}
			}

			// anything else is a single element
			foreach(Type t in single) {
				sb.AppendFormat(",[{0}{1}]->[{2}{3}]", 
					Interfaces(type), type.Name,
					Interfaces(t), t.IsInterface ? "<<" + t.Name + ">>" : t.Name);
			}

            return sb.ToString();
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != typeof(object)) {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur) {
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
