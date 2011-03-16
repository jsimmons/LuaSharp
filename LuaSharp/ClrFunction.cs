// 
// ClrFunction.cs
//  
// Author:
//       Jonathan Dickinson <jonathan@dickinsons.co.za>
// 
// Copyright (c) 2011 Jonathan Dickinson
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

namespace LuaSharp
{
	/// <summary>
	/// Represents a CLR function.
	/// </summary>
	/// <remarks>
	/// It is safe to share functions across <see cref="Lua"/> states.
	/// </remarks>
	public abstract class ClrFunction
	{
		internal CallbackFunction callback;
		internal static readonly object[] emptyObjects = new object[0];
		private string name;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LuaSharp.ClrFunction"/> class.
		/// </summary>
		public ClrFunction( )
		{
			name = GetType().FullName;
			callback = Invoke;
		}
		
		/// <summary>
		/// Called when lua requests that the function is invoked.
		/// </summary>
		/// <param name='lua'>
		/// The lua state.
		/// </param>
		private int Invoke( IntPtr s )
		{
			Lua lua;
			if( !LookupTable<IntPtr, Lua>.Retrieve( s, out lua ) )
			{
				try
				{
					LuaLib.lua_close( s ); // This somehow didn't get called.
				}
				catch { }
				return 0;
			}
			
			int argc = LuaLib.lua_gettop( s );
			object[] args;
			if( argc == 0 )
			{
				args = emptyObjects;
			}
			else
			{
				args = new object[argc];
				for( int i = 0; i < argc; i++)
				{
					object argv = Helpers.GetObject( s, i + 1);
					args[i] = argv;
				}
			}
			
			try
			{
				args = OnInvoke( lua, args ) ?? emptyObjects;
			}
			catch (Exception ex)
			{
				Helpers.Throw( s, "CLR exception when calling function: {0}: {1}", name, ex.Message );
				return 0;
			}
			
			if( args.Length > 0 && !LuaLib.lua_checkstack( s, args.Length ) )
			{
				Helpers.Throw( s, "Not enough space to allocate return values for function: {0}", name );
				return 0;
			}
			
			var lastValue = 0;
			try
			{
				for( int i = 0; i < args.Length; lastValue = i ++ )
				{
					Helpers.Push( s, args[i] );
				}
				return args.Length;
			}
			catch (Exception ex)
			{
				Helpers.Throw( s, "Failed to allocate return value for function: {0},{1}: {2}", name, lastValue, ex.Message );
				return 0;
			}
		}
		
		/// <summary>
		/// Called when the function should be invoked.
		/// </summary>
		/// <param name='state'>
		/// The Lua state that is calling the function.
		/// </param>
		/// <param name='args'>
		/// The arguments that were passed to the function.
		/// </param>
		/// <returns>
		/// The output values for the function. If no values are
		/// returned; null should be used.
		/// </returns>
		protected abstract object[] OnInvoke( Lua state, object[] args );
		
		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		/// <value>
		/// The name of the function.
		/// </value>
		public string Name
		{
			get
			{
				return name;
			}
		}
	}
}

