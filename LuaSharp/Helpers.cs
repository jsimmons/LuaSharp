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

namespace LuaSharp
{
	internal static class Helpers
	{
		#region Push Hackery with Less Hackery
		private static readonly HashSet<IntPtr> numberTypes = new HashSet<IntPtr>();
		private static readonly IntPtr 
				stringHandle, charHandle,
				boolHandle,
				tableHandle,
				callbackHandle,
				functionHandle;
		
		static Helpers()
		{
			numberTypes.Add( typeof( int ).TypeHandle.Value );
			numberTypes.Add( typeof( float ).TypeHandle.Value );
			numberTypes.Add( typeof( decimal ).TypeHandle.Value );
			numberTypes.Add( typeof( double ).TypeHandle.Value );
			numberTypes.Add( typeof( long ).TypeHandle.Value );
			numberTypes.Add( typeof( short ).TypeHandle.Value );
			numberTypes.Add( typeof( byte ).TypeHandle.Value );
			numberTypes.Add( typeof( ushort ).TypeHandle.Value );
			numberTypes.Add( typeof( uint ).TypeHandle.Value );
			numberTypes.Add( typeof( ulong ).TypeHandle.Value );
			numberTypes.Add( typeof( sbyte ).TypeHandle.Value );
			
			stringHandle = typeof( string ).TypeHandle.Value;
			charHandle = typeof( char ).TypeHandle.Value;
			boolHandle = typeof( bool ).TypeHandle.Value;
			tableHandle = typeof( LuaTable ).TypeHandle.Value;
			callbackHandle = typeof( CallbackFunction ).TypeHandle.Value;
			functionHandle = typeof( LuaFunction ).TypeHandle.Value;
		}
		#endregion
		
		public static void Push( IntPtr state, object o )
		{
			// nil == null
			if( o == null )
			{
				LuaLib.lua_pushnil( state );
				return;
			}
			
			var rth = Type.GetTypeHandle( o ).Value;
			if( rth == charHandle || rth == stringHandle )
			{
				LuaLib.lua_pushstring( state, o.ToString(  ) );
			}
			else if( rth == boolHandle )
			{
				LuaLib.lua_pushboolean( state, (bool)o );
			}
			else if( rth == tableHandle )
			{
				LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, (o as LuaTable).reference );
			}
			else if( rth == callbackHandle )
			{
				LuaLib.lua_pushcfunction( state, o as CallbackFunction );
			}
			else if( rth == functionHandle )
			{
				LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, (o as LuaFunction).reference );
			}
			else if( o is ClrFunction )
			{
				LuaLib.lua_pushcfunction( state, (o as ClrFunction).callback );
			}
			else
			{
				if( numberTypes.Contains( rth ) )
				{
					LuaLib.lua_pushnumber( state, Convert.ToDouble( o ) );
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
