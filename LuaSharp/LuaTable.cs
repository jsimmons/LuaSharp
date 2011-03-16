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
using System.Threading;

namespace LuaSharp
{
	/// <summary>
	/// Represents a Lua table.
	/// </summary>
	/// <exception cref='ArgumentNullException'>
	/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
	/// </exception>
	public sealed class LuaTable : IDisposable
	{
		private IntPtr state;
		internal int reference;
		
		/// <summary>
		/// Creates a LuaFunction for the object on the top of the stack, and pops it.
		/// </summary>
		/// <param name="s">
		/// A Lua State
		/// </param>
		internal LuaTable( IntPtr s )
		{
			state = s;
			reference = LuaLib.luaL_ref( state, (int)PseudoIndex.Registry );
		}		
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.LuaTable"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.LuaTable"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="LuaSharp.LuaTable"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="LuaSharp.LuaTable"/> so the garbage
		/// collector can reclaim the memory that the <see cref="LuaSharp.LuaTable"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
			var oldReference = Interlocked.Exchange( ref reference, (int)References.NoRef );
			if( oldReference == (int)References.NoRef )
				return;

			LuaLib.luaL_unref( state, (int)PseudoIndex.Registry, oldReference );
			
			System.GC.SuppressFinalize( this );
		}
		
		/// <summary>
		/// Gets or sets the Lua object with the specified path.
		/// </summary>
		/// <param name='path'>
		/// The object path.
		/// </param>
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
		
		/// <summary>
		/// Gets a value from the table given a key.
		/// </summary>
		/// <returns>
		/// The value.
		/// </returns>
		/// <param name='key'>
		/// The key.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public object GetValue( object key )
		{
			int reference = this.reference;
			if( reference == (int)References.NoRef )
				throw new ObjectDisposedException( GetType().Name );
			else if( reference == (int)References.RefNil )
				throw new NullReferenceException();
			
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			LuaLib.lua_gettable( state, -2 );
			return Helpers.Pop( state );
		}
		
		/// <summary>
		/// Sets a value in the table given a key.
		/// </summary>
		/// <param name='key'>
		/// The key.
		/// </param>
		/// <param name='value'>
		/// The value.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public void SetValue( object key, object value )
		{
			int reference = this.reference;
			if( reference == (int)References.NoRef )
				throw new ObjectDisposedException( GetType().Name );
			else if( reference == (int)References.RefNil )
				throw new NullReferenceException();
			
			if( key == null )
				throw new ArgumentNullException( "key" );

			LuaLib.luaL_getref( state, (int)PseudoIndex.Registry, reference );
	
			Helpers.Push( state, key );
			Helpers.Push( state, value );
			LuaLib.lua_settable( state, -3 );
		}
	}
}
