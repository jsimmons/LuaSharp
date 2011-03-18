// 
// LuaTest.cs
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
using System.IO;
using LuaSharp;
using NUnit.Framework;

namespace LuaSharp.Tests
{
	/// <summary>
	/// Represents a test for Lua scripting.
	/// </summary>
	public class LuaTest
	{
		/// <summary>
		/// Represents the print function.
		/// </summary>
		private class PrintFunction : ClrFunction
		{
			private TextWriter writer;
			
			public PrintFunction( TextWriter writer )
			{
				this.writer = writer;
			}
			
			protected override object[] OnInvoke (Lua state, object[] args)
			{
				if( args.Length != 1 )
					throw new ArgumentOutOfRangeException( "args" );
				writer.WriteLine( args[0] );
				return null;
			}
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="LuaSharp.Tests.LuaTest"/> class.
		/// </summary>
		public LuaTest( )
		{
			RemoteFunction.LookupFunction = Lookup;
		}
		
		protected void AssertOutput( string script, string function, string expectedOutput )
		{
			AssertOutput( script, function, expectedOutput, (string)null );
		}
		
		protected void AssertOutput( string script, string function, string expectedOutput, string message )
		{
			using( Lua lua = new Lua( ) )
			{
				lua.DoString( script );
				AssertOutput( lua, function, expectedOutput, message );
			}
		}
		
		protected void AssertOutput( Lua lua, string function, string expectedOutput )
		{
			AssertOutput( lua, function, expectedOutput, (string)null );
		}
		
		private static Lua Lookup( object key )
		{
			Lua result;
			LookupTable<string, Lua>.Retrieve( (string)key, out result );
			return result;
		}
		
		protected void AssertOutput( Lua lua, string function, string expectedOutput, string message )
		{
			
			expectedOutput = expectedOutput.Replace( "\r\n", "\n" ); // Get rid of Windows problems.
			using( StringWriter writer = new StringWriter( ) )
			{
				PrintFunction func = new PrintFunction( writer );
				lua["print"] = func;
				lua["remote"] = RemoteFunction.Instance;

				((LuaFunction)lua[function]).Call();
				
				Assert.AreEqual( expectedOutput, writer.ToString( ).Trim( ), message );
			}
		}
	}
}

