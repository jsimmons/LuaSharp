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
using System.Threading;

namespace LuaSharp
{
	/// <summary>
	/// Represents a CLR function.
	/// </summary>
	/// <remarks>
	/// It is safe to share functions across <see cref="Lua"/> states.
	/// </remarks>
	public abstract class ClrFunction : IDisposable
	{
		internal CallbackFunction callback;
		internal static readonly object[] emptyObjects = new object[0];
		private string name;
		private volatile int disposed;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LuaSharp.ClrFunction"/> class.
		/// </summary>
		public ClrFunction( )
		{
			name = GetType().FullName;
			callback = Invoke;
			disposed = 0;
		}
		
		/// <summary>
		/// Called when lua requests that the function is invoked.
		/// </summary>
		/// <param name='lua'>
		/// The lua state.
		/// </param>
		internal virtual int Invoke( IntPtr s )
		{
			if( disposed == 1 )
			{
				LuaLib.luaL_error(s, "function '%s' has been disposed", __arglist(name));
				return 0;
			}
			
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
				LuaLib.luaL_error(s, "exception calling function '%s' - %s", __arglist(name, ex.Message));
				return 0;
			}
			
			if( args.Length > 0 && !LuaLib.lua_checkstack( s, args.Length ) )
			{
				LuaLib.luaL_error(s, "not enough space for return values of function '%s'", __arglist(name));
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
				LuaLib.luaL_error(s, "failed to allocate return value for function '%s','%s' - '%s'", __arglist(name, lastValue, ex.Message));
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
		/// Invokes the <see cref="Dispose(bool)"/> method, guarding against
		/// repeated disposes.
		/// </summary>
		/// <remarks>
		/// This can be used to call <see cref="Dispose(bool)"/> from a finalizer
		/// if one is added to a class that inherits from this one.
		/// </remarks>
		protected void InvokeDispose()
		{
			var wasDisposed = Interlocked.Exchange( ref disposed, 1 ) == 1;
			if( wasDisposed )
				return;
			
			Dispose( false );
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.ClrFunction"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.ClrFunction"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="LuaSharp.ClrFunction"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="LuaSharp.ClrFunction"/> so the garbage
		/// collector can reclaim the memory that the <see cref="LuaSharp.ClrFunction"/> was occupying.
		/// </remarks>
		public void Dispose ()
		{
			var wasDisposed = Interlocked.Exchange( ref disposed, 1 ) == 1;
			if( wasDisposed )
				return;
			
			callback = null;
			name = null;
			
			Dispose( true );
			GC.SuppressFinalize( this );
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.ClrFunction"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.ClrFunction"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="LuaSharp.ClrFunction"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="LuaSharp.ClrFunction"/> so the garbage
		/// collector can reclaim the memory that the <see cref="LuaSharp.ClrFunction"/> was occupying.
		/// </remarks>
		protected virtual void Dispose( bool disposing )
		{
			
		}
	}
}

