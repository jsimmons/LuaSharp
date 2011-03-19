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
using System.Threading;

namespace LuaSharp
{
	/// <summary>
	/// Represents a reference to a function inside of a Lua state.
	/// </summary>
	public sealed class LuaFunction : IDisposable
	{
		private IntPtr state;
		internal volatile int reference;
		
		/// <summary>
		/// Creates a LuaFunction for the object on the top of the stack, and pops it.
		/// </summary>
		/// <param name="s">
		/// A Lua State
		/// </param>
		internal LuaFunction( IntPtr s )
		{
			state = s;
			reference = LuaLib.luaL_ref( state, (int)PseudoIndex.Registry );
		}		
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.LuaFunction"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.LuaFunction"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="LuaSharp.LuaFunction"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="LuaSharp.LuaFunction"/> so the garbage
		/// collector can reclaim the memory that the <see cref="LuaSharp.LuaFunction"/> was occupying.
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
		/// Call the function with the specified arguments.
		/// </summary>
		/// <param name='args'>
		/// The arguments to pass to the function.
		/// </param>
		/// <exception cref='InvalidOperationException'>
		/// Is thrown when an operation cannot be performed.
		/// </exception>
		/// <returns>
		/// The values passed back from the function as an array.
		/// </returns>
		public object[] Call( params object[] args )
		{
			int reference = this.reference;
			if( reference == (int)References.NoRef )
				throw new ObjectDisposedException( GetType().Name );
			else if( reference == (int)References.RefNil )
				throw new NullReferenceException();
			
			int oldTop = LuaLib.lua_gettop( state );

			if( !LuaLib.lua_checkstack( state, args.Length + 1 ) )
			{
				LuaLib.luaL_error(state, "stack overflow calling function", __arglist());
			}

			// Push the function.
			Helpers.Push( state, this );
			
			// Push the args
			foreach( object o in args )
			{
				Helpers.Push( state, o );
			}

			LuaLib.lua_call( state, args.Length, (int)LuaEnum.MultiRet );
			
			// Number of results is the new stack top - starting height of the stack.
			int returned = LuaLib.lua_gettop( state ) - oldTop;
			if( returned == 0 )
				return ClrFunction.emptyObjects;
			
			object[] returnedValues = new object[returned];
			for( int i = 0; i < returned; i++ )
			{
				returnedValues[i] = Helpers.Pop( state );
			}			
			
			return returnedValues;
		}
	}
}
