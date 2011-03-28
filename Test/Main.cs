//
// Main.cs
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
/// This will serve as a testing platform, and example usage of the LuaSharp library.
/// If you are looking for example usage of the low level wrapper, check out the LuaSharp class code.
/// </summary>

using System;
using LuaSharp;

namespace Test
{
	class MainClass
	{
		public static void Main( string[] args )
		{
			try
			{
				TestRemoting( );
					
				using( Lua state = new Lua(  ) )
				{
					LuaFunction print = state["print"] as LuaFunction;
					
					LuaTable table = state.CreateTable();
					table.SetValue( "test", "value" );
					state["incomingTable"] = table;
					
					state.DoFile( "test.lua" );
					var f = new Function();
					state["TestClr"] = f;
					
					LuaFunction f1 = state["AFunction"] as LuaFunction;
					
					state.DoString( "AFunction = nil" );
					
					print.Call( "AFunction returned: " + f1.Call(  )[0] );
					f1.Dispose(  );
				
					LuaFunction f2 = state["BFunction"] as LuaFunction;
					f2.Call(  );
					f2.Dispose(  );
					
					LuaTable sillytable = state["SillyTable"] as LuaTable;
					
					string str = sillytable["aaa"] as string;

					print.Call( str );

					sillytable["aaa"] = 9001;
					
					foreach( var val in sillytable )
					{
						Console.WriteLine("{0} = {1}", val.Key, val.Value);
					}
					
					print.Call( state["SillyTable", "aaa"] );
					
					sillytable.Dispose(  );

					state.CreateTable( "table" );
					print.Call( state["table"] as LuaTable );

					print.Dispose(  );
					GC.KeepAlive(f);
				}
			}
			catch( LuaException e )
			{
				Console.WriteLine( "Fail: " + e.Message );
			}
		}
		
		private static Lua Lookup( object key )
		{
			Lua result;
			LookupTable<string, Lua>.Retrieve( (string) key, out result );
			if( result != null && result.IsDisposed )
				result = null;
			return result;
		}
		
		private static void TestRemoting( )
		{
			RemoteFunction.LookupFunction = MainClass.Lookup;
				
			using( var lua2 = new Lua( ) )
			{
				using( var lua1 = new Lua( ) )
				{
					LookupTable<string, Lua>.Store( "lua1", lua1 );
					LookupTable<string, Lua>.Store( "lua2", lua2 );
					
					lua1["remote"] = RemoteFunction.Instance;
					lua1.DoString(
	@"function Test1( a, b )
		print( a );
		print( b.a.a );
		return ""Hello from Lua1 "" .. remote( ""lua2"", ""Test2"", ""a"", ""b"" );
	end");
					lua2["remote"] = RemoteFunction.Instance;
					lua2.DoString(
	@"
		function Test2( a, b )
			print( ""Test2"" .. a .. b );
			return ""Hello from Lua2 Called In"";
		end

		print( remote( ""lua1"", ""Test1"", ""a"", { a = { a = ""This is in a nested table."" } } ) );
	");
				}
			}
		}
	}
	
	class Function : ClrFunction
	{
		public Function ()
		{
			
		}
		
		protected override object[] OnInvoke ( Lua state, object[] args)
		{
			LuaFunction print = state["print"] as LuaFunction;
			print.Call( string.Format( "CLR function called with: {0}", args[0] ) );
			throw new Exception("Some CLR error.");
			return new object[] { "Test" };
		}
	}
}