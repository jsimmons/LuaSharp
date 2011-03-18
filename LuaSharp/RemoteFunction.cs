// 
// RemoteFunction.cs
//  
// Author:
//       Jonathan Dickinson <jonathan@dickinsons.co.za>
// 
// Copyright (c) 2011 Copyright (c) 2011 Grounded Games
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
using LuaWrap;
using System.Collections.Generic;
using System.Linq;

namespace LuaSharp
{
	/// <summary>
	/// Represents a function that allows one Lua state to call another.
	/// </summary>
	public sealed class RemoteFunction : ClrFunction
	{
		/// <summary>
		/// Performs a lookup
		/// </summary>
		public static Func<object, Lua> LookupFunction;
		
		/// <summary>
		/// The function instance.
		/// </summary>
		public static readonly RemoteFunction Instance = new RemoteFunction();
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LuaSharp.RemoteFunction"/> class.
		/// </summary>
		public RemoteFunction ()
		{
			
		}
		
		// Does nothing.
		protected override object[] OnInvoke (Lua state, object[] args)
		{
			throw new NotImplementedException ();
		}
		
		internal override int Invoke (IntPtr s)
		{
			// NB: This code is hairy/repetitive for a reason.
			// While I could have done everything using 
			if( LookupFunction == null )
			{
				Helpers.Throw( s, "Remoting is not supported by the environment." );
				return 0;
			}
			
			IntPtr lstate = s;
			int roldTop = 0;
			IntPtr rstate = IntPtr.Zero;
			Lua rLua = null;
			object[] path = null;
			
			int argc = LuaLib.lua_gettop( s );
			if( argc < 2 )
			{
				Helpers.Throw( s, "Both the script key and the function name needs to be provided." );
				return 0;
			}
						
			#region Enumerate args
			for( int i = 0; i < argc; i++)
			{
				LuaType type = LuaLib.lua_type( lstate, i + 1 );
				
				if( i == 0 ) // Script key.
				{
					try
					{
						object key = Helpers.GetObject( lstate, i + 1 );
						rLua = LookupFunction( Helpers.GetObject( lstate, i + 1 ) );
						if( rLua == null )
						{
							Helpers.Throw( s, "Could not find remote script with key: {0}", key );
							return 0;
						}
						rstate = rLua.state;
						roldTop = LuaLib.lua_gettop( rstate );
						
						if( !LuaLib.lua_checkstack( rstate, argc + 1 ) )
						{
							Helpers.Throw( rstate, "Stack overflow calling function: " );
							return 0;
						}
					}
					catch (Exception ex)
					{
						Helpers.Throw( s, "Error executing script lookup: {0}", ex.Message );
						return 0;
					}
					continue;
				}
				else if ( i == 1 ) // Function path.
				{
					object pathObj = Helpers.GetObject( lstate, i + 1 );
					if( pathObj is LuaTable )
					{
						List<object> list = new List<object>( );
						list.AddRange( ( (LuaTable) pathObj ).Select( x => x.Value ) );
						path = list.ToArray( );
					}
					else // Allow e.g. remote( "libcool", "coolstuff", args );
					{
						path = new object[] { pathObj };
					}
					var func = rLua.GetValue(path);
					LuaLib.luaL_getref( rstate, (int)PseudoIndex.Registry, ( (LuaFunction) func ).reference );
					continue;
				}
				
				switch (type) 
				{
					case LuaType.Number:
						LuaLib.lua_pushnumber( rstate, LuaLib.lua_tonumber( lstate, i + 1 ) );
						break;
					case LuaType.String:
						LuaLib.lua_pushstring( rstate, LuaLib.lua_tostring( lstate, i + 1 ) );
						break;
					case LuaType.Boolean:
						LuaLib.lua_pushboolean( rstate, LuaLib.lua_toboolean( lstate, i + 1 ) );
						break;
					case LuaType.Table:	
						LuaTable.CloneToState( lstate, rstate, i + 1 );
						break;
					case LuaType.Function:
						Helpers.Throw( s, "Callback functions are not yet supported." );
						return 0;
					case LuaType.Nil:
						LuaLib.lua_pushnil( rstate );
						break;
					case LuaType.None:
						LuaLib.lua_pushnil( rstate );
						break;
					default:
						Helpers.Throw( s, "Grabbing of exotic datatypes is not yet supported.");
						return 0;
				}
			}
			#endregion
			
			LuaLib.lua_call( rstate, argc - 2, (int)LuaEnum.MultiRet );
			
			#region Get Return Values
			int returned = LuaLib.lua_gettop( rstate ) - roldTop;
			for( int i = 0; i < returned; i++)
			{				
				LuaType type = LuaLib.lua_type( rstate, -1 );
				
				switch (type) 
				{
					case LuaType.Number:
						LuaLib.lua_pushnumber( lstate, LuaLib.lua_tonumber( rstate, -1 ) );
						break;
					case LuaType.String:
						LuaLib.lua_pushstring( lstate, LuaLib.lua_tostring( rstate, -1 ) );
						break;
					case LuaType.Boolean:
						LuaLib.lua_pushboolean( lstate, LuaLib.lua_toboolean( rstate, -1 ) );
						break;
					case LuaType.Table:	
						LuaTable.CloneToState( lstate, rstate, -1 );
						break;
					case LuaType.Function:
						Helpers.Throw( s, "Callback functions are not yet supported." );
						return 0;
					case LuaType.Nil:
						LuaLib.lua_pushnil( lstate );
						break;
					case LuaType.None:
						LuaLib.lua_pushnil( lstate );
						break;
					default:
						Helpers.Throw( s, "Grabbing of exotic datatypes is not yet supported.");
						return 0;
				}
				LuaLib.lua_pop( rstate, 1 );
			}
			#endregion
			
			return returned;
		}
	}
}

