// 
// LuaState.cs
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

/// <summary>
/// This will form a high level abstraction of the Lua API, making use of the Lua P/Invoke wrapper it will provide an easy to use interface.
/// </summary>

using System;
using System.Collections.Generic;

using LuaWrap;

namespace LuaSharp
{
	public class LuaState
	{		
		internal Lua state;		
		private CallbackFunction panicFunction;
		
		public LuaState()
		{
			state = new Lua();
			state.AuxOpenLibs();
			
			panicFunction = ( IntPtr s ) => { 
				Lua l = new Lua( s );
				throw new LuaException( "Unprotected Lua error: " + l.ToString( -1 ) );
			};
		
			state.AtPanic( panicFunction );			
		}
		
		public object this[params object[] path]
		{
			get
			{
				return GetValue( path );
			}
			set
			{
				SetValue( path );
			}
		}
		
		/// <summary>
		/// Sets the object at path defined by fragments to the object o.
		/// </summary>
		/// <param name="o">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <param name="fragments">
		/// A <see cref="System.Object[]"/>
		/// </param>
		public void SetValue( object o, params object[] path )
		{
			if( path.Length == 1 )
			{
				// Push the key.
				Helpers.Push( state, path[0] );
				
				// Push the value.
				Helpers.Push( state, o );
				
				// Perform the set.
				state.SetTable( (int)PseudoIndice.Globals );
			}
			else
			{
				int oldTop = state.GetTop();
				
				int len = path.Length - 1;
				object[] fragments = path.Slice( 0, len );
				object final = path[len];
			
				/// Traverse the main section of the path, leaving the last table on top.
				Helpers.Traverse( state, fragments );
				
				// Push the final key.
				Helpers.Push( state, final );
				
				// Push the value.
				Helpers.Push( state, o );
				
				// Perform the set.
				state.SetTable( -3 );
				
				state.SetTop( oldTop );
			}
		}
		
		/// <summary>
		/// Returns the value at the given path.
		/// </summary>
		/// <param name="fragments">
		/// A <see cref="System.Object[]"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Object"/>
		/// </returns>
		public object GetValue( params object[] path )
		{
			if( path.Length == 1 )
			{
				Helpers.Push( state, path[0] );
				state.GetTable( (int)PseudoIndice.Globals );
				return Helpers.Pop( state );
			}
			else
			{
				int oldTop = state.GetTop();
				
				int len = path.Length - 1;
				object[] fragments = path.Slice( 0, len );
				object final = path[len];
				
				// Traverse the main section of the path, leaving the last table on the top.
				Helpers.Traverse( state, fragments );
				
				// Push the final key.
				Helpers.Push( state, final );
				
				// Grab the result and throw it on the top of the stack.
				state.GetTable( -2 );
				
				object o = Helpers.Pop( state );
				
				state.SetTop( oldTop );
				
				return o;
			}
		}
		
		public void DoString( string chunk )
		{
			if( !state.AuxDoString( chunk ) )
				throw new LuaException( "Error executing chunk: " + state.ToString( -1 ) );
		}
	}
}
