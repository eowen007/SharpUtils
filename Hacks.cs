﻿/* Date: 6.1.2015, Time: 20:42 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using IllidanS4.SharpUtils.Reflection;
using IllidanS4.SharpUtils.Reflection.Emit;

namespace IllidanS4.SharpUtils
{
	/// <summary>
	/// Used to obtain invokable delegates from private members.
	/// </summary>
	public sealed class Hacks : HacksBase<MulticastDelegate>
	{
		private Hacks(){}
	}
	
	/// <summary>
	/// The base class for <see cref="Hacks"/> to limit the argument to <see cref="System.MulticastDelegate"/>.
	/// Do not use this class directly.
	/// </summary>
	public abstract class HacksBase<TDelegateBase> where TDelegateBase : class, ICloneable, ISerializable
	{
		internal HacksBase(){}
		
		const BindingFlags privflags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
		
		/// <summary>
		/// Creates an invokable delegate from a method in a specific type, found by its name and index.
		/// </summary>
		public static TDelegate GetInvoker<TDelegate>(Type type, string method, bool instance, int ord=0) where TDelegate : TDelegateBase
		{
			BindingFlags flags = privflags;
			flags &= instance ? ~BindingFlags.Static : ~BindingFlags.Instance;
			MethodInfo mi;
			try{
				mi = type.GetMethod(method, flags);
			}catch(AmbiguousMatchException)
			{
				var tParams = typeof(TDelegate).GetDelegateSignature().ParameterTypes;
				if(instance == true)
				{
					tParams = tParams.Skip(1).ToArray();
				}
				mi = type.GetMethod(method, flags, null, 0, tParams, null);
				if(mi == null) mi = type.GetMethods(flags).Where(m => m.Name == method).ElementAt(ord);
			}
			return GetInvoker<TDelegate>(mi);
		}
		
		/// <summary>
		/// Creates an invokable delegate from a specific method.
		/// </summary>
		public static TDelegate GetInvoker<TDelegate>(MethodInfo mi) where TDelegate : TDelegateBase
		{
			Type delType = typeof(TDelegate);
			MethodSignature delSig = delType.GetDelegateSignature();
			Type[] tParams = delSig.ParameterTypes;
			Type retType = delSig.ReturnType;
			
			var mParams = mi.GetParameters().Select(p => p.ParameterType).ToList();
			
			try{
				return (TDelegate)(object)mi.CreateDelegate(delType);
			}catch(ArgumentException)
			{
				
			}
			
			DynamicMethod dyn = new DynamicMethod(mi.Name, retType, tParams, typeof(Hacks).Module, true);
			var il = dyn.GetILGenerator();
			int add = 0;
			if(!mi.IsStatic)
			{
				add = 1;
				EmitThis(il, tParams[0], mi.DeclaringType);
			}
			for(int i = add; i < tParams.Length; i++)
			{
				il.EmitLdarg(i);
				EmitCast(il, tParams[i], mParams[i-add]);
			}
			il.Emit(OpCodes.Call, mi);
			EmitCast(il, mi.ReturnType, retType);
			il.Emit(OpCodes.Ret);
			return (TDelegate)(object)dyn.CreateDelegate(delType);
		}
		
		/// <summary>
		/// Creates an invokable delegate from a constructor in a type, found by its index.
		/// </summary>
		public static TDelegate GetConstructor<TDelegate>(Type type, int ord) where TDelegate : TDelegateBase
		{
			return GetConstructor<TDelegate>(type.GetConstructors(privflags)[ord]);
		}
		
		/// <summary>
		/// Creates an invokable delegate from a specific constructor.
		/// </summary>
		public static TDelegate GetConstructor<TDelegate>(ConstructorInfo ctor) where TDelegate : TDelegateBase
		{
			Type delType = typeof(TDelegate);
			MethodSignature delSig = delType.GetDelegateSignature();
			Type[] tParams = delSig.ParameterTypes;
			Type retType = delSig.ReturnType;
			
			var mParams = ctor.GetParameters().Select(p => p.ParameterType).ToList();
			
			DynamicMethod dyn = new DynamicMethod("new_"+ctor.DeclaringType.Name, retType, tParams, typeof(Hacks).Module, true);
			var il = dyn.GetILGenerator();
			for(int i = 0; i < tParams.Length; i++)
			{
				il.EmitLdarg(i);
				EmitCast(il, tParams[i], mParams[i]);
			}
			il.Emit(OpCodes.Newobj, ctor);
			EmitCast(il, ctor.DeclaringType, retType);
			il.Emit(OpCodes.Ret);
			return (TDelegate)(object)dyn.CreateDelegate(delType);
		}
		
		/// <summary>
		/// Creates a get delegate from a specific field.
		/// </summary>
		public static TDelegate GetFieldGetter<TDelegate>(Type type, string field) where TDelegate : TDelegateBase
		{
			return GetFieldGetter<TDelegate>(type.GetField(field, privflags));
		}
		
		/// <summary>
		/// Creates a get delegate from a specific field.
		/// </summary>
		public static TDelegate GetFieldGetter<TDelegate>(FieldInfo fi) where TDelegate : TDelegateBase
		{
			Type delType = typeof(TDelegate);
			MethodSignature delSig = ReflectionTools.GetDelegateSignature(delType);
			Type[] tParams = delSig.ParameterTypes;
			Type retType = delSig.ReturnType;
			
			DynamicMethod dyn = new DynamicMethod("get_"+fi.Name, retType, tParams, typeof(Hacks).Module, true);
			var il = dyn.GetILGenerator();
			if(fi.IsStatic)
			{
				il.Emit(OpCodes.Ldsfld, fi);
			}else{
				EmitThis(il, tParams[0], fi.DeclaringType);
				il.Emit(OpCodes.Ldfld, fi);
			}
			EmitCast(il, fi.FieldType, retType);
			il.Emit(OpCodes.Ret);
			return (TDelegate)(object)dyn.CreateDelegate(delType);
		}
		
		/// <summary>
		/// Creates a set delegate from a specific field or a collection of fields.
		/// </summary>
		public static TDelegate GetFieldSetter<TDelegate>(Type type, params string[] fields) where TDelegate : TDelegateBase
		{
			return GetFieldSetter<TDelegate>(fields.Select(f => type.GetField(f, privflags)).ToList());
		}
		
		/// <summary>
		/// Creates a set delegate from a specific field or a collection of fields.
		/// </summary>
		public static TDelegate GetFieldSetter<TDelegate>(IList<FieldInfo> fields) where TDelegate : TDelegateBase
		{
			Type delType = typeof(TDelegate);
			MethodSignature delSig = delType.GetDelegateSignature();
			Type[] tParams = delSig.ParameterTypes;
			Type retType = delSig.ReturnType;
			
			DynamicMethod dyn = new DynamicMethod("set_"+String.Join(",", fields.Select(f => f.Name)), retType, tParams, typeof(Hacks).Module, true);
			var il = dyn.GetILGenerator();
			int add = 0;
			if(fields.Any(f => !f.IsStatic))
			{
				add = 1;
			}
			for(int i = 0; i < fields.Count; i++)
			{
				var fi = fields[i];
				if(fi.IsStatic)
				{
					il.EmitLdarg(i+add);
					EmitCast(il, tParams[i+add], fi.FieldType);
					il.Emit(OpCodes.Stsfld, fi);
				}else{
					EmitThis(il, tParams[0], fi.DeclaringType);
					il.EmitLdarg(i+add);
					EmitCast(il, tParams[i+add], fi.FieldType);
					il.Emit(OpCodes.Stfld, fi);
				}
			}
			il.Emit(OpCodes.Ret);
			return (TDelegate)(object)dyn.CreateDelegate(delType);
		}
		
		/// <summary>
		/// Creates a get delegate from a specific property.
		/// </summary>
		public static TDelegate GetPropertyGetter<TDelegate>(Type type, string property) where TDelegate : TDelegateBase
		{
			return GetPropertyGetter<TDelegate>(type.GetProperty(property, privflags));
		}
		
		/// <summary>
		/// Creates a get delegate from a specific property.
		/// </summary>
		public static TDelegate GetPropertyGetter<TDelegate>(PropertyInfo pi) where TDelegate : TDelegateBase
		{
			return GetInvoker<TDelegate>(pi.GetGetMethod(true));
		}
		
		/// <summary>
		/// Creates a set delegate from a specific property.
		/// </summary>
		public static TDelegate GetPropertySetter<TDelegate>(Type type, string property) where TDelegate : TDelegateBase
		{
			return GetPropertySetter<TDelegate>(type.GetProperty(property, privflags));
		}
		
		/// <summary>
		/// Creates a set delegate from a specific property.
		/// </summary>
		public static TDelegate GetPropertySetter<TDelegate>(PropertyInfo pi) where TDelegate : TDelegateBase
		{
			return GetInvoker<TDelegate>(pi.GetSetMethod(true));
		}
		
		private static void EmitThis(ILGenerator il, Type argType, Type privateType)
		{
			il.Emit(OpCodes.Ldarg_0);
			if(argType.IsByRef || argType.IsPointer)
			{
				argType = argType.GetElementType();
				if(argType == privateType)
				{
					if(argType.IsValueType)
					{
						return;
					}else{
						il.Emit(OpCodes.Ldobj, argType);
					}
				}else if(argType.IsValueType)
				{
					il.Emit(OpCodes.Ldobj, argType);
				}
			}
			EmitCast(il, argType, privateType);
		}
		
		private static void EmitCast(ILGenerator il, Type from, Type to)
		{
			if(from.IsValueType && !to.IsValueType)
			{
				il.Emit(OpCodes.Box, from);
			}else if(!from.IsValueType && to.IsValueType)
			{
				il.Emit(OpCodes.Unbox, to);
				return;
			}
			if(from.IsEnum && to == Enum.GetUnderlyingType(from) ||
			   to.IsEnum && from == Enum.GetUnderlyingType(to))
			{
				return;
			}
			if(TryEmitCast(from, to) || TryEmitCast(to, from))
			{
				il.Emit(OpCodes.Castclass, to);
			}
		}
	
		private static bool TryEmitCast(Type a, Type b)
		{
			if(a == b)
			{
				return false;
			}else if(a.IsAssignableFrom(b) || b.IsAssignableFrom(a))
			{
				return true;
			}else if(a.IsEnum && Enum.GetUnderlyingType(a) == b)
			{
				return false;
			}else if(a.IsEnum && b.IsEnum && Enum.GetUnderlyingType(a) == Enum.GetUnderlyingType(b))
			{
				return false;
			}else if(a.IsPointer && b.IsPointer)
			{
				return false;
			}else if(a.IsPointer && (b == typeof(IntPtr) || b == typeof(UIntPtr)))
			{
				return false;
			}
			return true;
		}
	}
}
