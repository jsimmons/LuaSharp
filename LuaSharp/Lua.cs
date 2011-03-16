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
using System.IO;
using System.Threading;

namespace LuaSharp
{
	/// <summary>
	/// Represents a Lua state.
	/// </summary>
	/// <remarks>
	/// A Lua state is akin to an AppDomain in .Net.
	/// </remarks>
	public class Lua : IDisposable
	{		
		internal IntPtr state;
		private CallbackFunction panicFunction;
		private volatile int disposed;
		
		/// <summary>
		/// Creates a new instance of the <see cref="Lua"/> class.
		/// </summary>
		public Lua()
		{
			state = LuaLib.luaL_newstate();
			
			panicFunction = ( IntPtr s ) => {
				throw new LuaException( "Error in call to Lua API: " + LuaLib.lua_tostring( s, -1 ) );
			};
			
			LuaLib.lua_atpanic( state, panicFunction );
			
			LuaLib.luaL_openlibs( state );
			
			LookupTable<IntPtr, Lua>.Store( state, this );
			
			disposed = 0;
		}
		
		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the <see cref="LuaSharp.Lua"/> is
		/// reclaimed by garbage collection.
		/// </summary>
		~Lua()
		{
			var wasDisposed = Interlocked.Exchange( ref disposed, 1 ) == 1;
			if( wasDisposed )
				return;
			
			Dispose( false );
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.Lua"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.Lua"/>. The <see cref="Dispose"/>
		/// method leaves the <see cref="LuaSharp.Lua"/> in an unusable state. After calling <see cref="Dispose"/>, you must
		/// release all references to the <see cref="LuaSharp.Lua"/> so the garbage collector can reclaim the memory that the
		/// <see cref="LuaSharp.Lua"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
			var wasDisposed = Interlocked.Exchange( ref disposed, 1 ) == 1;
			if( wasDisposed )
				return;
			
			Dispose( true );
			System.GC.SuppressFinalize( this );
		}
		
		/// <summary>
		/// Releases all resource used by the <see cref="LuaSharp.Lua"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="LuaSharp.Lua"/>. The <see cref="Dispose"/>
		/// method leaves the <see cref="LuaSharp.Lua"/> in an unusable state. After calling <see cref="Dispose"/>, you must
		/// release all references to the <see cref="LuaSharp.Lua"/> so the garbage collector can reclaim the memory that the
		/// <see cref="LuaSharp.Lua"/> was occupying.
		/// </remarks>
		protected virtual void Dispose( bool disposing )
		{
			if( disposing )
			{
				LookupTable<IntPtr, Lua>.Remove( state );
				LuaLib.lua_close( state );
				state = IntPtr.Zero;
			}
		}
		
		/// <summary>
		/// Gets or sets the a Lua object with the specified path.
		/// </summary>
		/// <param name='path'>
		/// The object path.
		/// </param>
		public object this[params object[] path]
		{
			get
			{
				return GetValue( path );
			}
			set
			{
				SetValue( value, path );
			}
		}
		
		/// <summary>
		/// Sets the object at path defined by fragments to the object o.
		/// </summary>
		/// <param name="o">
		/// A <see cref="System.Object"/> the object to set.
		/// </param>
		/// <param name="path">
		/// A <see cref="System.Object[]"/> containing the path to where the object should be stored.
		/// </param>
		public void SetValue( object o, params object[] path )
		{
			if( disposed == 1 )
				throw new ObjectDisposedException( GetType().Name );
			
			if( path == null || path.Length == 0 )
			{
				throw new ArgumentNullException( "path" );
			}
			else if( path.Length == 1 )
			{
				// Push the key.
				Helpers.Push( state, path[0] );
				
				// Push the value.
				Helpers.Push( state, o );
				
				// Perform the set.
				LuaLib.lua_settable( state, (int)PseudoIndex.Globals );
			}
			else
			{
				int len = path.Length - 1;
				object[] fragments = path.Slice( 0, len );				
				object final = path[len];
			
				// Need this here else we don't have enough control.
				Helpers.Push( state, fragments[0] );
				LuaLib.lua_gettable( state, (int)PseudoIndex.Globals );
				
				/// Traverse the main section of the path, leaving the last table on top.
				Helpers.Traverse( state, fragments );
				
				// Push the final key.
				Helpers.Push( state, final );
				
				// Push the value.
				Helpers.Push( state, o );
				
				// Perform the set.
				LuaLib.lua_settable( state, -3 );
				
				// Remove the last table.
				LuaLib.lua_pop( state, -1 );
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
			if( disposed == 1 )
				throw new ObjectDisposedException( GetType().Name );
			
			if( path == null || path.Length == 0 )
			{
				throw new ArgumentNullException( "path" );
			}
			else if( path.Length == 1 )
			{
				Helpers.Push( state, path[0] );
				LuaLib.lua_gettable( state, (int)PseudoIndex.Globals );
				return Helpers.Pop( state );
			}
			else
			{
				int len = path.Length - 1;
				object[] fragments = path.Slice( 0, len );
				object final = path[len];
				
				// Need this here else we don't have enough control.
				Helpers.Push( state, fragments[0] );
				LuaLib.lua_gettable( state, (int)PseudoIndex.Globals );
				
				// Traverse the main section of the path, leaving the last table on the top.
				Helpers.Traverse( state, fragments );
				
				// Push the final key.
				Helpers.Push( state, final );
				
				// Grab the result and throw it on the top of the stack.
				LuaLib.lua_gettable( state, -2 );
				
				object o = Helpers.Pop( state );
				
				// Remove the last table.
				LuaLib.lua_pop( state, -1 );
				
				return o;
			}
		}
		
		/// <summary>
		/// Creates a new table in the state.
		/// </summary>
		/// <param name='path'>
		/// The object path for the table.
		/// </param>
		public LuaTable CreateTable( params object[] path )
		{
			return CreateTable( 0, 0, path );
		}
		
		/// <summary>
		/// Creates the table.
		/// </summary>
		/// <param name='initialArrayCapacity'>
		/// The initial capacity for array elements.
		/// </param>
		/// <param name='initialNonArrayCapacity'>
		/// THe initial capacity for non-array elements.
		/// </param>
		/// <param name='path'>
		/// The object path.
		/// </param>
		/// <exception cref='ArgumentNullException'>
		/// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
		/// </exception>
		public LuaTable CreateTable( int initialArrayCapacity, int initialNonArrayCapacity, params object[] path )
		{
			if( disposed == 1 )
				throw new ObjectDisposedException( GetType().Name );
			
			if( path == null || path.Length == 0 )
			{
				throw new ArgumentNullException( "path" );
			}
			else if( path.Length == 1 )
			{
				// Push the key.
				Helpers.Push( state, path[0] );

				// Push the value.
				LuaLib.lua_createtable( state, initialArrayCapacity, initialNonArrayCapacity );

				// Perform the set.
				LuaLib.lua_settable( state, (int)PseudoIndex.Globals );
			}
			else
			{
				int len = path.Length - 1;
				object[] fragments = path.Slice( 0, len );
				object final = path[len];

				// Need this here else we don't have enough control.
				Helpers.Push( state, fragments[0] );
				LuaLib.lua_gettable( state, (int)PseudoIndex.Globals );

				// Traverse the main section of the path, leaving the last table on top.
				Helpers.Traverse( state, fragments );

				// Push the final key.
				Helpers.Push( state, final );

				// Push the value.
				LuaLib.lua_createtable( state, initialArrayCapacity, initialNonArrayCapacity );

				// Perform the set.
				LuaLib.lua_settable( state, -3 );

				// Remove the last table.
				LuaLib.lua_pop( state, -1 );
			}
			
			return (LuaTable) GetValue( path );
		}
		
		/// <summary>
		/// Executes a literal chunk of Lua code.
		/// </summary>
		/// <param name="chunk">
		/// A <see cref="System.String"/> that containes the chunk to execute.
		/// </param>
		public void DoString( string chunk )
		{
			if( disposed == 1 )
				throw new ObjectDisposedException( GetType().Name );
			
			if( string.IsNullOrEmpty( chunk ) )
				throw new ArgumentNullException( "chunk" );
			
			if( !LuaLib.luaL_dostring( state, chunk ) )
				throw new LuaException( "Error executing chunk: " + LuaLib.lua_tostring( state, -1 ) );
		}
		
		/// <summary>
		/// Executes the code contained in a file.
		/// </summary>
		/// <param name="filename">
		/// A <see cref="System.String"/> containing the path to the file.
		/// </param>
		public void DoFile( string filename )
		{
			if( disposed == 1 )
				throw new ObjectDisposedException( GetType().Name );
			
			if( string.IsNullOrEmpty( filename ) )
				throw new ArgumentNullException( "filename" );
			if( !File.Exists( filename ) )
				throw new ArgumentOutOfRangeException( filename + " does not exist.", new FileNotFoundException( ) );
			
			filename = Path.GetFullPath( filename );
			if( !LuaLib.luaL_dofile( state, filename ) )
				throw new LuaException( "Error executing file: " + LuaLib.lua_tostring( state, -1 ) );
		}
	}
}
