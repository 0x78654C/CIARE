// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	public class ReflectionClass : DefaultClass
	{
		const BindingFlags flags = BindingFlags.Instance  |
			BindingFlags.Static    |
			BindingFlags.NonPublic |
			BindingFlags.DeclaredOnly |
			BindingFlags.Public;
		
		readonly Type reflectedType;
		readonly Lazy<ReflectedMembers> members;
		readonly Lazy<bool> hasExtensionMethods;

		sealed class ReflectedMembers
		{
			public IList<IField> Fields;
			public IList<IProperty> Properties;
			public IList<IMethod> Methods;
			public IList<IEvent> Events;
		}

		public override IList<IField> Fields => members.Value.Fields;
		public override IList<IProperty> Properties => members.Value.Properties;
		public override IList<IMethod> Methods => members.Value.Methods;
		public override IList<IEvent> Events => members.Value.Events;
		public override bool HasExtensionMethods => hasExtensionMethods.Value;

		bool FindExtensionMethods()
		{
			if (members.IsValueCreated)
				return members.Value.Methods.Any(method => method.IsExtensionMethod);
			// Extension lookup must not create every member of every imported type.
			foreach (MethodInfo method in reflectedType.GetMethods(flags & ~BindingFlags.Instance)) {
				if (!method.IsStatic || method.IsSpecialName ||
				    !(method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly)) continue;
				foreach (CustomAttributeData attribute in method.CustomAttributes) {
					string name = attribute.AttributeType.FullName;
					if (name == "System.Runtime.CompilerServices.ExtensionAttribute" || name == "Boo.Lang.ExtensionAttribute")
						return true;
				}
			}
			return false;
		}

		void InitNestedTypes(Type type)
		{
			foreach (Type nestedType in type.GetNestedTypes(flags)) {
				// We cannot use nestedType.IsVisible - that only checks for public types,
				// but we also need to load protected types.
				if (nestedType.IsNestedPublic || nestedType.IsNestedFamily || nestedType.IsNestedFamORAssem) {
					string name = this.FullyQualifiedName + "." + nestedType.Name;
					InnerClasses.Add(new ReflectionClass(CompilationUnit, nestedType, name, this));
				}
			}
		}

		ReflectedMembers InitMembers()
		{
			Type type = reflectedType;
			var fields = new List<IField>();
			var properties = new List<IProperty>();
			var methods = new List<IMethod>();
			var events = new List<IEvent>();
			foreach (FieldInfo field in type.GetFields(flags)) {
				if (!field.IsPublic && !field.IsFamily && !field.IsFamilyOrAssembly) continue;
				if (!field.IsSpecialName) {
					fields.Add(new ReflectionField(field, this));
				}
			}
			
			foreach (PropertyInfo propertyInfo in type.GetProperties(flags)) {
				ReflectionProperty prop = new ReflectionProperty(propertyInfo, this);
				if (prop.IsPublic || prop.IsProtected)
					properties.Add(prop);
			}
			
			foreach (ConstructorInfo constructorInfo in type.GetConstructors(flags)) {
				if (!constructorInfo.IsPublic && !constructorInfo.IsFamily && !constructorInfo.IsFamilyOrAssembly) continue;
				methods.Add(new ReflectionMethod(constructorInfo, this));
			}
			
			foreach (MethodInfo methodInfo in type.GetMethods(flags)) {
				if (!methodInfo.IsPublic && !methodInfo.IsFamily && !methodInfo.IsFamilyOrAssembly) continue;
				if (!methodInfo.IsSpecialName) {
					methods.Add(new ReflectionMethod(methodInfo, this));
				}
			}
			
			foreach (EventInfo eventInfo in type.GetEvents(flags)) {
				events.Add(new ReflectionEvent(eventInfo, this));
			}
			// Publish fully frozen lists once; readers never see partial initialization.
			return new ReflectedMembers {
				Fields = FreezeList(fields), Properties = FreezeList(properties),
				Methods = FreezeList(methods), Events = FreezeList(events)
			};
		}
		
		static bool IsDelegate(Type type)
		{
			return type.IsSubclassOf(typeof(Delegate)) && type != typeof(MulticastDelegate);
		}
		
		internal static void AddAttributes(IProjectContent pc, IList<IAttribute> list, IList<CustomAttributeData> attributes)
		{
			foreach (CustomAttributeData att in attributes) {
				DefaultAttribute a = new DefaultAttribute(ReflectionReturnType.Create(pc, null, att.Constructor.DeclaringType, false));
				foreach (CustomAttributeTypedArgument arg in att.ConstructorArguments) {
					a.PositionalArguments.Add(ReplaceTypeByIReturnType(pc, arg.Value));
				}
				foreach (CustomAttributeNamedArgument arg in att.NamedArguments) {
					a.NamedArguments.Add(arg.MemberInfo.Name, ReplaceTypeByIReturnType(pc, arg.TypedValue.Value));
				}
				list.Add(a);
			}
		}
		
		static object ReplaceTypeByIReturnType(IProjectContent pc, object val)
		{
			if (val is Type) {
				return ReflectionReturnType.Create(pc, null, (Type)val, false, false);
			} else {
				return val;
			}
		}
		
		internal static void ApplySpecialsFromAttributes(DefaultClass c)
		{
			foreach (IAttribute att in c.Attributes) {
				if (att.AttributeType.FullyQualifiedName == "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute"
				    || att.AttributeType.FullyQualifiedName == "System.Runtime.CompilerServices.CompilerGlobalScopeAttribute")
				{
					c.ClassType = ClassType.Module;
					break;
				}
			}
		}
		
		public static string SplitTypeParameterCountFromReflectionName(string reflectionName)
		{
			int lastBackTick = reflectionName.LastIndexOf('`');
			if (lastBackTick < 0)
				return reflectionName;
			else
				return reflectionName.Substring(0, lastBackTick);
		}
		
		public static string SplitTypeParameterCountFromReflectionName(string reflectionName, out int typeParameterCount)
		{
			int pos = reflectionName.LastIndexOf('`');
			if (pos < 0) {
				typeParameterCount = 0;
				return reflectionName;
			} else {
				string typeCount = reflectionName.Substring(pos + 1);
				if (int.TryParse(typeCount, out typeParameterCount))
					return reflectionName.Substring(0, pos);
				else
					return reflectionName;
			}
		}
		
		public static string ConvertReflectionNameToFullName(string reflectionName, out int typeParameterCount)
		{
			if (reflectionName.IndexOf('+') > 0) {
				typeParameterCount = 0;
				StringBuilder newName = new StringBuilder();
				foreach (string namepart in reflectionName.Split('+')) {
					if (newName.Length > 0)
						newName.Append('.');
					int partTypeParameterCount;
					newName.Append(SplitTypeParameterCountFromReflectionName(namepart, out partTypeParameterCount));
					typeParameterCount += partTypeParameterCount;
				}
				return newName.ToString();
			} else {
				return SplitTypeParameterCountFromReflectionName(reflectionName, out typeParameterCount);
			}
		}
		
		public ReflectionClass(ICompilationUnit compilationUnit, Type type, string fullName, IClass declaringType) : base(compilationUnit, declaringType)
		{
			reflectedType = type;
			members = new Lazy<ReflectedMembers>(InitMembers, true);
			hasExtensionMethods = new Lazy<bool>(FindExtensionMethods, true);
			FullyQualifiedName = SplitTypeParameterCountFromReflectionName(fullName);
			
			try {
				AddAttributes(compilationUnit.ProjectContent, this.Attributes, CustomAttributeData.GetCustomAttributes(type));
			} catch (Exception ex) {
				HostCallback.ShowError("Error reading custom attributes", ex);
			}
			
			// set classtype
			if (type.IsInterface) {
				this.ClassType = ClassType.Interface;
			} else if (type.IsEnum) {
				this.ClassType = ClassType.Enum;
			} else if (type.IsValueType) {
				this.ClassType = ClassType.Struct;
			} else if (IsDelegate(type)) {
				this.ClassType = ClassType.Delegate;
			} else {
				this.ClassType = ClassType.Class;
				ApplySpecialsFromAttributes(this);
			}
			if (type.IsGenericTypeDefinition) {
				foreach (Type g in type.GetGenericArguments()) {
					this.TypeParameters.Add(new DefaultTypeParameter(this, g));
				}
				int i = 0;
				foreach (Type g in type.GetGenericArguments()) {
					AddConstraintsFromType(this.TypeParameters[i++], g);
				}
			}
			
			ModifierEnum modifiers  = ModifierEnum.None;
			
			if (type.IsNestedAssembly) {
				modifiers |= ModifierEnum.Internal;
			}
			if (type.IsSealed) {
				modifiers |= ModifierEnum.Sealed;
			}
			if (type.IsAbstract) {
				modifiers |= ModifierEnum.Abstract;
			}
			if (type.IsSealed && type.IsAbstract) {
				modifiers |= ModifierEnum.Static;
			}
			
			if (type.IsNestedPrivate ) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (type.IsNestedFamily ) {
				modifiers |= ModifierEnum.Protected;
			} else if (type.IsNestedPublic || type.IsPublic) {
				modifiers |= ModifierEnum.Public;
			} else if (type.IsNotPublic) {
				modifiers |= ModifierEnum.Internal;
			} else if (type.IsNestedFamORAssem || type.IsNestedFamANDAssem) {
				modifiers |= ModifierEnum.Protected;
				modifiers |= ModifierEnum.Internal;
			}
			this.Modifiers = modifiers;
			
			// set base classes
			if (type.BaseType != null) { // it's null for System.Object ONLY !!!
				BaseTypes.Add(ReflectionReturnType.Create(this, type.BaseType, false));
			}
			
			foreach (Type iface in type.GetInterfaces()) {
				BaseTypes.Add(ReflectionReturnType.Create(this, iface, false));
			}
			
			this.AddDefaultConstructorIfRequired = (this.ClassType == ClassType.Struct || this.ClassType == ClassType.Enum);
			InitNestedTypes(type);
		}
		
		internal static void AddConstraintsFromType(ITypeParameter tp, Type type)
		{
			foreach (Type constraint in type.GetGenericParameterConstraints()) {
				if (tp.Method != null) {
					tp.Constraints.Add(ReflectionReturnType.Create(tp.Method, constraint, false));
				} else {
					tp.Constraints.Add(ReflectionReturnType.Create(tp.Class, constraint, false));
				}
			}
		}
		
		protected override bool KeepInheritanceTree {
			get { return true; }
		}
	}
}
