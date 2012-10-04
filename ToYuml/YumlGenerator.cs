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

		List<Association> Associations = new List<Association>();
		List<string> Entries;

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
			Entries = new List<string>();

			foreach (var type in Types) {

				if (type.IsClass) {
					Entries.Add(string.Format("[{0}{1}]", Interfaces(type), type.Name));

					ExplicitInterfaces(type);
					DerivedClasses(type);
					AssosiatedClasses(type);
				}
				// [<<A>>]^-.-[B]
				else if (type.IsInterface && InterfaceInheritance) {
					Entries.Add(string.Format("[<<{0}>>]", type.Name));
				}
			}

			// process through all associations
			foreach (var assoc in Associations) {
				Entries.Add(assoc.ToString());
			}

			return string.Join(",", Entries);
		}

		// inline representation of an interface
		public string Interfaces(Type type)
		{
			if (InterfaceInheritance) return "";

			StringBuilder sb = new StringBuilder();
			foreach (var interfaceType in type.GetInterfaces()) {
				if (!Types.Contains(interfaceType)) continue;
				sb.Append(string.Format("<<{0}>>;", interfaceType.Name));
			}
			return sb.ToString();
		}

		// explicit interface inheritance
		private void ExplicitInterfaces(Type type)
		{
			if (!InterfaceInheritance) return;

			foreach (var interfaceType in type.GetInterfaces()) {
				if (!Types.Contains(interfaceType)) continue;
				Entries.Add(string.Format("[<<{0}>>]^-.-[{1}]", interfaceType.Name, type.Name));
			}
		}

		private void DerivedClasses(Type type)
		{
			// there's no need to climb the inheritance chain
			Type baseType = type.BaseType;
			if (baseType == null || !Types.Contains(baseType))
				return;

			Entries.Add(string.Format("[{0}{1}]^-[{2}{3}]", new object[] {
				Interfaces(baseType), baseType.Name,
				Interfaces(type), type.Name }
			));
		}

		private void AssosiatedClasses(Type type)
		{
			HashSet<Type> single = new HashSet<Type>();
			HashSet<Type> generic = new HashSet<Type>();

			BindingFlags binding = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.SetField
				| BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance;

			if (IncludeNonPublicFields) {
				binding |= BindingFlags.NonPublic;
			}

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
				if (fi.DeclaringType != type) continue;

				if (Types.Contains(fi.FieldType)) {
					single.Add(fi.FieldType);
				}
				else if (fi.FieldType.IsGenericType) {
					generic.Add(fi.FieldType);
				}
			}

			// process generics first. if they are enumerable, then output the 1-0..* notation, 
			// else add all type parameters to the "single" set
			foreach (Type t in generic) {
				var IsEnumerable = t.GetInterface(typeof(IEnumerable).FullName) != null;
				var typeParameters = t.GetGenericArguments();

				if (!IsEnumerable) {
					// add all type parameters to the single list
					foreach (Type typeParam in typeParameters) {
						if (Types.Contains(typeParam))
							single.Add(typeParam);
					}
				}
				else {
					// it's enumerable, and should be output as 1-0..*
					Type p0 = typeParameters[0];
					if (Types.Contains(p0)) { // enumerable on <T>
						AddAssociation(type, p0, true);
					}
				}
			}

			// anything else is a single element
			foreach (Type t in single) {
				AddAssociation(type, t, false);
			}
		}

		void AddAssociation(Type fromType, Type toType, bool isEnumerable)
		{
			// search for an existing FORWARD association
			Association fwd = Associations.Find(a => {
				return a.Type1 == fromType && a.Type2 == toType;
			});
			if (fwd != null) {
				// TODO: increment association
				// association already exists
				return;
			}

			// forward assoc doesn't exist, look for the reverse
			Association rev = Associations.Find(a => {
				return a.Type1 == toType && a.Type2 == fromType;
			});
			if (rev != null) {
				// reverse association already exists: [toType]->[fromType]
				// so update it with 
				rev.Increment(fromType, toType, isEnumerable);
				return;
			}

			// neither a forward nor a reverse association exists
			Associations.Add(new Association(this, fromType, toType, isEnumerable));
		}

		/*
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
		 * */
	}

	// This class wasn't used to store inheritance
	public class Association
	{
		YumlGenerator generator;

		public Type Type1 { get; private set; }
		public Type Type2 { get; private set; }
		public int Multiplicity1 { get; private set; }
		public int Multiplicity2 { get; private set; }
		public bool NavigableTo1 { get; private set; }
		public bool NavigableTo2 { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Object" /> class.
		/// </summary>
		public Association(YumlGenerator generator, Type type1, Type type2, bool type2IsEnumerable)
		{
			this.generator = generator;

			Type1 = type1;
			Type2 = type2;
			if (type2IsEnumerable) {
				Multiplicity1 = 1;
				Multiplicity2 = int.MaxValue;
			}
			else {
				Multiplicity1 = 1;
				Multiplicity2 = 1;
			}
			NavigableTo2 = true;
			NavigableTo1 = false;
		}

		public void Increment(Type fromType, Type toType, bool isType2Enumerable)
		{
			if (fromType == Type1 && toType == Type2) {
				// forward
				NavigableTo2 = true;
				if (Multiplicity2 < int.MaxValue) {
					if (isType2Enumerable) Multiplicity2 = int.MaxValue;
					else Multiplicity2 += 1;
				}
			}
			else if (fromType == Type2 && toType == Type1) {
				// reverse
				NavigableTo1 = true;
				if (Multiplicity1 < int.MaxValue) {
					if (isType2Enumerable) Multiplicity1 = int.MaxValue;
					else Multiplicity1 += 1;
				}
			}
			else {
				throw new InvalidOperationException("Invalid types specified to update");
			}
		}

		public override string ToString()
		{
			return string.Format("[{0}{1}]{2}{3}-{4}{5}[{6}{7}]",
				// LHS
				generator.Interfaces(Type1), 
				Type1.IsInterface ? "<<" + Type1.Name + ">>" : Type1.Name,
				NavigableTo1 ? "<" : string.Empty,
				Multiplicity1 == int.MaxValue ? "0..*" : string.Empty,
				// RHS
				Multiplicity2 == int.MaxValue ? "0..*" : string.Empty,
				NavigableTo2 ? ">" : string.Empty,
				generator.Interfaces(Type2), 
				Type2.IsInterface ? "<<" + Type2.Name + ">>" : Type2.Name
			);
		}
	}
}
