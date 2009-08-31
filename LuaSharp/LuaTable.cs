// 
// LuaTable.cs
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

// 
// LuaFunction.cs
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
using System.Collections;

namespace LuaSharp
{
	public class LuaTable : IDisposable
	{
		private IntPtr state;
		internal int reference;
		
		/// <summary>
		/// Creates a LuaFunction for the object on the top of the stack, and pops it.
		/// </summary>
		/// <param name="s">
		/// A Lua State
		/// </param>
		public LuaTable( IntPtr s )
		{
			state = s;
			reference = LuaLib.luaL_ref( state, (int)PseudoIndex.Registry );
		}		
		
		public void Dispose()
		{
			if( reference == (int)References.NoRef )
				return;

			LuaLib.luaL_unref( state, (int)PseudoIndex.Registry, reference );
			reference = (int)References.NoRef;
			
			System.GC.SuppressFinalize( this );
		}

		public object this[object path]
		{
			get
			{
				return GetValue( path );
			}
			set
			{
				SetValue( path, value );
			}
		}
		
		public object GetValue( object key )
		{		
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			LuaLib.lua_gettable( state, -2 );
			return Helpers.Pop( state );
		}
		
		public void SetValue( object key, object value )
		{
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			Helpers.Push( state, value );
			LuaLib.lua_settable( state, -3 );
		}
	}
}
