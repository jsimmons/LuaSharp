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

namespace LuaSharp
{
	public class LuaFunction : IDisposable
	{
		private IntPtr state;
		internal int reference;
		
		public LuaFunction( IntPtr state, int reference )
		{
			this.state = state;
			this.reference = reference;		
		}
		
		~LuaFunction()
		{
			Dispose( false );
		}
		
		public void Dispose()
		{
			Dispose( true );
			System.GC.SuppressFinalize( this );
		}
		
		protected virtual void Dispose( bool disposing )
		{
			if( reference == (int)References.NoRef )
				return;
			
//			Console.WriteLine( "Disposing a Function ref = {0} state = {1}", reference, state.ToString() );
//			LuaLib.luaL_unref( state, (int)PseudoIndice.Registry, reference );
//			reference = (int)References.NoRef;
		}
		
		public object[] Call( params object[] args )
		{
			int oldTop = LuaLib.lua_gettop( state );
						
			if( !LuaLib.lua_checkstack( state, args.Length + 1 ) )
			{
				// Doing lua error manually as Mono does not like luaL_error currently.
				Helpers.Push( state, "Stack overflow calling function: " );
				LuaLib.luaL_where( state, 1 ); // TODO: not sure if this is working.
				LuaLib.lua_concat( state, 2 );
				LuaLib.lua_error( state );
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
			object[] returnedValues = new object[returned];
			for( int i = 0; i < returned; i++ )
			{
				returnedValues[i] = Helpers.Pop( state );
			}
			
			// Return state to former glory incase something went amiss.
			LuaLib.lua_settop( state, oldTop );
			
			return returnedValues;
		}
	}
}
