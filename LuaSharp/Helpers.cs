// 
// Helpers.cs
//  
// Author:
//       Joshua Simmons <simmons.44@gmail.com>
// 
// Copyright (c) 2009 Joshua Simmons
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using LuaWrap;
using System.Globalization;

namespace LuaSharp
{
	internal static class Helpers
	{
		private static readonly Dictionary<RuntimeTypeHandle, Action<IntPtr, object>> pushers;
		
		static Helpers()
		{
			pushers = new Dictionary<RuntimeTypeHandle, Action<IntPtr, object>>( 3 );
			
			pushers.Add( 
				typeof( LuaTable ).TypeHandle, (x, y) => 
				LuaLib.luaL_getref( x, (int)PseudoIndex.Registry, ( (LuaTable) y ).reference ) );
			
			pushers.Add( 
				typeof( CallbackFunction ).TypeHandle, (x, y) => 
				LuaLib.lua_pushcfunction( x, (CallbackFunction) y ) );
			
			pushers.Add( 
				typeof( LuaFunction ).TypeHandle, (x, y) => 
				LuaLib.luaL_getref( x, (int)PseudoIndex.Registry, ( (LuaFunction) y ).reference ) );
		}
		
		public static void Push( IntPtr state, object o )
		{
			// nil == null
			if( o == null )
			{
				LuaLib.lua_pushnil( state );
				return;
			}
			
			// First do a switch lookup. Switch is incredibly fast.
			IConvertible iConvertible = o as IConvertible;
			if( iConvertible != null )
			{
				switch( iConvertible.GetTypeCode() )
				{
					case TypeCode.Char:
					case TypeCode.String:
						LuaLib.lua_pushstring( state, o.ToString(  ) );
						return;
					case TypeCode.Boolean:
						LuaLib.lua_pushboolean( state, (bool)o );
						return;
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Decimal:
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						LuaLib.lua_pushnumber( state, iConvertible.ToDouble( CultureInfo.InvariantCulture ) );
						return;
				}
			}
			
			// Now handle the sealed classes. We can use RuntimeTypeHandle
			// because of the lack of inheritance.
			// RTH is fast because it's incredibly easy for the CLR to
			// lookup in memory.
			RuntimeTypeHandle rth = Type.GetTypeHandle( o );
			Action<IntPtr, object> pusher;
			if( pushers.TryGetValue( rth, out pusher ) )
			{
				pusher( state, o );
			}
			else
			{
				// Now handle cases where inheritance can happen.
				ClrFunction clrFunction = o as ClrFunction;
				if( clrFunction != null )
				{
					LuaLib.lua_pushcfunction( state, clrFunction.callback );
				}
				else
				{
					throw new NotImplementedException( "Passing of exotic datatypes is not yet handled" );
				}
			}
		}
		
		/// <summary>
		/// Pops and returns a value from the top of the stack.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Object"/>
		/// </returns>
		public static object Pop( IntPtr state )
		{
			object o = GetObject( state, -1 );			
			LuaLib.lua_pop( state, 1 );

			return o;
		}
		
		/// <summary>
		/// Returns an object from the given index of the stack, does not remove.
		/// </summary>
		/// <param name="state">
		/// A <see cref="IntPtr"/>
		/// </param>
		/// <param name="index">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Object"/>
		/// </returns>
		public static object GetObject( IntPtr state, int index )
		{
			LuaType type = LuaLib.lua_type( state, index );
			
			// TODO: Implement tables and other data structures.
			switch( type )
			{
				case LuaType.Number:
				{
					return LuaLib.lua_tonumber( state, index );
				}
				
				case LuaType.String:
				{
					return LuaLib.lua_tostring( state, index );
				}
				
				case LuaType.Boolean:
				{
					return LuaLib.lua_toboolean( state, index );
				}
				
				case LuaType.Table:
				{					
					LuaLib.lua_pushvalue( state, index );
					return new LuaTable( state );
				}
				
				case LuaType.Function:
				{
					LuaLib.lua_pushvalue( state, index );
					return new LuaFunction( state );
				}
				
				case LuaType.Nil:
				{
					return null;
				}
				
				case LuaType.None:
				{
					return null;
				}
				
				default:
				{
					throw new NotImplementedException( "Grabbing of exotic datatypes is not yet handled" );
				}
			}
		}
		
		/// <summary>
		/// Traverses a given set of fragments and leaves the result on the top of the stack.
		/// </summary>
		/// <param name="fragments">
		/// A <see cref="System.Object[]"/>
		/// </param>		
		public static void Traverse( IntPtr state, params object[] fragments )
		{			
			for( int i = 1; i < fragments.Length; i++ )
			{
				Push( state, fragments[i] );
				LuaLib.lua_gettable( state, -2 );
				LuaLib.lua_remove( state, -2 );
			}
		}
		
		/// <summary>
		/// Throw the specified message into the specified state as an error.
		/// </summary>
		/// <param name='s'>
		/// The state.
		/// </param>
		/// <param name='message'>
		/// The format arguments.
		/// </param>
		public static void Throw( IntPtr s, string message, params object[] args )
		{
			if( args != null && args.Length != 0 )
				Helpers.Push( s, string.Format(message, args) );
			else
				Helpers.Push( s, message );
			
			LuaLib.luaL_where( s, 1 ); // TODO: not sure if this is working.
			LuaLib.lua_concat( s, 2 );
			LuaLib.lua_error( s );
		}
	}
}
