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
		#region Push Hackery
		private static List<Type> numberTypes = new List<Type> {
			typeof( int ),
			typeof( float ),
			typeof( decimal ),
			typeof( double ),
			typeof( long ),
			typeof( short ),
			typeof( byte ),
			typeof( ushort ),
			typeof( uint ),
			typeof( ulong ),
			typeof( sbyte )
		};
		#endregion
		
		public static void Push( Lua state, object o )
		{
			Type t = o.GetType();
			
			// nil == null
			if( o == null )
			{
				state.PushNil();
			}
			else if( numberTypes.Contains( t ) )
			{
				state.PushNumber( Convert.ToDouble( o ) );
			}
			else if( t == typeof( char ) || t == typeof( string ) )
			{
				state.PushString( o.ToString() );
			}
			else if( t == typeof( bool ) )
			{
				state.PushBoolean( (bool)o );
			}
			else if( t == typeof( CallbackFunction ) )
			{
				state.PushCFunction( (CallbackFunction)o );
			}
			else if( t == typeof( LuaFunction ) )
			{
				state.AuxGetRef( (int)PseudoIndice.Registry, (o as LuaFunction).reference );
			}
			else
			{
				throw new NotImplementedException( "Passing of exotic datatypes is not yet handled" );
			}			
		}
		
		public static object Pop( Lua state )
		{
			object o = GetObject( state, -1 );
			state.Pop( 1 );
			return o;
		}
		
		public static object GetObject( Lua state, int index )
		{
			LuaType type = state.Type( index );
			
			// TODO: Implement tables and other data structures.
			switch( type )
			{
				case LuaType.Number:
				{
					return state.ToNumber( index );
				}
				
				case LuaType.String:
				{
					return state.ToString( index );
				}
				
				case LuaType.Boolean:
				{
					return state.ToBoolean( index );
				}
				
				case LuaType.Table:
				{
					throw new NotImplementedException( "Grabbing of exotic datatypes is not yet handled" );
					//state.PushValue( index );
					//int reference = state.AuxRef( (int)PseudoIndice.Registry );
					//return new LuaTable( reference, interpreter );
				}
				
				case LuaType.Function:
				{
					// If we're not grabbing the function from the top of the stack we want to copy it to the top of the stack.
					if( index != -1 )
						state.PushValue( index );
				
					int reference = state.AuxRef( (int)PseudoIndice.Registry );
					return new LuaFunction( state, reference );
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
		public static void Traverse( Lua state, params object[] fragments )
		{
			// TODO: Give this a more meaningful value?
			state.CheckStack( fragments.Length );
			
			Push( state, fragments[0] );
			state.GetTable( (int)PseudoIndice.Globals );
			
			if( fragments.Length == 1 )
			{					
				return;
			}
			else
			{
				for( int i = 1; i < fragments.Length; i++ )
				{
					Push( state, fragments[i] );
					state.GetTable( -2 );
				}
			}
		}
	}
}
